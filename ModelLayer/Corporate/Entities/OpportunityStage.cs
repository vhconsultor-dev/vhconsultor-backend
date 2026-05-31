namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Catalog of opportunity stages (First contact, Proposal sent, etc.)
/// </summary>
public class OpportunityStage
{
    public int OpportunityStageId { get; set; }
    public string StageKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? DefaultChecklistText { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
