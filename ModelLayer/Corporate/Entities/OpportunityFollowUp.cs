namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Follow-up activities for opportunity stages (with mandatory evidence attachments)
/// </summary>
public class OpportunityFollowUp
{
    public int FollowUpId { get; set; }
    public int OpportunityId { get; set; }
    public string StageKey { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Completed, Cancelled
    public DateTime? DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? CompletedByUserId { get; set; }
    public string? Notes { get; set; }
    public bool IsRequired { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
