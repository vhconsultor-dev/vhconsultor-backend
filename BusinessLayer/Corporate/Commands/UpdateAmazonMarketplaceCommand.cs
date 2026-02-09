using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Corporate.Entities;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command to update an existing Amazon marketplace.
/// </summary>
public class UpdateAmazonMarketplaceCommand
{
    private readonly DBcontext _context;

    public UpdateAmazonMarketplaceCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Updates an Amazon marketplace by ID.
    /// </summary>
    /// <param name="amazonMarketplaceId">ID of the marketplace to update.</param>
    /// <param name="request">Updated data.</param>
    /// <returns>True if updated, false if not found.</returns>
    public async Task<bool> ExecuteAsync(int amazonMarketplaceId, UpdateAmazonMarketplaceRequest request)
    {
        var entity = await _context.AmazonMarketplaces.FindAsync(amazonMarketplaceId);
        if (entity == null)
            return false;

        entity.AmazonMarketplaceCode = request.AmazonMarketplaceCode.Trim();
        entity.CountryCode = request.CountryCode.Trim();
        entity.CountryName = request.CountryName.Trim();
        entity.AmazonRegion = request.AmazonRegion.Trim();
        entity.CurrencyCode = request.CurrencyCode.Trim();
        entity.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return true;
    }
}

/// <summary>
/// Request to update an Amazon marketplace.
/// </summary>
public class UpdateAmazonMarketplaceRequest
{
    public string AmazonMarketplaceCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string AmazonRegion { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
