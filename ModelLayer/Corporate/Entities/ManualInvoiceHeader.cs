namespace ModelLayer.Corporate.Entities;

public class ManualInvoiceHeader
{
    public int ManualInvoiceHeaderId { get; set; }
    public int CustomerId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; } = 1;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "Draft";
    public string PaymentStatus { get; set; } = "Unpaid";
    public DateTime? PaidDate { get; set; }
    public int? PaidBy { get; set; }
    public int? PaymentMethodId { get; set; }
    public string? PaymentReference { get; set; }
    public string? DepositNumber { get; set; }
    public string? TransferNumber { get; set; }
    public DateTime? BillingPeriodFrom { get; set; }
    public DateTime? BillingPeriodTo { get; set; }
    public string? BillingName { get; set; }
    public string? BillingEmail { get; set; }
    public string? Notes { get; set; }
    public string? ClientNotes { get; set; }
    public string Lang { get; set; } = "es";
    public DateTime? SentAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public int? CancelledBy { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? LastModifiedBy { get; set; }

    // Navigation properties
    public virtual Customer? Customer { get; set; }
    public virtual ICollection<ManualInvoiceDetail> Details { get; set; } = new List<ManualInvoiceDetail>();
}
