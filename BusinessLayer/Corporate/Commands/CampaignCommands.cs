using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using ModelLayer;
using ModelLayer.Corporate.Entities;
using BusinessLayer.Shared.Services;

namespace BusinessLayer.Corporate.Commands;

public class CreateCampaignCommand
{
    private readonly DBcontext _context;

    public CreateCampaignCommand(DBcontext context) => _context = context;

    public async Task<int> ExecuteAsync(CreateCampaignRequest request)
    {
        var campaign = new Campaign
        {
            Name = request.Name.Trim(),
            Subject = request.Subject.Trim(),
            BodyContent = request.BodyContent.Trim(),
            Status = "Pending",
            ScheduledAt = request.ScheduledAt,
            CreatedByUserId = request.CreatedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Campaigns.Add(campaign);
        await _context.SaveChangesAsync();
        return campaign.CampaignId;
    }
}

public class UpdateCampaignCommand
{
    private readonly DBcontext _context;

    public UpdateCampaignCommand(DBcontext context) => _context = context;

    public async Task ExecuteAsync(UpdateCampaignRequest request)
    {
        var campaign = await _context.Campaigns.FirstOrDefaultAsync(c => c.CampaignId == request.CampaignId);
        if (campaign == null)
            throw new InvalidOperationException($"Campaign with ID {request.CampaignId} not found.");

        EnsurePending(campaign);

        campaign.Name = request.Name.Trim();
        campaign.Subject = request.Subject.Trim();
        campaign.BodyContent = request.BodyContent.Trim();
        campaign.ScheduledAt = request.ScheduledAt;
        campaign.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public static void EnsurePending(Campaign campaign)
    {
        if (campaign.Status != "Pending")
            throw new InvalidOperationException($"Campaign {campaign.CampaignId} cannot be modified. Status is '{campaign.Status}'.");
    }
}

public class UploadCampaignAttachmentsCommand
{
    private readonly DBcontext _context;
    private readonly AzureBlobStorageService _blobStorageService;

    public UploadCampaignAttachmentsCommand(DBcontext context, AzureBlobStorageService blobStorageService)
    {
        _context = context;
        _blobStorageService = blobStorageService;
    }

    public async Task<int> ExecuteAsync(UploadCampaignAttachmentsRequest request)
    {
        var campaign = await _context.Campaigns.FirstOrDefaultAsync(c => c.CampaignId == request.CampaignId);
        if (campaign == null)
            throw new InvalidOperationException($"Campaign with ID {request.CampaignId} not found.");

        UpdateCampaignCommand.EnsurePending(campaign);

        if (request.Files == null || request.Files.Count == 0)
            throw new InvalidOperationException("At least one file is required.");

        var count = 0;
        foreach (var file in request.Files)
        {
            ValidateFile(file);

            var ext = Path.GetExtension(file.FileName);
            var uniqueName = $"{Guid.NewGuid()}{ext}";

            string fileUrl;
            using (var stream = file.OpenReadStream())
            {
                fileUrl = await _blobStorageService.UploadFileAsync(
                    stream, uniqueName, $"Campaigns/{request.CampaignId}", file.ContentType);
            }

            _context.CampaignAttachments.Add(new CampaignAttachment
            {
                CampaignId = request.CampaignId,
                FileUrl = fileUrl,
                FileName = file.FileName,
                ContentType = file.ContentType,
                UploadedByUserId = request.UploadedByUserId,
                UploadedAt = DateTime.UtcNow
            });
            count++;
        }

        campaign.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return count;
    }

    private void ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
            throw new InvalidOperationException($"File '{file.FileName}' is empty.");

        if (!_blobStorageService.IsFileExtensionAllowed(file.FileName))
        {
            var allowed = string.Join(", ", _blobStorageService.GetAllowedExtensions());
            throw new InvalidOperationException(
                $"File type not allowed for '{file.FileName}'. Allowed: {allowed}");
        }

        if (!_blobStorageService.IsFileSizeAllowed(file.Length))
        {
            var maxMb = _blobStorageService.GetMaxFileSizeMB();
            throw new InvalidOperationException(
                $"File '{file.FileName}' exceeds maximum size of {maxMb} MB.");
        }
    }
}

public class AddCampaignProspectsCommand
{
    private readonly DBcontext _context;

    public AddCampaignProspectsCommand(DBcontext context) => _context = context;

    public async Task<AddCampaignProspectsResponse> ExecuteAsync(AddCampaignProspectsRequest request)
    {
        var campaign = await _context.Campaigns.FirstOrDefaultAsync(c => c.CampaignId == request.CampaignId);
        if (campaign == null)
            throw new InvalidOperationException($"Campaign with ID {request.CampaignId} not found.");

        UpdateCampaignCommand.EnsurePending(campaign);

        var existingEmails = await _context.CampaignProspects
            .Where(p => p.CampaignId == request.CampaignId)
            .Select(p => p.Email.ToLower())
            .ToListAsync();

        var existingSet = existingEmails.ToHashSet();
        var added = 0;
        var skipped = 0;

        foreach (var prospect in request.Prospects)
        {
            var email = NormalizeEmail(prospect.Email);
            if (string.IsNullOrEmpty(email))
            {
                skipped++;
                continue;
            }

            if (existingSet.Contains(email))
            {
                skipped++;
                continue;
            }

            _context.CampaignProspects.Add(new CampaignProspect
            {
                CampaignId = request.CampaignId,
                Email = email,
                LeadId = prospect.LeadId,
                FirstName = prospect.FirstName?.Trim(),
                LastName = prospect.LastName?.Trim(),
                CreatedAt = DateTime.UtcNow
            });

            existingSet.Add(email);
            added++;
        }

        if (added > 0)
        {
            campaign.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return new AddCampaignProspectsResponse { AddedCount = added, SkippedCount = skipped };
    }

    public async Task<AddCampaignProspectsResponse> ExecuteFromLeadsAsync(AddCampaignProspectsFromLeadsRequest request)
    {
        var leads = await _context.CustomerSubmissions
            .AsNoTracking()
            .Where(l => request.LeadIds.Contains(l.SubmissionID))
            .ToListAsync();

        var prospects = leads
            .Where(l => !string.IsNullOrWhiteSpace(l.Email))
            .Select(l => new CampaignProspectInput
            {
                Email = l.Email,
                LeadId = l.SubmissionID,
                FirstName = l.FirstName,
                LastName = l.LastName
            })
            .ToList();

        return await ExecuteAsync(new AddCampaignProspectsRequest
        {
            CampaignId = request.CampaignId,
            Prospects = prospects
        });
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}

public class DeleteCampaignProspectCommand
{
    private readonly DBcontext _context;

    public DeleteCampaignProspectCommand(DBcontext context) => _context = context;

    public async Task ExecuteAsync(int campaignId, int prospectId)
    {
        var campaign = await _context.Campaigns.FirstOrDefaultAsync(c => c.CampaignId == campaignId);
        if (campaign == null)
            throw new InvalidOperationException($"Campaign with ID {campaignId} not found.");

        UpdateCampaignCommand.EnsurePending(campaign);

        var prospect = await _context.CampaignProspects
            .FirstOrDefaultAsync(p => p.CampaignProspectId == prospectId && p.CampaignId == campaignId);

        if (prospect == null)
            throw new InvalidOperationException($"Prospect with ID {prospectId} not found in campaign {campaignId}.");

        _context.CampaignProspects.Remove(prospect);
        campaign.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}

#region Request/Response DTOs

public class CreateCampaignRequest
{
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyContent { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public int? CreatedByUserId { get; set; }
}

public class UpdateCampaignRequest : CreateCampaignRequest
{
    public int CampaignId { get; set; }
}

public class UploadCampaignAttachmentsRequest
{
    public int CampaignId { get; set; }
    public int? UploadedByUserId { get; set; }
    public List<IFormFile> Files { get; set; } = new();
}

public class CampaignProspectInput
{
    public string Email { get; set; } = string.Empty;
    public int? LeadId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public class AddCampaignProspectsRequest
{
    public int CampaignId { get; set; }
    public List<CampaignProspectInput> Prospects { get; set; } = new();
}

public class AddCampaignProspectsFromLeadsRequest
{
    public int CampaignId { get; set; }
    public List<int> LeadIds { get; set; } = new();
}

public class AddCampaignProspectsResponse
{
    public int AddedCount { get; set; }
    public int SkippedCount { get; set; }
}

public class SendCampaignRequest
{
    public int CampaignId { get; set; }
    public List<string>? AdditionalBccEmails { get; set; }
}

public class SendCampaignResponse
{
    public int CampaignId { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public int TotalRecipients { get; set; }
    public List<string> Failures { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

#endregion
