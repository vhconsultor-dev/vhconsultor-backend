using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Corporate.Entities;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command to delete an Amazon account–marketplace association.
/// </summary>
public class DeleteAmazonAccountMarketplaceCommand
{
    private readonly DBcontext _context;

    public DeleteAmazonAccountMarketplaceCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Deletes an association by ID. Returns true if deleted, false if not found.
    /// </summary>
    public async Task<bool> ExecuteAsync(int amazonAccountMarketplaceId)
    {
        var entity = await _context.AmazonAccountMarketplaces.FindAsync(amazonAccountMarketplaceId);
        if (entity == null)
            return false;

        _context.AmazonAccountMarketplaces.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }
}
