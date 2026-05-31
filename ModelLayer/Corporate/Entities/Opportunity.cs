namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Opportunity entity - represents a lead that has been converted to a formal sales opportunity
/// </summary>
public class Opportunity
{
    // Keys
    public int OpportunityId { get; set; }
    public int? SubmissionId { get; set; }
    
    // Status & Stage
    public string Status { get; set; } = "Open"; // Open, Won, Lost
    public string? CurrentStageKey { get; set; }
    public string Title { get; set; } = string.Empty;
    
    // Denormalized contact data (from lead)
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public int NumberOfListings { get; set; }
    public string? ProductPageLink { get; set; }
    public string? StoreLink { get; set; }
    public string? SelectedPlatform { get; set; }
    public string? AccountType { get; set; }
    public string? ServiceType { get; set; }
    public string? AnnualSalesRange { get; set; }
    public string? AdvertisingBudgetRange { get; set; }
    public string? PromotionalBudgetRange { get; set; }
    public string? AdditionalDetails { get; set; }
    
    // Assignment
    public int AssignedToUserId { get; set; }
    public int? ViewerUserId { get; set; }
    
    // Won (Phase 3)
    public int? CustomerId { get; set; }
    public int? ContractId { get; set; }
    public DateTime? WonAt { get; set; }
    public int? WonByUserId { get; set; }
    
    // Lost (Phase 3)
    public DateTime? LostAt { get; set; }
    public int? LostByUserId { get; set; }
    public string? LostReasonKey { get; set; }
    public string? LostReasonNotes { get; set; }
    
    // Audit
    public DateTime ConvertedAt { get; set; }
    public int? ConvertedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
