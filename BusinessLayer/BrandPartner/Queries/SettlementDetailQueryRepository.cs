using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.BrandPartner.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.BrandPartner.Queries;

/// <summary>
/// Repository de consultas para SettlementDetails
/// </summary>
public class SettlementDetailQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public SettlementDetailQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<IEnumerable<SettlementDetail>> GetByHeaderIdAsync(long settlementHeaderId, SettlementDetailFilters filters)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                SettlementDetailId, SettlementHeaderId, RowNumber,
                TransactionType, OrderId, MerchantOrderId, AdjustmentId, ShipmentId,
                MarketplaceName, ShipmentFeeType, ShipmentFeeAmount, OrderFeeType, OrderFeeAmount,
                FulfillmentId, PostedDate, OrderItemCode, MerchantOrderItemId, MerchantAdjustmentItemId,
                Sku, QuantityPurchased, PriceType, PriceAmount,
                ItemRelatedFeeType, ItemRelatedFeeAmount, MiscFeeAmount, OtherFeeAmount,
                OtherFeeReasonDescription, PromotionId, PromotionType, PromotionAmount,
                DirectPaymentType, DirectPaymentAmount, OtherAmount,
                AffectsInventory, InventoryDelta, RowHash, CreatedAt
            FROM [BrandPartner].[SettlementDetails]
            WHERE SettlementHeaderId = @SettlementHeaderId";

        var parameters = new DynamicParameters();
        parameters.Add("SettlementHeaderId", settlementHeaderId);

        if (!string.IsNullOrWhiteSpace(filters.TransactionType))
        {
            sql += " AND TransactionType = @TransactionType";
            parameters.Add("TransactionType", filters.TransactionType);
        }

        if (!string.IsNullOrWhiteSpace(filters.OrderId))
        {
            sql += " AND OrderId = @OrderId";
            parameters.Add("OrderId", filters.OrderId);
        }

        if (!string.IsNullOrWhiteSpace(filters.Sku))
        {
            sql += " AND Sku LIKE @Sku";
            parameters.Add("Sku", $"%{filters.Sku}%");
        }

        if (filters.PostedDateFrom.HasValue)
        {
            sql += " AND PostedDate >= @PostedDateFrom";
            parameters.Add("PostedDateFrom", filters.PostedDateFrom.Value);
        }

        if (filters.PostedDateTo.HasValue)
        {
            sql += " AND PostedDate <= @PostedDateTo";
            parameters.Add("PostedDateTo", filters.PostedDateTo.Value);
        }

        sql += " ORDER BY PostedDate DESC, RowNumber ASC";

        if (filters.PageSize > 0)
        {
            int offset = (filters.PageNumber - 1) * filters.PageSize;
            sql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            parameters.Add("Offset", offset);
            parameters.Add("PageSize", filters.PageSize);
        }

        var items = await connection.QueryAsync<SettlementDetail>(sql, parameters);
        return items;
    }

    public async Task<IEnumerable<SettlementDetail>> GetByOrderIdAsync(string orderId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                SettlementDetailId, SettlementHeaderId, RowNumber,
                TransactionType, OrderId, MerchantOrderId, AdjustmentId, ShipmentId,
                MarketplaceName, ShipmentFeeType, ShipmentFeeAmount, OrderFeeType, OrderFeeAmount,
                FulfillmentId, PostedDate, OrderItemCode, MerchantOrderItemId, MerchantAdjustmentItemId,
                Sku, QuantityPurchased, PriceType, PriceAmount,
                ItemRelatedFeeType, ItemRelatedFeeAmount, MiscFeeAmount, OtherFeeAmount,
                OtherFeeReasonDescription, PromotionId, PromotionType, PromotionAmount,
                DirectPaymentType, DirectPaymentAmount, OtherAmount,
                AffectsInventory, InventoryDelta, RowHash, CreatedAt
            FROM [BrandPartner].[SettlementDetails]
            WHERE OrderId = @OrderId
            ORDER BY PostedDate DESC";

        return await connection.QueryAsync<SettlementDetail>(sql, new { OrderId = orderId });
    }
}

/// <summary>
/// Filtros para consulta de settlement details
/// </summary>
public class SettlementDetailFilters
{
    public string? TransactionType { get; set; }
    public string? OrderId { get; set; }
    public string? Sku { get; set; }
    public DateTime? PostedDateFrom { get; set; }
    public DateTime? PostedDateTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 100;
}
