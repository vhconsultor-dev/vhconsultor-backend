using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.BrandPartner.Entities;

namespace BusinessLayer.BrandPartner.Queries;

/// <summary>
/// Repository de queries para usuarios Brand Partner
/// </summary>
public class BrandPartnerUserQueryRepository
{
    private readonly DBcontext _context;

    public BrandPartnerUserQueryRepository(DBcontext context)
    {
        _context = context;
    }

    public async Task<BrandPartnerUser?> GetByEmailAsync(string email)
    {
        return await _context.BrandPartnerUsers
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<BrandPartnerUser?> GetByEmailAndCustomerAsync(int customerId, string email)
    {
        return await _context.BrandPartnerUsers
            .FirstOrDefaultAsync(u => u.CustomerId == customerId && u.Email.ToLower() == email.ToLower());
    }

    public async Task<BrandPartnerUser?> GetByIdAsync(int brandPartnerUserId)
    {
        return await _context.BrandPartnerUsers.FindAsync(brandPartnerUserId);
    }

    public async Task<List<BrandPartnerUser>> GetUsersByCustomerIdAsync(
        int customerId,
        bool? isActive = null,
        bool? emailVerified = null)
    {
        var query = _context.BrandPartnerUsers.Where(u => u.CustomerId == customerId);

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        if (emailVerified.HasValue)
            query = query.Where(u => u.EmailVerified == emailVerified.Value);

        return await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
    }

    public async Task<bool> EmailExistsForCustomerAsync(int customerId, string email)
    {
        return await _context.BrandPartnerUsers
            .AnyAsync(u => u.CustomerId == customerId && u.Email.ToLower() == email.ToLower());
    }
}
