namespace ModelLayer.Corporate.Entities;

public class Campaign
{
    public int CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyContent { get; set; } = string.Empty;
    /// <summary>Pending | Sent</summary>
    public string Status { get; set; } = "Pending";
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<CampaignAttachment> Attachments { get; set; } = new List<CampaignAttachment>();
    public ICollection<CampaignProspect> Prospects { get; set; } = new List<CampaignProspect>();
}
