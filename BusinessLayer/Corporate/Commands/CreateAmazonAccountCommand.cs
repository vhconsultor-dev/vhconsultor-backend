using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command to create a new Amazon account.
/// </summary>
public class CreateAmazonAccountCommand
{
    private readonly DBcontext _context;

    public CreateAmazonAccountCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates a new Amazon account. Validates that CustomerId exists.
    /// </summary>
    public async Task<int> ExecuteAsync(CreateAmazonAccountRequest request)
    {
        var customerExists = await _context.Customers.AnyAsync(c => c.CustomerId == request.CustomerId);
        if (!customerExists)
        {
            throw new InvalidOperationException(
                $"Customer with ID {request.CustomerId} was not found. Cannot create Amazon account for a non-existent customer.");
        }

        var entity = new AmazonAccount
        {
            CustomerId = request.CustomerId,
            AmazonAccountIdentifier = request.AmazonAccountIdentifier.Trim(),
            IsSeller = request.IsSeller,
            IsVendor = request.IsVendor,
            RefreshToken = request.RefreshToken.Trim(),
            AmazonRegion = request.AmazonRegion.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTimeService.GetCostaRicaNow()
        };

        _context.AmazonAccounts.Add(entity);
        await _context.SaveChangesAsync();
        return entity.AmazonAccountId;
    }
}

/// <summary>
/// Request to create an Amazon account.
/// </summary>
public class CreateAmazonAccountRequest
{
    public int CustomerId { get; set; }
    public string AmazonAccountIdentifier { get; set; } = string.Empty;
    public bool IsSeller { get; set; }
    public bool IsVendor { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public string AmazonRegion { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
