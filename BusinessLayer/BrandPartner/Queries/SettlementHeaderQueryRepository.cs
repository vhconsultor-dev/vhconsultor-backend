using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.BrandPartner.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.BrandPartner.Queries;

/// <summary>
/// Repository de consultas para SettlementHeaders
/// </summary>
public class SettlementHeaderQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public SettlementHeaderQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<IEnumerable<SettlementHeader>> GetByAmazonAccountAsync(int amazonAccountId, SettlementHeaderFilters filters)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                SettlementHeaderId, AmazonAccountId, SettlementId,
                SettlementStartDate, SettlementEndDate, DepositDate,
                TotalAmount, Currency, SourceFileName, ImportedAt, CreatedAt
            FROM [BrandPartner].[SettlementHeaders]
            WHERE AmazonAccountId = @AmazonAccountId";

        var parameters = new DynamicParameters();
        parameters.Add("AmazonAccountId", amazonAccountId);

        if (!string.IsNullOrWhiteSpace(filters.SettlementId))
        {
            sql += " AND SettlementId = @SettlementId";
            parameters.Add("SettlementId", filters.SettlementId);
        }

        if (filters.DepositDateFrom.HasValue)
        {
            sql += " AND DepositDate >= @DepositDateFrom";
            parameters.Add("DepositDateFrom", filters.DepositDateFrom.Value);
        }

        if (filters.DepositDateTo.HasValue)
        {
            sql += " AND DepositDate <= @DepositDateTo";
            parameters.Add("DepositDateTo", filters.DepositDateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.Currency))
        {
            sql += " AND Currency = @Currency";
            parameters.Add("Currency", filters.Currency);
        }

        sql += " ORDER BY DepositDate DESC, CreatedAt DESC";

        if (filters.PageSize > 0)
        {
            int offset = (filters.PageNumber - 1) * filters.PageSize;
            sql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            parameters.Add("Offset", offset);
            parameters.Add("PageSize", filters.PageSize);
        }

        var items = await connection.QueryAsync<SettlementHeader>(sql, parameters);
        return items;
    }

    public async Task<SettlementHeader?> GetByIdAsync(long settlementHeaderId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                SettlementHeaderId, AmazonAccountId, SettlementId,
                SettlementStartDate, SettlementEndDate, DepositDate,
                TotalAmount, Currency, SourceFileName, ImportedAt, CreatedAt
            FROM [BrandPartner].[SettlementHeaders]
            WHERE SettlementHeaderId = @SettlementHeaderId";

        return await connection.QueryFirstOrDefaultAsync<SettlementHeader>(sql, new { SettlementHeaderId = settlementHeaderId });
    }

    public async Task<bool> SettlementIdExistsAsync(int amazonAccountId, string settlementId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT CAST(CASE WHEN EXISTS(
                SELECT 1 FROM [BrandPartner].[SettlementHeaders]
                WHERE AmazonAccountId = @AmazonAccountId AND SettlementId = @SettlementId
            ) THEN 1 ELSE 0 END AS BIT)";

        return await connection.ExecuteScalarAsync<bool>(sql, new { AmazonAccountId = amazonAccountId, SettlementId = settlementId });
    }
}

/// <summary>
/// Filtros para consulta de settlement headers
/// </summary>
public class SettlementHeaderFilters
{
    public string? SettlementId { get; set; }
    public DateTime? DepositDateFrom { get; set; }
    public DateTime? DepositDateTo { get; set; }
    public string? Currency { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
