namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Entidad que representa un item/línea de una factura
/// </summary>
public class InvoiceItem
{
    public int InvoiceItemId { get; set; }
    public int InvoiceId { get; set; }
    public int? ContractServiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }
    public int? ServiceOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public virtual Invoice? Invoice { get; set; }
    public virtual ContractService? ContractService { get; set; }
}




