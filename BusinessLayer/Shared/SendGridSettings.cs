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
}
