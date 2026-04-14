using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Commands;

public class UpdateLeadCommand
{
    private readonly DBcontext _context;

    public UpdateLeadCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task<bool> ExecuteAsync(int submissionId, UpdateLeadRequest request)
    {
        var lead = await _context.CustomerSubmissions.FindAsync(submissionId);
        if (lead == null) return false;

        lead.FirstName = request.FirstName;
        lead.LastName = request.LastName;
        lead.Email = request.Email;
        lead.PhoneNumber = request.PhoneNumber;
        lead.Country = request.Country;
        lead.BrandName = request.BrandName;
        lead.NumberOfListings = request.NumberOfListings;
        lead.ProductPageLink = request.ProductPageLink;
        lead.StoreLink = request.StoreLink;
        lead.SelectedPlatform = request.SelectedPlatform;
        lead.AccountType = request.AccountType;
        lead.ServiceType = request.ServiceType;
        lead.AnnualSalesRange = request.AnnualSalesRange;
        lead.AdvertisingBudgetRange = request.AdvertisingBudgetRange;
        lead.PromotionalBudgetRange = request.PromotionalBudgetRange;
        lead.AdditionalDetails = request.AdditionalDetails;
        lead.Notes = request.Notes;

        await _context.SaveChangesAsync();
        return true;
    }
}

public class UpdateLeadRequest
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
}
