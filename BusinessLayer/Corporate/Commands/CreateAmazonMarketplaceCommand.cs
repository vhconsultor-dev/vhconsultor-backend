using ModelLayer;
using ModelLayer.Corporate.Entities;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command to create a new Amazon marketplace.
/// </summary>
public class CreateAmazonMarketplaceCommand
{
    private readonly DBcontext _context;

    public CreateAmazonMarketplaceCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates a new Amazon marketplace.
    /// </summary>
    /// <param name="request">Marketplace data.</param>
    /// <returns>ID of the created marketplace.</returns>
    public async Task<int> ExecuteAsync(CreateAmazonMarketplaceRequest request)
    {
        var entity = new AmazonMarketplace
        {
            AmazonMarketplaceCode = request.AmazonMarketplaceCode.Trim(),
            CountryCode = request.CountryCode.Trim(),
            CountryName = request.CountryName.Trim(),
            AmazonRegion = request.AmazonRegion.Trim(),
            CurrencyCode = request.CurrencyCode.Trim(),
            IsActive = request.IsActive
        };

        _context.AmazonMarketplaces.Add(entity);
        await _context.SaveChangesAsync();
        return entity.AmazonMarketplaceId;
    }
}

/// <summary>
/// Request to create an Amazon marketplace.
/// </summary>
public class CreateAmazonMarketplaceRequest
{
    public string AmazonMarketplaceCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string AmazonRegion { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
