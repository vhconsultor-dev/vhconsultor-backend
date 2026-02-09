using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command to create a new Amazon account–marketplace association.
/// </summary>
public class CreateAmazonAccountMarketplaceCommand
{
    private readonly DBcontext _context;

    public CreateAmazonAccountMarketplaceCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates a new association. Validates that AmazonAccountId and AmazonMarketplaceId exist
    /// and that the pair is not already linked.
    /// </summary>
    public async Task<int> ExecuteAsync(CreateAmazonAccountMarketplaceRequest request)
    {
        var accountExists = await _context.AmazonAccounts.AnyAsync(a => a.AmazonAccountId == request.AmazonAccountId);
        if (!accountExists)
        {
            throw new InvalidOperationException(
                $"Amazon account with ID {request.AmazonAccountId} was not found. Cannot link marketplace to a non-existent account.");
        }

        var marketplaceExists = await _context.AmazonMarketplaces.AnyAsync(m => m.AmazonMarketplaceId == request.AmazonMarketplaceId);
        if (!marketplaceExists)
        {
            throw new InvalidOperationException(
                $"Amazon marketplace with ID {request.AmazonMarketplaceId} was not found. Cannot link account to a non-existent marketplace.");
        }

        var duplicateExists = await _context.AmazonAccountMarketplaces.AnyAsync(x =>
            x.AmazonAccountId == request.AmazonAccountId && x.AmazonMarketplaceId == request.AmazonMarketplaceId);
        if (duplicateExists)
        {
            throw new InvalidOperationException(
                $"This Amazon account is already linked to the selected marketplace. Amazon account ID {request.AmazonAccountId} and marketplace ID {request.AmazonMarketplaceId} association already exists.");
        }

        var entity = new AmazonAccountMarketplace
        {
            AmazonAccountId = request.AmazonAccountId,
            AmazonMarketplaceId = request.AmazonMarketplaceId,
            IsPrimary = request.IsPrimary,
            IsActive = request.IsActive,
            CreatedAt = DateTimeService.GetCostaRicaNow()
        };

        _context.AmazonAccountMarketplaces.Add(entity);
        await _context.SaveChangesAsync();
        return entity.AmazonAccountMarketplaceId;
    }
}

/// <summary>
/// Request to create an Amazon account–marketplace association.
/// </summary>
public class CreateAmazonAccountMarketplaceRequest
{
    public int AmazonAccountId { get; set; }
    public int AmazonMarketplaceId { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
}
