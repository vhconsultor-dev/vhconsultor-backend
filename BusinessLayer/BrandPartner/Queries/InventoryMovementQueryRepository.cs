using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.BrandPartner.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.BrandPartner.Queries;

/// <summary>
/// Repository de consultas para InventoryMovements (auditoría)
/// </summary>
public class InventoryMovementQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public InventoryMovementQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<IEnumerable<InventoryMovement>> GetByInventoryItemAsync(long inventoryItemId, InventoryMovementFilters filters)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                InventoryMovementId, InventoryItemId, SettlementHeaderId, SettlementDetailId,
                MovementType, ReasonCode, QuantityBefore, QuantityDelta, QuantityAfter,
                ReferenceType, ReferenceId, Comments, CreatedBy, CreatedAt
            FROM [BrandPartner].[InventoryMovements]
            WHERE InventoryItemId = @InventoryItemId";

        var parameters = new DynamicParameters();
        parameters.Add("InventoryItemId", inventoryItemId);

        if (!string.IsNullOrWhiteSpace(filters.MovementType))
        {
            sql += " AND MovementType = @MovementType";
            parameters.Add("MovementType", filters.MovementType);
        }

        if (filters.DateFrom.HasValue)
        {
            sql += " AND CreatedAt >= @DateFrom";
            parameters.Add("DateFrom", filters.DateFrom.Value);
        }

        if (filters.DateTo.HasValue)
        {
            sql += " AND CreatedAt <= @DateTo";
            parameters.Add("DateTo", filters.DateTo.Value);
        }

        sql += " ORDER BY CreatedAt DESC";

        if (filters.PageSize > 0)
        {
            int offset = (filters.PageNumber - 1) * filters.PageSize;
            sql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            parameters.Add("Offset", offset);
            parameters.Add("PageSize", filters.PageSize);
        }

        return await connection.QueryAsync<InventoryMovement>(sql, parameters);
    }

    public async Task<IEnumerable<InventoryMovement>> GetByAccountAndInventoryItemAsync(int amazonAccountId, long inventoryItemId, InventoryMovementFilters filters)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                im.InventoryMovementId, im.InventoryItemId, im.SettlementHeaderId, im.SettlementDetailId,
                im.MovementType, im.ReasonCode, im.QuantityBefore, im.QuantityDelta, im.QuantityAfter,
                im.ReferenceType, im.ReferenceId, im.Comments, im.CreatedBy, im.CreatedAt
            FROM [BrandPartner].[InventoryMovements] im
            INNER JOIN [BrandPartner].[InventoryItems] ii ON ii.InventoryItemId = im.InventoryItemId
            WHERE im.InventoryItemId = @InventoryItemId
              AND ii.AmazonAccountId = @AmazonAccountId";

        var parameters = new DynamicParameters();
        parameters.Add("InventoryItemId", inventoryItemId);
        parameters.Add("AmazonAccountId", amazonAccountId);

        if (!string.IsNullOrWhiteSpace(filters.MovementType))
        {
            sql += " AND im.MovementType = @MovementType";
            parameters.Add("MovementType", filters.MovementType);
        }

        if (filters.DateFrom.HasValue)
        {
            sql += " AND im.CreatedAt >= @DateFrom";
            parameters.Add("DateFrom", filters.DateFrom.Value);
        }

        if (filters.DateTo.HasValue)
        {
            sql += " AND im.CreatedAt <= @DateTo";
            parameters.Add("DateTo", filters.DateTo.Value);
        }

        sql += " ORDER BY im.CreatedAt DESC";

        if (filters.PageSize > 0)
        {
            int offset = (filters.PageNumber - 1) * filters.PageSize;
            sql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            parameters.Add("Offset", offset);
            parameters.Add("PageSize", filters.PageSize);
        }

        return await connection.QueryAsync<InventoryMovement>(sql, parameters);
    }
}

/// <summary>
/// Filtros para consulta de movimientos de inventario
/// </summary>
public class InventoryMovementFilters
{
    public string? MovementType { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 100;
}
