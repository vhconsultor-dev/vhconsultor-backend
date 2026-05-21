using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Shared;
using BusinessLayer.Corporate.Models;

namespace BusinessLayer.Corporate.Queries;

public class UserCustomerAssignmentQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public UserCustomerAssignmentQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<List<UserCustomerAssignmentFlatRow>> GetFlatRowsAsync(
        int? userCustomerAssignmentId = null,
        int? userId = null,
        int? customerId = null,
        bool? isActive = null)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT
                uca.[UserCustomerAssignmentId],
                uca.[UserId],
                uca.[CustomerId],
                uca.[IsActive] AS AssignmentIsActive,
                uca.[AssignedAt],
                uca.[AssignedBy],
                uca.[UpdatedAt] AS AssignmentUpdatedAt,
                u.[FirstName] AS CorporateUserFirstName,
                u.[LastName] AS CorporateUserLastName,
                u.[Email] AS CorporateUserEmail,
                u.[IsActive] AS CorporateUserIsActive,
                c.[CompanyName],
                c.[NIT],
                c.[CompanyType],
                c.[PrimaryEmail] AS CustomerPrimaryEmail,
                c.[PrimaryPhone] AS CustomerPrimaryPhone,
                c.[City],
                c.[CountryId],
                c.[ClientStatus],
                c.[IsActive] AS CustomerIsActive,
                aa.[AmazonAccountId],
                aa.[AmazonAccountIdentifier],
                aa.[IsSeller] AS AmazonIsSeller,
                aa.[IsVendor] AS AmazonIsVendor,
                aa.[AmazonRegion],
                aa.[IsActive] AS AmazonAccountIsActive,
                aa.[CreatedAt] AS AmazonCreatedAt,
                aa.[UpdatedAt] AS AmazonUpdatedAt
            FROM [Global].[UserCustomerAssignments] uca
            INNER JOIN [Global].[Users] u ON u.[UserId] = uca.[UserId]
            INNER JOIN [Corporate].[Customers] c ON c.[CustomerId] = uca.[CustomerId]
            LEFT JOIN [Corporate].[AmazonAccounts] aa ON aa.[CustomerId] = c.[CustomerId]
            WHERE 1 = 1";

        var parameters = new DynamicParameters();

        if (userCustomerAssignmentId.HasValue)
        {
            sql += " AND uca.[UserCustomerAssignmentId] = @UserCustomerAssignmentId";
            parameters.Add("UserCustomerAssignmentId", userCustomerAssignmentId.Value);
        }

        if (userId.HasValue)
        {
            sql += " AND uca.[UserId] = @UserId";
            parameters.Add("UserId", userId.Value);
        }

        if (customerId.HasValue)
        {
            sql += " AND uca.[CustomerId] = @CustomerId";
            parameters.Add("CustomerId", customerId.Value);
        }

        if (isActive.HasValue)
        {
            sql += " AND uca.[IsActive] = @IsActive";
            parameters.Add("IsActive", isActive.Value);
        }

        sql += " ORDER BY uca.[UserId], c.[CompanyName], aa.[AmazonAccountIdentifier]";

        var rows = await connection.QueryAsync<UserCustomerAssignmentFlatRow>(sql, parameters);
        return rows.ToList();
    }

    public static List<UserCustomerAssignmentDetailDto> MapFlatRowsToDetails(IReadOnlyList<UserCustomerAssignmentFlatRow> rows)
    {
        return rows
            .GroupBy(r => r.UserCustomerAssignmentId)
            .Select(g =>
            {
                var first = g.First();
                return new UserCustomerAssignmentDetailDto
                {
                    UserCustomerAssignmentId = first.UserCustomerAssignmentId,
                    UserId = first.UserId,
                    CustomerId = first.CustomerId,
                    IsActive = first.AssignmentIsActive,
                    AssignedAt = first.AssignedAt,
                    AssignedBy = first.AssignedBy,
                    UpdatedAt = first.AssignmentUpdatedAt,
                    CorporateUser = new UserCustomerAssignmentCorporateUserDto
                    {
                        UserId = first.UserId,
                        FirstName = first.CorporateUserFirstName,
                        LastName = first.CorporateUserLastName,
                        Email = first.CorporateUserEmail,
                        IsActive = first.CorporateUserIsActive
                    },
                    Customer = new UserCustomerAssignmentCustomerDto
                    {
                        CustomerId = first.CustomerId,
                        CompanyName = first.CompanyName,
                        NIT = first.NIT,
                        CompanyType = first.CompanyType,
                        PrimaryEmail = first.CustomerPrimaryEmail,
                        PrimaryPhone = first.CustomerPrimaryPhone,
                        City = first.City,
                        CountryId = first.CountryId,
                        ClientStatus = first.ClientStatus,
                        IsActive = first.CustomerIsActive
                    },
                    AmazonAccounts = g
                        .Where(r => r.AmazonAccountId.HasValue)
                        .Select(r => new UserCustomerAssignmentAmazonAccountDto
                        {
                            AmazonAccountId = r.AmazonAccountId!.Value,
                            CustomerId = first.CustomerId,
                            AmazonAccountIdentifier = r.AmazonAccountIdentifier ?? string.Empty,
                            IsSeller = r.AmazonIsSeller ?? false,
                            IsVendor = r.AmazonIsVendor ?? false,
                            AmazonRegion = r.AmazonRegion ?? string.Empty,
                            IsActive = r.AmazonAccountIsActive ?? false,
                            CreatedAt = r.AmazonCreatedAt ?? default,
                            UpdatedAt = r.AmazonUpdatedAt
                        })
                        .GroupBy(a => a.AmazonAccountId)
                        .Select(x => x.First())
                        .OrderBy(a => a.AmazonAccountIdentifier)
                        .ToList()
                };
            })
            .OrderBy(d => d.CorporateUser.LastName)
            .ThenBy(d => d.Customer.CompanyName)
            .ToList();
    }
}
