using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.BrandPartner.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.BrandPartner.Queries;

/// <summary>
/// Repository de consultas para InventorySnapshots
/// </summary>
public class InventorySnapshotQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public InventorySnapshotQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<IEnumerable<InventorySnapshot>> GetByHeaderIdAsync(long settlementHeaderId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                InventorySnapshotId, SettlementHeaderId, InventoryItemId,
                Sku, QuantityBeforeSettlement, QuantityDeltaSettlement,
                QuantityAfterSettlement, SnapshotDate, CreatedAt
            FROM [BrandPartner].[InventorySnapshots]
            WHERE SettlementHeaderId = @SettlementHeaderId
            ORDER BY Sku ASC";

        return await connection.QueryAsync<InventorySnapshot>(sql, new { SettlementHeaderId = settlementHeaderId });
    }

    public async Task<IEnumerable<InventorySnapshot>> GetByInventoryItemAsync(long inventoryItemId, DateTime? dateFrom, DateTime? dateTo)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                InventorySnapshotId, SettlementHeaderId, InventoryItemId,
                Sku, QuantityBeforeSettlement, QuantityDeltaSettlement,
                QuantityAfterSettlement, SnapshotDate, CreatedAt
            FROM [BrandPartner].[InventorySnapshots]
            WHERE InventoryItemId = @InventoryItemId";

        var parameters = new DynamicParameters();
        parameters.Add("InventoryItemId", inventoryItemId);

        if (dateFrom.HasValue)
        {
            sql += " AND SnapshotDate >= @DateFrom";
            parameters.Add("DateFrom", dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            sql += " AND SnapshotDate <= @DateTo";
            parameters.Add("DateTo", dateTo.Value);
        }

        sql += " ORDER BY SnapshotDate DESC";

        return await connection.QueryAsync<InventorySnapshot>(sql, parameters);
    }
}
