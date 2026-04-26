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
