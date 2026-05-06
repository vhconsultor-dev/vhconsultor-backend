namespace ModelLayer.Corporate.Entities;

public class ManualInvoiceDetail
{
    public int ManualInvoiceDetailId { get; set; }
    public int ManualInvoiceHeaderId { get; set; }
    public string ServiceDescription { get; set; } = string.Empty;
    public string? UnitLabel { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
    public bool TaxApplicable { get; set; } = true;
    public string? Notes { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation property
    public virtual ManualInvoiceHeader? Header { get; set; }
}
