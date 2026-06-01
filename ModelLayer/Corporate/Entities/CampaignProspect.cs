namespace ModelLayer.Corporate.Entities;

public class CampaignProspect
{
    public int CampaignProspectId { get; set; }
    public int CampaignId { get; set; }
    public string Email { get; set; } = string.Empty;
    public int? LeadId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
