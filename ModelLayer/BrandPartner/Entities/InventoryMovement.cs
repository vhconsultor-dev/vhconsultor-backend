namespace ModelLayer.BrandPartner.Entities;

/// <summary>
/// Bitácora de movimientos de inventario para auditoría completa
/// </summary>
public class InventoryMovement
{
    public long InventoryMovementId { get; set; }
    public long InventoryItemId { get; set; }
    public long? SettlementHeaderId { get; set; }
    public long? SettlementDetailId { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string? ReasonCode { get; set; }
    public int QuantityBefore { get; set; }
    public int QuantityDelta { get; set; }
    public int QuantityAfter { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceId { get; set; }
    public string? Comments { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
