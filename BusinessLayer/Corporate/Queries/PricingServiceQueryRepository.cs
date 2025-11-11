using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

public class PricingServiceQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public PricingServiceQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<IEnumerable<dynamic>> GetPricingServicesAsync(
        int? serviceId = null,
        string? serviceName = null,
        string? serviceKey = null,
        string? serviceCategory = null,
        bool? isActive = null)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);

        var sql = new StringBuilder(@"
            SELECT 
                ServiceId,
                ServiceName,
                ServiceKey,
                Description,
                ServiceCategory,
                IsActive,
                CreatedAt,
                UpdatedAt
            FROM Corporate.PricingServices
            WHERE 1=1");

        var parameters = new DynamicParameters();

        if (serviceId.HasValue)
        {
            sql.Append(" AND ServiceId = @ServiceId");
            parameters.Add("ServiceId", serviceId.Value);
        }

        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            sql.Append(" AND ServiceName LIKE @ServiceName");
            parameters.Add("ServiceName", $"%{serviceName}%");
        }

        if (!string.IsNullOrWhiteSpace(serviceKey))
        {
            sql.Append(" AND ServiceKey = @ServiceKey");
            parameters.Add("ServiceKey", serviceKey);
        }

        if (!string.IsNullOrWhiteSpace(serviceCategory))
        {
            sql.Append(" AND ServiceCategory = @ServiceCategory");
            parameters.Add("ServiceCategory", serviceCategory);
        }

        if (isActive.HasValue)
        {
            sql.Append(" AND IsActive = @IsActive");
            parameters.Add("IsActive", isActive.Value);
        }

        sql.Append(" ORDER BY ServiceName ASC");

        return await connection.QueryAsync<dynamic>(sql.ToString(), parameters);
    }

    public async Task<dynamic?> GetPricingServiceByIdAsync(int id)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);

        var sql = @"
            SELECT 
                ServiceId,
                ServiceName,
                ServiceKey,
                Description,
                ServiceCategory,
                IsActive,
                CreatedAt,
                UpdatedAt
            FROM Corporate.PricingServices
            WHERE ServiceId = @Id";

        return await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
    }

    public async Task<dynamic?> GetPricingServiceByKeyAsync(string serviceKey)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);

        var sql = @"
            SELECT 
                ServiceId,
                ServiceName,
                ServiceKey,
                Description,
                ServiceCategory,
                IsActive,
                CreatedAt,
                UpdatedAt
            FROM Corporate.PricingServices
            WHERE ServiceKey = @ServiceKey";

        return await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { ServiceKey = serviceKey });
    }
}
