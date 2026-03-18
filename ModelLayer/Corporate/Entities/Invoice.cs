namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Entidad que representa una factura generada desde un contrato
/// </summary>
public class Invoice
{
    public int InvoiceId { get; set; }
    public int ContractId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft"; // Draft, Sent, Paid, Overdue, Cancelled
    public string PaymentStatus { get; set; } = "Unpaid"; // Unpaid, Paid
    public DateTime? PaidDate { get; set; }
    public int? PaidBy { get; set; }
    public int? PaymentMethodId { get; set; }
    public string? PaymentReference { get; set; }
    public string? DepositNumber { get; set; }
    public string? TransferNumber { get; set; }
    public string? Notes { get; set; }
    /// <summary>Language for PDF generation: "en" (English) or "es" (Spanish).</summary>
    public string? Lang { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? LastModifiedBy { get; set; }
    
    // Navigation properties
    public virtual Contract? Contract { get; set; }
    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
    public virtual ICollection<InvoiceAttachment> InvoiceAttachments { get; set; } = new List<InvoiceAttachment>();
}

