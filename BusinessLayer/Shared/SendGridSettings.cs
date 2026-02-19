namespace BusinessLayer.Shared;

/// <summary>
/// Settings for SendGrid email service configuration
/// </summary>
public class SendGridSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "VH Consultor";
    public string ContractTemplateId { get; set; } = string.Empty;
    public string ResetPasswordTemplateId { get; set; } = string.Empty;
    /// <summary>
    /// Template ID for invoice statement email (billing notification with PDF attachment).
    /// </summary>
    public string InvoiceStatementTemplateId { get; set; } = string.Empty;
    public string SupportEmail { get; set; } = string.Empty;
}
