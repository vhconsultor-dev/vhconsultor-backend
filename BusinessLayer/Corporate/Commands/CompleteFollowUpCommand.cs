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
        // 1. Validate follow-up exists
        var followUp = await _context.OpportunityFollowUps
            .FirstOrDefaultAsync(f => f.FollowUpId == request.FollowUpId);

        if (followUp == null)
            throw new InvalidOperationException(
                $"Follow-up with ID {request.FollowUpId} not found.");

        // 2. Validate follow-up is pending
        if (followUp.Status != "Pending")
            throw new InvalidOperationException(
                $"Follow-up with ID {request.FollowUpId} is not in Pending status. Current status: {followUp.Status}.");

        // 3. Validate at least one file is uploaded
        if (request.Files == null || request.Files.Count == 0)
            throw new InvalidOperationException(
                "At least one attachment is required to complete a follow-up.");

        // 4. Validate opportunity exists
        var opportunity = await _context.Opportunities
            .FirstOrDefaultAsync(o => o.OpportunityId == followUp.OpportunityId);

        if (opportunity == null)
            throw new InvalidOperationException(
                $"Opportunity with ID {followUp.OpportunityId} not found.");

        // 5. Upload files to Azure Blob Storage
        var attachments = new List<OpportunityFollowUpAttachment>();

        foreach (var file in request.Files)
        {
            // Validate file
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

            // Generate unique file name
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

            // Upload to Azure Blob
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

            // Create attachment record
            var attachment = new OpportunityFollowUpAttachment
            {
                FollowUpId = request.FollowUpId,
                FileUrl = fileUrl,
                FileName = file.FileName,
                ContentType = file.ContentType,
                UploadedBy = request.CompletedByUserId,
                UploadedAt = DateTime.UtcNow
            };

            attachments.Add(attachment);
        }

        // 6. Update follow-up status
        followUp.Status = "Completed";
        followUp.CompletedAt = DateTime.UtcNow;
        followUp.CompletedByUserId = request.CompletedByUserId;
        followUp.Notes = request.Notes;
        followUp.UpdatedAt = DateTime.UtcNow;

        // 7. Add attachments
        _context.OpportunityFollowUpAttachments.AddRange(attachments);

        await _context.SaveChangesAsync();

        return new CompleteFollowUpResponse
        {
            FollowUpId = followUp.FollowUpId,
            AttachmentCount = attachments.Count,
            Message = "Follow-up completed successfully."
        };
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
}
