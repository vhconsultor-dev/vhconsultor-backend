using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Security.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Security.Queries;

public class UserQueryRepository
{
    private readonly string _connectionString;

    public UserQueryRepository(IConnectionResolver connectionResolver)
    {
        try
        {
            _connectionString = connectionResolver.GetConnectionString("PostSales");
            
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException("Connection string 'PostSales' is null or empty");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to initialize UserQueryRepository: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<User>> GetUsersAsync(string? email = null, string? sippUser = null)
    {
        var sql = @"
            SELECT Id, CompanyId, Email, Name, RoleId, CreatedDate, CreatedBy, 
                   IsActive, ModifiedDate, ModifiedBy, sippUser
            FROM AutoshopManagement.Users 
            WHERE IsActive = 1";

        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(email))
        {
            sql += " AND Email = @Email";
            parameters.Add("@Email", email);
        }

        if (!string.IsNullOrEmpty(sippUser))
        {
            sql += " AND sippUser = @SippUser";
            parameters.Add("@SippUser", sippUser);
        }

        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryAsync<User>(sql, parameters);
    }
} 