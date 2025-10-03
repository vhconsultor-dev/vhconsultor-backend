using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Security.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Security.Queries;

public class JwtCredentialsQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public JwtCredentialsQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<JwtCredentials?> ValidateCredentialsAsync(int companyId, string username, string password)
    {
        const string query = @"
            SELECT 
                Id,
                CompanyId,
                Application,
                Username,
                Password,
                SecureKey,
                Profile,
                IsActive,
                CreatedDate,
                ModifiedDate,
                CreatedBy,
                ModifiedBy
            FROM [AutoshopManagement].[JwtCredentials]
            WHERE CompanyId = @CompanyId 
                AND Username = @Username 
                AND Password = @Password
                AND IsActive = 1";

        using var connection = new SqlConnection(_connectionResolver.GetConnectionString("PostSales"));
        return await connection.QuerySingleOrDefaultAsync<JwtCredentials>(query, new { CompanyId = companyId, Username = username, Password = password });
    }
} 