using ModelLayer;
using ModelLayer.Ecommerce.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Commands;

public class CreateLeadCommand
{
    private readonly DBcontext _context;

    public CreateLeadCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task<int> ExecuteAsync(CreateLeadRequest request)
    {
        var lead = new CustomerSubmission
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Country = request.Country,
            BrandName = request.BrandName,
            NumberOfListings = request.NumberOfListings,
            ProductPageLink = request.ProductPageLink,
            StoreLink = request.StoreLink,
            SelectedPlatform = request.SelectedPlatform,
            AccountType = request.AccountType,
            ServiceType = request.ServiceType,
            AnnualSalesRange = request.AnnualSalesRange,
            AdvertisingBudgetRange = request.AdvertisingBudgetRange,
            PromotionalBudgetRange = request.PromotionalBudgetRange,
            AdditionalDetails = request.AdditionalDetails,
            SubmissionDate = DateTimeService.GetCostaRicaNow(),
            SubmissionType = "corporate_manual",
            IsRead = false,
            CreatedByUserId = request.CreatedByUserId,
            Notes = request.Notes
        };

        _context.CustomerSubmissions.Add(lead);
        await _context.SaveChangesAsync();

        return lead.SubmissionID;
    }
}

public class CreateLeadRequest
{
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
    public string? Notes { get; set; }
    public int? CreatedByUserId { get; set; }
}
