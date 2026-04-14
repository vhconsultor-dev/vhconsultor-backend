using ApplicationLayer.Shared;
using BusinessLayer.Ecommerce.Commands;
using BusinessLayer.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelLayer.Shared;

namespace ApplicationLayer.Ecommerce;

public class CustomerSubmissionService
{
    private readonly CreateCustomerSubmissionCommand _createCustomerSubmissionCommand;
    private readonly SendGridService _sendGridService;
    private readonly IOptions<SendGridSettings> _sendGridSettings;
    private readonly ILogger<CustomerSubmissionService> _logger;

    public CustomerSubmissionService(
        CreateCustomerSubmissionCommand createCustomerSubmissionCommand,
        SendGridService sendGridService,
        IOptions<SendGridSettings> sendGridSettings,
        ILogger<CustomerSubmissionService> logger)
    {
        _createCustomerSubmissionCommand = createCustomerSubmissionCommand;
        _sendGridService = sendGridService;
        _sendGridSettings = sendGridSettings;
        _logger = logger;
    }

    #region Commands

    public async Task<int> CreateCustomerSubmissionAsync(CreateCustomerSubmissionRequest request)
    {
        var submissionId = await _createCustomerSubmissionCommand.ExecuteAsync(request);

        await SendContactFormEmailAsync(request, submissionId);

        return submissionId;
    }

    #endregion

    #region Private Helpers

    private async Task SendContactFormEmailAsync(CreateCustomerSubmissionRequest request, int submissionId)
    {
        var templateId = _sendGridSettings.Value.ContactFormTemplateId;
        var notificationEmail = _sendGridSettings.Value.ContactFormNotificationEmail;

        if (string.IsNullOrWhiteSpace(templateId) || string.IsNullOrWhiteSpace(notificationEmail))
        {
            _logger.LogWarning(
                "Contact form email not sent for submission {SubmissionId}: ContactFormTemplateId or ContactFormNotificationEmail is not configured.",
                submissionId);
            return;
        }

        var submittedAt = DateTimeService.GetCostaRicaNow().ToString("yyyy-MM-dd hh:mm tt");
        var leadId = $"LEAD-{DateTimeService.GetCostaRicaNow():yyyyMMdd}-{submissionId:D3}";

        var templateData = new Dictionary<string, object>
        {
            { "submittedAt", submittedAt },
            { "source", "BrandPartner Website" },
            { "firstName", request.FirstName },
            { "lastName", request.LastName },
            { "email", request.Email },
            { "phone", request.PhoneNumber },
            { "country", request.Country },
            { "brandName", request.BrandName },
            { "numberOfListings", request.NumberOfListings.ToString() },
            { "productPageUrl", request.ProductPageLink },
            { "storeUrl", request.StoreLink ?? "" },
            { "platform", FormatPlatform(request.SelectedPlatform) },
            { "accountType", FormatAccountType(request.AccountType) },
            { "serviceType", FormatServiceType(request.ServiceType) },
            { "annualSalesRange", request.AnnualSalesRange ?? "" },
            { "advertisingBudgetRange", request.AdvertisingBudgetRange ?? "" },
            { "promotionalBudgetRange", request.PromotionalBudgetRange ?? "" },
            { "additionalDetails", request.AdditionalDetails ?? "" },
            { "leadId", leadId }
        };

        try
        {
            var result = await _sendGridService.SendTemplateEmailAsync(
                notificationEmail,
                templateId,
                templateData,
                ccEmail: null);

            if (!result.Success)
            {
                _logger.LogError(
                    "Failed to send contact form email for submission {SubmissionId}. Message: {Message}. Details: {Details}",
                    submissionId, result.Message, result.ErrorDetails);
            }
            else
            {
                _logger.LogInformation(
                    "Contact form email sent successfully for submission {SubmissionId}.",
                    submissionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error sending contact form email for submission {SubmissionId}.",
                submissionId);
        }
    }

    private static string FormatPlatform(string platform) => platform.ToLower() switch
    {
        "amazon" => "Amazon",
        "walmart" => "Walmart",
        "tiktok" => "TikTok",
        _ => platform
    };

    private static string FormatAccountType(string? accountType) => accountType?.ToLower() switch
    {
        "seller-central" => "Seller Central (3P)",
        "vendor-central" => "Vendor Central (1P)",
        "supplier-center" => "Supplier Center (1P)",
        "seller-center" => "Seller Center (3P)",
        _ => accountType ?? ""
    };

    private static string FormatServiceType(string? serviceType) => serviceType?.ToLower() switch
    {
        "full-service" => "Full Service",
        "advertising-only" => "Advertising Only",
        _ => serviceType ?? ""
    };

    #endregion
}
