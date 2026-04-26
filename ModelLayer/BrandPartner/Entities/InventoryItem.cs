namespace ModelLayer.BrandPartner.Entities;

/// <summary>
/// Inventario actual por AmazonAccount + SKU
/// </summary>
public class InventoryItem
{
    public long InventoryItemId { get; set; }
    public int AmazonAccountId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Asin { get; set; }
    public string? ProductName { get; set; }
    public string? PrepOwner { get; set; }
    public string? LabelingOwner { get; set; }
    public decimal? UnitsPerBox { get; set; }
    public int? NumberOfBoxes { get; set; }
    public decimal? BoxLengthIn { get; set; }
    public decimal? BoxWidthIn { get; set; }
    public decimal? BoxHeightIn { get; set; }
    public decimal? BoxWeightLb { get; set; }
    public int QuantityOnHand { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
