namespace ModelLayer.BrandPartner.Entities;

/// <summary>
/// Detalle línea por línea de settlement
/// </summary>
public class SettlementDetail
{
    public long SettlementDetailId { get; set; }
    public long SettlementHeaderId { get; set; }
    public int? RowNumber { get; set; }
    public string? TransactionType { get; set; }
    public string? OrderId { get; set; }
    public string? MerchantOrderId { get; set; }
    public string? AdjustmentId { get; set; }
    public string? ShipmentId { get; set; }
    public string? MarketplaceName { get; set; }
    public string? ShipmentFeeType { get; set; }
    public decimal? ShipmentFeeAmount { get; set; }
    public string? OrderFeeType { get; set; }
    public decimal? OrderFeeAmount { get; set; }
    public string? FulfillmentId { get; set; }
    public DateTime? PostedDate { get; set; }
    public string? OrderItemCode { get; set; }
    public string? MerchantOrderItemId { get; set; }
    public string? MerchantAdjustmentItemId { get; set; }
    public string? Sku { get; set; }
    public decimal? QuantityPurchased { get; set; }
    public string? PriceType { get; set; }
    public decimal? PriceAmount { get; set; }
    public string? ItemRelatedFeeType { get; set; }
    public decimal? ItemRelatedFeeAmount { get; set; }
    public decimal? MiscFeeAmount { get; set; }
    public decimal? OtherFeeAmount { get; set; }
    public string? OtherFeeReasonDescription { get; set; }
    public string? PromotionId { get; set; }
    public string? PromotionType { get; set; }
    public decimal? PromotionAmount { get; set; }
    public string? DirectPaymentType { get; set; }
    public decimal? DirectPaymentAmount { get; set; }
    public decimal? OtherAmount { get; set; }
    public bool AffectsInventory { get; set; }
    public int? InventoryDelta { get; set; }
    public string? RowHash { get; set; }
    public string? RawRowJson { get; set; }
    public DateTime CreatedAt { get; set; }
}
