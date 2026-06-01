namespace ModelLayer.Corporate.Entities;

public class CampaignAttachment
{
    public int CampaignAttachmentId { get; set; }
    public int CampaignId { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public int? UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; }
}
