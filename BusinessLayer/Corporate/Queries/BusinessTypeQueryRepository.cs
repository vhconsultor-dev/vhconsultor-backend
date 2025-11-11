using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

public class BusinessTypeQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public BusinessTypeQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<IEnumerable<dynamic>> GetBusinessTypesAsync(
        int? businessTypeId = null,
        string? businessTypeName = null,
        string? businessTypeKey = null,
        bool? isActive = null)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);

        var sql = new StringBuilder(@"
            SELECT 
                BusinessTypeId,
                BusinessTypeName,
                BusinessTypeKey,
                Description,
                IsActive,
                CreatedAt,
                UpdatedAt
            FROM Corporate.BusinessTypes
            WHERE 1=1");

        var parameters = new DynamicParameters();

        if (businessTypeId.HasValue)
        {
            sql.Append(" AND BusinessTypeId = @BusinessTypeId");
            parameters.Add("BusinessTypeId", businessTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(businessTypeName))
        {
            sql.Append(" AND BusinessTypeName LIKE @BusinessTypeName");
            parameters.Add("BusinessTypeName", $"%{businessTypeName}%");
        }

        if (!string.IsNullOrWhiteSpace(businessTypeKey))
        {
            sql.Append(" AND BusinessTypeKey = @BusinessTypeKey");
            parameters.Add("BusinessTypeKey", businessTypeKey);
        }

        if (isActive.HasValue)
        {
            sql.Append(" AND IsActive = @IsActive");
            parameters.Add("IsActive", isActive.Value);
        }

        sql.Append(" ORDER BY BusinessTypeName ASC");

        return await connection.QueryAsync<dynamic>(sql.ToString(), parameters);
    }

    public async Task<dynamic?> GetBusinessTypeByIdAsync(int id)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);

        var sql = @"
            SELECT 
                BusinessTypeId,
                BusinessTypeName,
                BusinessTypeKey,
                Description,
                IsActive,
                CreatedAt,
                UpdatedAt
            FROM Corporate.BusinessTypes
            WHERE BusinessTypeId = @Id";

        return await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
    }
}

