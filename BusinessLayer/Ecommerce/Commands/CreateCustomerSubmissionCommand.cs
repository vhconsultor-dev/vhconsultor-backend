using ModelLayer;
using ModelLayer.Ecommerce.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Ecommerce.Commands;

/// <summary>
/// Command para crear una nueva CustomerSubmission usando Entity Framework
/// </summary>
public class CreateCustomerSubmissionCommand
{
    private readonly DBcontext _context;

    public CreateCustomerSubmissionCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ejecuta la creación de una nueva Customer Submission
    /// </summary>
    /// <param name="request">Datos de la submission</param>
    /// <returns>ID de la submission creada</returns>
    public async Task<int> ExecuteAsync(CreateCustomerSubmissionRequest request)
    {
        var submission = new CustomerSubmission
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
            ServiceType = request.ServiceType,
            AnnualSalesRange = request.AnnualSalesRange,
            AdvertisingBudgetRange = request.AdvertisingBudgetRange,
            PromotionalBudgetRange = request.PromotionalBudgetRange,
            AdditionalDetails = request.AdditionalDetails,
            SubmissionDate = DateTimeService.GetCostaRicaNow(),
            SubmissionType = request.SubmissionType
        };

        _context.CustomerSubmissions.Add(submission);
        await _context.SaveChangesAsync();
        
        return submission.SubmissionID;
    }
}

/// <summary>
/// Request para crear una Customer Submission
/// </summary>
public class CreateCustomerSubmissionRequest
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
    public string? ServiceType { get; set; }
    public string? AnnualSalesRange { get; set; }
    public string? AdvertisingBudgetRange { get; set; }
    public string? PromotionalBudgetRange { get; set; }
    public string? AdditionalDetails { get; set; }
    public string SubmissionType { get; set; } = string.Empty;
}

