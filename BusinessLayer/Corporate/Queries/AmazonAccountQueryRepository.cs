using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

/// <summary>
/// Repository for querying Corporate.AmazonAccounts with optional filters.
/// </summary>
public class AmazonAccountQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public AmazonAccountQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    /// <summary>
    /// Gets Amazon accounts with optional filters. String filters use partial match (LIKE).
    /// </summary>
    public async Task<IEnumerable<AmazonAccount>> GetAmazonAccountsAsync(
        int? id = null,
        int? customerId = null,
        string? amazonAccountIdentifier = null,
        bool? isSeller = null,
        bool? isVendor = null,
        string? amazonRegion = null,
        bool? isActive = null)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                AmazonAccountId,
                CustomerId,
                AmazonAccountIdentifier,
                IsSeller,
                IsVendor,
                RefreshToken,
                AmazonRegion,
                IsActive,
                CreatedAt,
                UpdatedAt
            FROM [Corporate].[AmazonAccounts]
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (id.HasValue)
        {
            sql += " AND AmazonAccountId = @Id";
            parameters.Add("Id", id.Value);
        }

        if (customerId.HasValue)
        {
            sql += " AND CustomerId = @CustomerId";
            parameters.Add("CustomerId", customerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(amazonAccountIdentifier))
        {
            sql += " AND AmazonAccountIdentifier LIKE @AmazonAccountIdentifier";
            parameters.Add("AmazonAccountIdentifier", "%" + amazonAccountIdentifier.Trim() + "%");
        }

        if (isSeller.HasValue)
        {
            sql += " AND IsSeller = @IsSeller";
            parameters.Add("IsSeller", isSeller.Value);
        }

        if (isVendor.HasValue)
        {
            sql += " AND IsVendor = @IsVendor";
            parameters.Add("IsVendor", isVendor.Value);
        }

        if (!string.IsNullOrWhiteSpace(amazonRegion))
        {
            sql += " AND AmazonRegion LIKE @AmazonRegion";
            parameters.Add("AmazonRegion", "%" + amazonRegion.Trim() + "%");
        }

        if (isActive.HasValue)
        {
            sql += " AND IsActive = @IsActive";
            parameters.Add("IsActive", isActive.Value);
        }

        sql += " ORDER BY CreatedAt DESC";

        var items = await connection.QueryAsync<AmazonAccount>(sql, parameters);
        return items.ToList();
    }

    /// <summary>
    /// Gets a single account by ID.
    /// </summary>
    public async Task<AmazonAccount?> GetByIdAsync(int amazonAccountId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                AmazonAccountId, CustomerId, AmazonAccountIdentifier,
                IsSeller, IsVendor, RefreshToken, AmazonRegion, IsActive,
                CreatedAt, UpdatedAt
            FROM [Corporate].[AmazonAccounts]
            WHERE AmazonAccountId = @AmazonAccountId";

        return await connection.QueryFirstOrDefaultAsync<AmazonAccount>(sql, new { AmazonAccountId = amazonAccountId });
    }
}
