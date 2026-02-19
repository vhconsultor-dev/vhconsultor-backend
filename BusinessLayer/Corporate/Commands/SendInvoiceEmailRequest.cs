namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Request model for sending invoice statement email (billing notification with PDF attachment).
/// </summary>
public class SendInvoiceEmailRequest
{
    /// <summary>
    /// Recipient email address (e.g. client contact).
    /// </summary>
    public string ToEmail { get; set; } = string.Empty;

    /// <summary>
    /// Optional CC emails, comma-separated.
    /// </summary>
    public string? CcEmail { get; set; }

    /// <summary>
    /// Data for the SendGrid invoice statement template.
    /// </summary>
    public InvoiceEmailData Data { get; set; } = new();
}

/// <summary>
/// Invoice data for the email template (Invoice Statement).
/// </summary>
public class InvoiceEmailData
{
    public string ClientName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string InvoiceMonth { get; set; } = string.Empty;
    public string InvoiceYear { get; set; } = string.Empty;
    public string IssueDate { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string TotalAmount { get; set; } = string.Empty;
}

/// <summary>
/// Response model for sending invoice email.
/// </summary>
public class SendInvoiceEmailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
