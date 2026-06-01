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
    /// Variables: fullName, code, expiresIn (e.g. "3 minutes")
    /// </summary>
    public string BrandPartnerTwoFactorCodeTemplateId { get; set; } = string.Empty;
    /// <summary>
    /// Template ID for Brand Partner reset password email (contraseña temporal).
    /// Variables: fullName, username (email), password (contraseña temporal)
    /// </summary>
    public string BrandPartnerResetPasswordTemplateId { get; set; } = string.Empty;
    /// <summary>
    /// Template ID for contact form submission email (lead notification).
    /// Variables: submittedAt, source, firstName, lastName, email, phone, country,
    /// brandName, numberOfListings, productPageUrl, storeUrl, platform, accountType,
    /// serviceType, annualSalesRange, advertisingBudgetRange, promotionalBudgetRange,
    /// additionalDetails, leadId
    /// </summary>
    public string ContactFormTemplateId { get; set; } = string.Empty;
    /// <summary>
    /// Internal notification email address to receive contact form submissions.
    /// </summary>
    public string ContactFormNotificationEmail { get; set; } = string.Empty;

    /// <summary>
    /// Template ID for corporate email campaigns.
    /// Variables: subject, campaignBody, campaignName, recipientFirstName, recipientLastName
    /// </summary>
    public string CampaignTemplateId { get; set; } = string.Empty;
}
