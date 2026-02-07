using BusinessLayer.Shared;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ApplicationLayer.Shared;

/// <summary>
/// Service for sending emails using SendGrid
/// </summary>
public class SendGridService
{
    private readonly SendGridSettings _settings;
    private readonly SendGridClient _client;

    public SendGridService(IOptions<SendGridSettings> settings)
    {
        _settings = settings.Value;
        _client = new SendGridClient(_settings.ApiKey);
    }

    /// <summary>
    /// Sends an email using a SendGrid template with optional PDF attachment
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="templateId">SendGrid template ID</param>
    /// <param name="templateData">Dynamic data for the template</param>
    /// <param name="ccEmail">Optional CC email address</param>
    /// <param name="pdfContent">Optional PDF file content as byte array</param>
    /// <param name="pdfFileName">Optional PDF file name</param>
    /// <returns>True if email was sent successfully, false otherwise</returns>
    public async Task<SendGridEmailResult> SendTemplateEmailAsync(
        string toEmail,
        string templateId,
        object templateData,
        string? ccEmail = null,
        byte[]? pdfContent = null,
        string? pdfFileName = null)
    {
        try
        {
            var from = new EmailAddress(_settings.FromEmail, _settings.FromName);
            var to = new EmailAddress(toEmail);

            var msg = MailHelper.CreateSingleTemplateEmail(from, to, templateId, templateData);

            // Add CC if provided (supports multiple emails separated by commas)
            if (!string.IsNullOrWhiteSpace(ccEmail))
            {
                var ccEmails = ccEmail.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var email in ccEmails)
                {
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        msg.AddCc(new EmailAddress(email));
                    }
                }
            }

            // Add PDF attachment if provided
            if (pdfContent != null && !string.IsNullOrWhiteSpace(pdfFileName))
            {
                var base64Content = Convert.ToBase64String(pdfContent);
                msg.AddAttachment(pdfFileName, base64Content, "application/pdf");
            }

            var response = await _client.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                return new SendGridEmailResult
                {
                    Success = true,
                    Message = "Email sent successfully"
                };
            }

            var responseBody = await response.Body.ReadAsStringAsync();
            return new SendGridEmailResult
            {
                Success = false,
                Message = $"Failed to send email. Status: {response.StatusCode}",
                ErrorDetails = responseBody
            };
        }
        catch (Exception ex)
        {
            return new SendGridEmailResult
            {
                Success = false,
                Message = "An error occurred while sending email",
                ErrorDetails = ex.Message
            };
        }
    }

    /// <summary>
    /// Sends an email using a SendGrid template without attachment (simplified version)
    /// </summary>
    public async Task<SendGridEmailResult> SendTemplateEmailAsync(
        string toEmail,
        string templateId,
        object templateData,
        string? ccEmail = null)
    {
        return await SendTemplateEmailAsync(toEmail, templateId, templateData, ccEmail, null, null);
    }
}

/// <summary>
/// Result of SendGrid email operation
/// </summary>
public class SendGridEmailResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
}
