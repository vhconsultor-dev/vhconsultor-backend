using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using ModelLayer;
using ModelLayer.Corporate.Entities;
using BusinessLayer.Shared.Services;

namespace BusinessLayer.Corporate.Commands;

public class CompleteFollowUpCommand
{
    private readonly DBcontext _context;
    private readonly AzureBlobStorageService _blobStorageService;

    public CompleteFollowUpCommand(DBcontext context, AzureBlobStorageService blobStorageService)
    {
        _context = context;
        _blobStorageService = blobStorageService;
    }

    public async Task<CompleteFollowUpResponse> ExecuteAsync(CompleteFollowUpRequest request)
    {
        var followUp = await _context.OpportunityFollowUps
            .FirstOrDefaultAsync(f => f.FollowUpId == request.FollowUpId);

        if (followUp == null)
            throw new InvalidOperationException(
                $"Follow-up with ID {request.FollowUpId} not found.");

        if (followUp.Status != "Pending")
            throw new InvalidOperationException(
                $"Follow-up with ID {request.FollowUpId} is not in Pending status. Current status: {followUp.Status}.");

        if (request.Files == null || request.Files.Count == 0)
            throw new InvalidOperationException(
                "At least one attachment is required to complete a follow-up.");

        var opportunity = await _context.Opportunities
            .FirstOrDefaultAsync(o => o.OpportunityId == followUp.OpportunityId);

        if (opportunity == null)
            throw new InvalidOperationException(
                $"Opportunity with ID {followUp.OpportunityId} not found.");

        if (opportunity.Status != "Open")
            throw new InvalidOperationException(
                $"Cannot complete follow-up. Opportunity status is {opportunity.Status}, must be Open.");

        var attachments = new List<OpportunityFollowUpAttachment>();

        foreach (var file in request.Files)
        {
            if (file.Length == 0)
                throw new InvalidOperationException(
                    $"File '{file.FileName}' is empty.");

            if (!_blobStorageService.IsFileExtensionAllowed(file.FileName))
            {
                var allowedExtensions = string.Join(", ", _blobStorageService.GetAllowedExtensions());
                throw new InvalidOperationException(
                    $"File type not allowed. The file '{file.FileName}' has an unsupported extension. Allowed types are: {allowedExtensions}");
            }

            if (!_blobStorageService.IsFileSizeAllowed(file.Length))
            {
                var maxSizeMB = _blobStorageService.GetMaxFileSizeMB();
                var fileSizeMB = Math.Round(file.Length / (1024.0 * 1024.0), 2);
                throw new InvalidOperationException(
                    $"File size exceeds the maximum allowed limit. The file '{file.FileName}' is {fileSizeMB} MB, but the maximum allowed size is {maxSizeMB} MB");
            }

            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

            string fileUrl;
            try
            {
                using (var stream = file.OpenReadStream())
                {
                    fileUrl = await _blobStorageService.UploadFileAsync(
                        stream,
                        uniqueFileName,
                        $"OpportunityFollowUps/{followUp.OpportunityId}/{request.FollowUpId}",
                        file.ContentType);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to upload file '{file.FileName}' to Azure Blob Storage. Error: {ex.Message}", ex);
            }

            attachments.Add(new OpportunityFollowUpAttachment
            {
                FollowUpId = request.FollowUpId,
                FileUrl = fileUrl,
                FileName = file.FileName,
                ContentType = file.ContentType,
                UploadedBy = request.CompletedByUserId,
                UploadedAt = DateTime.UtcNow
            });
        }

        followUp.Status = "Completed";
        followUp.CompletedAt = DateTime.UtcNow;
        followUp.CompletedByUserId = request.CompletedByUserId;
        followUp.Notes = request.Notes;
        followUp.UpdatedAt = DateTime.UtcNow;

        _context.OpportunityFollowUpAttachments.AddRange(attachments);

        OpportunityFollowUp? nextFollowUp = null;
        OpportunityStage? nextStage = null;
        var isLastStageCompleted = false;

        if (followUp.IsRequired && followUp.StageKey == opportunity.CurrentStageKey)
        {
            (nextStage, nextFollowUp, isLastStageCompleted) =
                await CreateNextStageFollowUpAsync(opportunity, followUp.StageKey);
        }

        await _context.SaveChangesAsync();

        var message = "Follow-up completed successfully.";
        if (isLastStageCompleted)
            message += " Final stage completed. Mark the opportunity as Won or Lost.";
        else if (nextStage != null && nextFollowUp != null)
            message += $" Next stage '{nextStage.DisplayName}' assigned with due date {nextFollowUp.DueAt:yyyy-MM-dd} UTC.";

        return new CompleteFollowUpResponse
        {
            FollowUpId = followUp.FollowUpId,
            AttachmentCount = attachments.Count,
            Message = message,
            NextStageKey = nextStage?.StageKey,
            NextStageDisplayName = nextStage?.DisplayName,
            NextFollowUpId = nextFollowUp?.FollowUpId,
            NextFollowUpDueAt = nextFollowUp?.DueAt,
            IsLastStageCompleted = isLastStageCompleted
        };
    }

    /// <summary>
    /// Advances opportunity to the next catalog stage and creates a required pending follow-up.
    /// </summary>
    private async Task<(OpportunityStage? nextStage, OpportunityFollowUp? nextFollowUp, bool isLastStage)> CreateNextStageFollowUpAsync(
        Opportunity opportunity,
        string completedStageKey)
    {
        var stages = await _context.OpportunityStages
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        var currentIndex = stages.FindIndex(s => s.StageKey == completedStageKey);
        if (currentIndex < 0 || currentIndex >= stages.Count - 1)
            return (null, null, currentIndex >= 0 && currentIndex == stages.Count - 1);

        var nextStage = stages[currentIndex + 1];

        opportunity.CurrentStageKey = nextStage.StageKey;
        opportunity.UpdatedAt = DateTime.UtcNow;

        var hasPendingRequired = await _context.OpportunityFollowUps.AnyAsync(f =>
            f.OpportunityId == opportunity.OpportunityId
            && f.StageKey == nextStage.StageKey
            && f.Status == "Pending"
            && f.IsRequired);

        if (hasPendingRequired)
            return (nextStage, null, false);

        var nextFollowUp = new OpportunityFollowUp
        {
            OpportunityId = opportunity.OpportunityId,
            StageKey = nextStage.StageKey,
            Status = "Pending",
            DueAt = OpportunityFollowUpDueDateCalculator.GetDueAt(nextStage.StageKey),
            IsRequired = true,
            Notes = nextStage.DefaultChecklistText,
            CreatedAt = DateTime.UtcNow
        };

        _context.OpportunityFollowUps.Add(nextFollowUp);
        return (nextStage, nextFollowUp, false);
    }
}

public class CompleteFollowUpRequest
{
    public int FollowUpId { get; set; }
    public string? Notes { get; set; }
    public int CompletedByUserId { get; set; }
    public List<IFormFile> Files { get; set; } = new List<IFormFile>();
}

public class CompleteFollowUpResponse
{
    public int FollowUpId { get; set; }
    public int AttachmentCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? NextStageKey { get; set; }
    public string? NextStageDisplayName { get; set; }
    public int? NextFollowUpId { get; set; }
    public DateTime? NextFollowUpDueAt { get; set; }
    public bool IsLastStageCompleted { get; set; }
}
