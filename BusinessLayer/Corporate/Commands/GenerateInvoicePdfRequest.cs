using System.Text.Json.Serialization;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Request to generate an invoice PDF using CraftMyPDF. Property names are serialized as snake_case to match the invoice template.
/// </summary>
public class GenerateInvoicePdfRequest
{
    [JsonPropertyName("items")]
    public List<InvoicePdfItemRequest> Items { get; set; } = new();

    [JsonPropertyName("company_name")]
    public string CompanyName { get; set; } = string.Empty;

    [JsonPropertyName("company_address")]
    public string CompanyAddress { get; set; } = string.Empty;

    [JsonPropertyName("company_email")]
    public string CompanyEmail { get; set; } = string.Empty;

    [JsonPropertyName("bill_to")]
    public string BillTo { get; set; } = string.Empty;

    [JsonPropertyName("bill_to_address")]
    public string BillToAddress { get; set; } = string.Empty;

    [JsonPropertyName("invoice_detail")]
    public string InvoiceDetail { get; set; } = string.Empty;

    [JsonPropertyName("invoice_no")]
    public string InvoiceNo { get; set; } = string.Empty;

    [JsonPropertyName("invoice_date")]
    public string InvoiceDate { get; set; } = string.Empty;

    [JsonPropertyName("invoice_due_date")]
    public string InvoiceDueDate { get; set; } = string.Empty;

    [JsonPropertyName("footer")]
    public string Footer { get; set; } = string.Empty;

    [JsonPropertyName("taxRate")]
    public decimal? TaxRate { get; set; }

    [JsonPropertyName("otherAmount")]
    public decimal? OtherAmount { get; set; }

    [JsonPropertyName("balance")]
    public string Balance { get; set; } = string.Empty;

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "$";

    [JsonPropertyName("lang")]
    public string Lang { get; set; } = "es";
}

/// <summary>
/// Line item for the invoice PDF template (row in the table).
/// </summary>
public class InvoicePdfItemRequest
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("qty")]
    public decimal Qty { get; set; }

    [JsonPropertyName("unitprice")]
    public decimal Unitprice { get; set; }
}
