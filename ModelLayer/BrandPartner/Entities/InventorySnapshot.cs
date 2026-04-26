namespace ModelLayer.BrandPartner.Entities;

/// <summary>
/// Snapshot de inventario por settlement (foto del momento del corte)
/// </summary>
public class InventorySnapshot
{
    public long InventorySnapshotId { get; set; }
    public long SettlementHeaderId { get; set; }
    public long InventoryItemId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int QuantityBeforeSettlement { get; set; }
    public int QuantityDeltaSettlement { get; set; }
    public int QuantityAfterSettlement { get; set; }
    public DateTime SnapshotDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
