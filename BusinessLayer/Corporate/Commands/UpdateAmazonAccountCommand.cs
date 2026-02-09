using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command to update an existing Amazon account.
/// </summary>
public class UpdateAmazonAccountCommand
{
    private readonly DBcontext _context;

    public UpdateAmazonAccountCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Updates an Amazon account by ID. Validates that CustomerId exists if changed.
    /// </summary>
    public async Task<bool> ExecuteAsync(int amazonAccountId, UpdateAmazonAccountRequest request)
    {
        var entity = await _context.AmazonAccounts.FindAsync(amazonAccountId);
        if (entity == null)
            return false;

        var customerExists = await _context.Customers.AnyAsync(c => c.CustomerId == request.CustomerId);
        if (!customerExists)
        {
            throw new InvalidOperationException(
                $"Customer with ID {request.CustomerId} was not found. Cannot assign Amazon account to a non-existent customer.");
        }

        entity.CustomerId = request.CustomerId;
        entity.AmazonAccountIdentifier = request.AmazonAccountIdentifier.Trim();
        entity.IsSeller = request.IsSeller;
        entity.IsVendor = request.IsVendor;
        entity.RefreshToken = request.RefreshToken.Trim();
        entity.AmazonRegion = request.AmazonRegion.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTimeService.GetCostaRicaNow();

        await _context.SaveChangesAsync();
        return true;
    }
}

/// <summary>
/// Request to update an Amazon account.
/// </summary>
public class UpdateAmazonAccountRequest
{
    public int CustomerId { get; set; }
    public string AmazonAccountIdentifier { get; set; } = string.Empty;
    public bool IsSeller { get; set; }
    public bool IsVendor { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public string AmazonRegion { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
