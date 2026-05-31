namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Optional attachments for opportunity comments
/// </summary>
public class OpportunityCommentAttachment
{
    public int CommentAttachmentId { get; set; }
    public int CommentId { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public int UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
}
