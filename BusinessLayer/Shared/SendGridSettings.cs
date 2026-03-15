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
    /// <summary>
    /// Template ID for Brand Partner 2FA code email (código de verificación 2FA).
    /// Variables: {{ fullName }}, {{ code }}, {{ expiresIn }}
    /// </summary>
    public string BrandPartnerTwoFactorCodeTemplateId { get; set; } = string.Empty;
    /// <summary>
    /// Template ID for Brand Partner reset password email (contraseña temporal).
    /// Variables: {{ fullName }}, {{ email }}, {{ newPassword }}
    /// </summary>
    public string BrandPartnerResetPasswordTemplateId { get; set; } = string.Empty;
}
