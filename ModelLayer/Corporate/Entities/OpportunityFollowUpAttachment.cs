namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Attachments for follow-up evidence (stored in Azure Blob Storage)
/// </summary>
public class OpportunityFollowUpAttachment
{
    public int FollowUpAttachmentId { get; set; }
    public int FollowUpId { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public int UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
}
