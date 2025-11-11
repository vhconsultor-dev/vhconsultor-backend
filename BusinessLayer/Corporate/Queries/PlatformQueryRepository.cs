using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

public class PlatformQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public PlatformQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<IEnumerable<dynamic>> GetPlatformsAsync(
        int? platformId = null,
        int? businessTypeId = null,
        string? platformName = null,
        string? platformKey = null,
        bool? isActive = null)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);

        var sql = new StringBuilder(@"
            SELECT 
                p.PlatformId,
                p.BusinessTypeId,
                p.PlatformName,
                p.PlatformKey,
                p.Description,
                p.IsActive,
                p.CreatedAt,
                p.UpdatedAt,
                bt.BusinessTypeName,
                bt.BusinessTypeKey
            FROM Corporate.Platforms p
            INNER JOIN Corporate.BusinessTypes bt ON p.BusinessTypeId = bt.BusinessTypeId
            WHERE 1=1");

        var parameters = new DynamicParameters();

        if (platformId.HasValue)
        {
            sql.Append(" AND p.PlatformId = @PlatformId");
            parameters.Add("PlatformId", platformId.Value);
        }

        if (businessTypeId.HasValue)
        {
            sql.Append(" AND p.BusinessTypeId = @BusinessTypeId");
            parameters.Add("BusinessTypeId", businessTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(platformName))
        {
            sql.Append(" AND p.PlatformName LIKE @PlatformName");
            parameters.Add("PlatformName", $"%{platformName}%");
        }

        if (!string.IsNullOrWhiteSpace(platformKey))
        {
            sql.Append(" AND p.PlatformKey = @PlatformKey");
            parameters.Add("PlatformKey", platformKey);
        }

        if (isActive.HasValue)
        {
            sql.Append(" AND p.IsActive = @IsActive");
            parameters.Add("IsActive", isActive.Value);
        }

        sql.Append(" ORDER BY p.PlatformName ASC");

        return await connection.QueryAsync<dynamic>(sql.ToString(), parameters);
    }

    public async Task<dynamic?> GetPlatformByIdAsync(int id)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);

        var sql = @"
            SELECT 
                p.PlatformId,
                p.BusinessTypeId,
                p.PlatformName,
                p.PlatformKey,
                p.Description,
                p.IsActive,
                p.CreatedAt,
                p.UpdatedAt,
                bt.BusinessTypeName,
                bt.BusinessTypeKey
            FROM Corporate.Platforms p
            INNER JOIN Corporate.BusinessTypes bt ON p.BusinessTypeId = bt.BusinessTypeId
            WHERE p.PlatformId = @Id";

        return await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
    }
}

