using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Security.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Security.Queries;

public class SystemCredentialsQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public SystemCredentialsQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<SystemCredentials?> GetSystemCredentialsAsync(
        string? username = null, 
        string? password = null, 
        string? securityKey = null)
    {
        var query = @"
            SELECT 
                Id,
                ApplicationName,
                [User],
                Password,
                SecurityKey,
                IsActive,
                CreatedDate,
                ModifiedDate,
                CreatedBy,
                ModifiedBy
            FROM [UnitsTracking].[SystemCredentials]
            WHERE IsActive = 1";

        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(username))
        {
            query += " AND [User] = @Username";
            parameters.Add("@Username", username);
        }

        if (!string.IsNullOrEmpty(password))
        {
            query += " AND Password = @Password";
            parameters.Add("@Password", password);
        }

        if (!string.IsNullOrEmpty(securityKey))
        {
            query += " AND SecurityKey = @SecurityKey";
            parameters.Add("@SecurityKey", securityKey);
        }

        query += " ORDER BY ApplicationName, [User]";

        using var connection = new SqlConnection(_connectionResolver.GetConnectionString("PurdyApps"));
        
        // Si se proporcionan todos los parámetros, usar QuerySingleOrDefaultAsync
        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(securityKey))
        {
            return await connection.QuerySingleOrDefaultAsync<SystemCredentials>(query, parameters);
        }
        
        // Si no se proporcionan todos los parámetros, usar QueryAsync
        var results = await connection.QueryAsync<SystemCredentials>(query, parameters);
        return results.FirstOrDefault();
    }
} 