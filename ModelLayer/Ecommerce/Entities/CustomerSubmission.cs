namespace ModelLayer.Ecommerce.Entities;

public class CustomerSubmission
{
    public int SubmissionID { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public int NumberOfListings { get; set; }
    public string ProductPageLink { get; set; } = string.Empty;
    public string? StoreLink { get; set; }
    public string SelectedPlatform { get; set; } = string.Empty;
    public string? AccountType { get; set; }
    public string? ServiceType { get; set; }
    public string? AnnualSalesRange { get; set; }
    public string? AdvertisingBudgetRange { get; set; }
    public string? PromotionalBudgetRange { get; set; }
    public string? AdditionalDetails { get; set; }
    public DateTime SubmissionDate { get; set; }
    public string SubmissionType { get; set; } = string.Empty;

    // Lead management fields
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public int? ReadByUserId { get; set; }
    public int? CreatedByUserId { get; set; }
    public string? Notes { get; set; }
}

