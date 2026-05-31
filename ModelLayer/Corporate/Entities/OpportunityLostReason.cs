namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Catalog of reasons why an opportunity was lost
/// </summary>
public class OpportunityLostReason
{
    public int OpportunityLostReasonId { get; set; }
    public string ReasonKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
