using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command to update an existing Amazon account–marketplace association.
/// </summary>
public class UpdateAmazonAccountMarketplaceCommand
{
    private readonly DBcontext _context;

    public UpdateAmazonAccountMarketplaceCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Updates an association by ID. Only IsPrimary and IsActive are updated.
    /// </summary>
    public async Task<bool> ExecuteAsync(int amazonAccountMarketplaceId, UpdateAmazonAccountMarketplaceRequest request)
    {
        var entity = await _context.AmazonAccountMarketplaces.FindAsync(amazonAccountMarketplaceId);
        if (entity == null)
            return false;

        entity.IsPrimary = request.IsPrimary;
        entity.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return true;
    }
}

/// <summary>
/// Request to update an Amazon account–marketplace association.
/// </summary>
public class UpdateAmazonAccountMarketplaceRequest
{
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
}
