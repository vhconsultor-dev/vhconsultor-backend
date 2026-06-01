using BusinessLayer.Corporate.Commands;
using BusinessLayer.Corporate.Queries;
using BusinessLayer.Shared;
using BusinessLayer.Shared.Services;
using ApplicationLayer.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ModelLayer;
using ModelLayer.Corporate.Entities;

namespace ApplicationLayer.Corporate;

public class CampaignService
{
    private readonly CreateCampaignCommand _createCampaignCommand;
    private readonly UpdateCampaignCommand _updateCampaignCommand;
    private readonly UploadCampaignAttachmentsCommand _uploadAttachmentsCommand;
    private readonly AddCampaignProspectsCommand _addProspectsCommand;
    private readonly DeleteCampaignProspectCommand _deleteProspectCommand;
    private readonly CampaignQueryRepository _queryRepository;
    private readonly DBcontext _context;
    private readonly SendGridService _sendGridService;
    private readonly AzureBlobStorageService _blobStorageService;
    private readonly SendGridSettings _sendGridSettings;

    public CampaignService(
        CreateCampaignCommand createCampaignCommand,
        UpdateCampaignCommand updateCampaignCommand,
        UploadCampaignAttachmentsCommand uploadAttachmentsCommand,
        AddCampaignProspectsCommand addProspectsCommand,
        DeleteCampaignProspectCommand deleteProspectCommand,
        CampaignQueryRepository queryRepository,
        DBcontext context,
        SendGridService sendGridService,
        AzureBlobStorageService blobStorageService,
        IOptions<SendGridSettings> sendGridSettings)
    {
        _createCampaignCommand = createCampaignCommand;
        _updateCampaignCommand = updateCampaignCommand;
        _uploadAttachmentsCommand = uploadAttachmentsCommand;
        _addProspectsCommand = addProspectsCommand;
        _deleteProspectCommand = deleteProspectCommand;
        _queryRepository = queryRepository;
        _context = context;
        _sendGridService = sendGridService;
        _blobStorageService = blobStorageService;
        _sendGridSettings = sendGridSettings.Value;
    }

    public Task<IEnumerable<ModelLayer.Corporate.Entities.Campaign>> GetAllAsync(string? status = null)
        => _queryRepository.GetAllAsync(status);

    public Task<ModelLayer.Corporate.Entities.Campaign?> GetByIdAsync(int id)
        => _queryRepository.GetByIdAsync(id);

    public Task<IEnumerable<CampaignAttachment>> GetAttachmentsAsync(int campaignId)
        => _queryRepository.GetAttachmentsAsync(campaignId);

    public Task<IEnumerable<CampaignProspect>> GetProspectsAsync(int campaignId)
        => _queryRepository.GetProspectsAsync(campaignId);

    public Task<int> CreateAsync(CreateCampaignRequest request)
        => _createCampaignCommand.ExecuteAsync(request);

    public Task UpdateAsync(UpdateCampaignRequest request)
        => _updateCampaignCommand.ExecuteAsync(request);

    public Task<int> UploadAttachmentsAsync(UploadCampaignAttachmentsRequest request)
        => _uploadAttachmentsCommand.ExecuteAsync(request);

    public Task<AddCampaignProspectsResponse> AddProspectsAsync(AddCampaignProspectsRequest request)
        => _addProspectsCommand.ExecuteAsync(request);

    public Task<AddCampaignProspectsResponse> AddProspectsFromLeadsAsync(AddCampaignProspectsFromLeadsRequest request)
        => _addProspectsCommand.ExecuteFromLeadsAsync(request);

    public Task DeleteProspectAsync(int campaignId, int prospectId)
        => _deleteProspectCommand.ExecuteAsync(campaignId, prospectId);

    public async Task<SendCampaignResponse> SendAsync(SendCampaignRequest request)
    {
        if (string.IsNullOrWhiteSpace(_sendGridSettings.CampaignTemplateId))
            throw new InvalidOperationException(
                "Campaign email template is not configured. Set SendGrid:CampaignTemplateId.");

        var campaign = await _context.Campaigns
            .FirstOrDefaultAsync(c => c.CampaignId == request.CampaignId);

        if (campaign == null)
            throw new InvalidOperationException($"Campaign with ID {request.CampaignId} not found.");

        UpdateCampaignCommand.EnsurePending(campaign);

        var prospects = await _context.CampaignProspects
            .Where(p => p.CampaignId == request.CampaignId)
            .ToListAsync();

        var recipientMap = new Dictionary<string, CampaignProspect?>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in prospects)
        {
            var email = AddCampaignProspectsCommand.NormalizeEmail(p.Email);
            if (!string.IsNullOrEmpty(email) && !recipientMap.ContainsKey(email))
                recipientMap[email] = p;
        }

        if (request.AdditionalBccEmails != null)
        {
            foreach (var raw in request.AdditionalBccEmails)
            {
                var email = AddCampaignProspectsCommand.NormalizeEmail(raw);
                if (!string.IsNullOrEmpty(email) && !recipientMap.ContainsKey(email))
                    recipientMap[email] = null;
            }
        }

        if (recipientMap.Count == 0)
            throw new InvalidOperationException("No recipients to send. Add prospects or additional BCC emails.");

        var attachments = await _context.CampaignAttachments
            .Where(a => a.CampaignId == request.CampaignId)
            .ToListAsync();

        var attachmentPayload = new List<(byte[] Content, string FileName, string ContentType)>();
        foreach (var att in attachments)
        {
            var (content, contentType) = await _blobStorageService.DownloadFileAsync(att.FileUrl);
            attachmentPayload.Add((content, att.FileName, att.ContentType ?? contentType));
        }

        var sentCount = 0;
        var failedCount = 0;
        var failures = new List<string>();
        var sentAt = DateTime.UtcNow;

        foreach (var (email, prospect) in recipientMap)
        {
            var templateData = new
            {
                subject = campaign.Subject,
                campaignBody = campaign.BodyContent,
                campaignName = campaign.Name,
                recipientFirstName = prospect?.FirstName ?? string.Empty,
                recipientLastName = prospect?.LastName ?? string.Empty
            };

            var result = await _sendGridService.SendTemplateEmailBccAsync(
                email,
                _sendGridSettings.CampaignTemplateId,
                templateData,
                attachmentPayload);

            if (result.Success)
            {
                sentCount++;
                if (prospect != null)
                    prospect.SentAt = sentAt;
            }
            else
            {
                failedCount++;
                failures.Add($"{email}: {result.Message}");
            }
        }

        if (sentCount == 0)
            throw new InvalidOperationException(
                $"Campaign send failed for all recipients. {string.Join(" | ", failures.Take(5))}");

        campaign.Status = "Sent";
        campaign.SentAt = sentAt;
        campaign.UpdatedAt = sentAt;

        await _context.SaveChangesAsync();

        return new SendCampaignResponse
        {
            CampaignId = campaign.CampaignId,
            SentCount = sentCount,
            FailedCount = failedCount,
            TotalRecipients = recipientMap.Count,
            Failures = failures,
            Message = failedCount > 0
                ? $"Campaign sent to {sentCount} recipient(s). {failedCount} failed."
                : $"Campaign sent successfully to {sentCount} recipient(s)."
        };
    }
}
