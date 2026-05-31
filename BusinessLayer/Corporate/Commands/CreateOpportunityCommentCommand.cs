using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using ModelLayer;
using ModelLayer.Corporate.Entities;
using BusinessLayer.Shared.Services;
using System.Text.RegularExpressions;

namespace BusinessLayer.Corporate.Commands;

public class CreateOpportunityCommentCommand
{
    private readonly DBcontext _context;
    private readonly AzureBlobStorageService _blobStorageService;

    public CreateOpportunityCommentCommand(
        DBcontext context,
        AzureBlobStorageService blobStorageService)
    {
        _context = context;
        _blobStorageService = blobStorageService;
    }

    public async Task<CreateOpportunityCommentResponse> ExecuteAsync(CreateOpportunityCommentRequest request)
    {
        // 1. Validate opportunity exists
        var opportunity = await _context.Opportunities
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OpportunityId == request.OpportunityId);

        if (opportunity == null)
            throw new InvalidOperationException(
                $"Opportunity with ID {request.OpportunityId} not found.");

        // 2. Validate author exists
        var author = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == request.AuthorUserId);

        if (author == null)
            throw new InvalidOperationException(
                $"User with ID {request.AuthorUserId} not found.");

        if (!author.IsCorporate)
            throw new InvalidOperationException(
                $"User with ID {request.AuthorUserId} is not a corporate user.");

        if (!author.IsActive)
            throw new InvalidOperationException(
                $"User with ID {request.AuthorUserId} is inactive.");

        // 3. Create comment
        var comment = new OpportunityComment
        {
            OpportunityId = request.OpportunityId,
            AuthorUserId = request.AuthorUserId,
            Body = request.Body,
            CreatedAt = DateTime.UtcNow
        };

        _context.OpportunityComments.Add(comment);
        await _context.SaveChangesAsync();

        // 4. Parse and save mentions
        var mentionedUserIds = ParseMentions(request.Body);
        var mentions = new List<OpportunityCommentMention>();

        foreach (var userId in mentionedUserIds.Distinct())
        {
            // Validate mentioned user exists and is corporate
            var mentionedUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (mentionedUser != null && mentionedUser.IsCorporate && mentionedUser.IsActive)
            {
                var mention = new OpportunityCommentMention
                {
                    CommentId = comment.CommentId,
                    MentionedUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                mentions.Add(mention);
            }
        }

        if (mentions.Any())
        {
            _context.OpportunityCommentMentions.AddRange(mentions);
            await _context.SaveChangesAsync();
        }

        // 5. Upload attachments if provided
        var attachments = new List<OpportunityCommentAttachment>();

        if (request.Files != null && request.Files.Count > 0)
        {
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
                            $"OpportunityComments/{request.OpportunityId}/{comment.CommentId}",
                            file.ContentType);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to upload file '{file.FileName}' to Azure Blob Storage. Error: {ex.Message}", ex);
                }

                // Create attachment record
                var attachment = new OpportunityCommentAttachment
                {
                    CommentId = comment.CommentId,
                    FileUrl = fileUrl,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    UploadedBy = request.AuthorUserId,
                    UploadedAt = DateTime.UtcNow
                };

                attachments.Add(attachment);
            }

            if (attachments.Any())
            {
                _context.OpportunityCommentAttachments.AddRange(attachments);
                await _context.SaveChangesAsync();
            }
        }

        return new CreateOpportunityCommentResponse
        {
            CommentId = comment.CommentId,
            MentionedUserIds = mentions.Select(m => m.MentionedUserId).ToList(),
            AttachmentCount = attachments.Count,
            Message = "Comment created successfully."
        };
    }

    private List<int> ParseMentions(string body)
    {
        var mentions = new List<int>();
        
        // Pattern: @userId (e.g., @123)
        var pattern = @"@(\d+)";
        var matches = Regex.Matches(body, pattern);

        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1 && int.TryParse(match.Groups[1].Value, out var userId))
            {
                mentions.Add(userId);
            }
        }

        return mentions;
    }
}

public class CreateOpportunityCommentRequest
{
    public int OpportunityId { get; set; }
    public int AuthorUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public List<IFormFile>? Files { get; set; }
}

public class CreateOpportunityCommentResponse
{
    public int CommentId { get; set; }
    public List<int> MentionedUserIds { get; set; } = new List<int>();
    public int AttachmentCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
