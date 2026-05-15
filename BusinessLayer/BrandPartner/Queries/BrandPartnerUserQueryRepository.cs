using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.BrandPartner.Entities;
using ModelLayer.Shared.Entities;

namespace BusinessLayer.BrandPartner.Queries;

/// <summary>
/// Consultas de usuarios Brand Partner (origen: [Global].[Users] con IsBrandPartner = true).
/// </summary>
public class BrandPartnerUserQueryRepository
{
    private readonly DBcontext _context;

    public BrandPartnerUserQueryRepository(DBcontext context)
    {
        _context = context;
    }

    private static BrandPartnerUser MapFromUser(User u) => new()
    {
        UserId = u.UserId,
        CustomerId = u.CustomerId ?? 0,
        Email = u.Email,
        PasswordHash = u.PasswordHash,
        FirstName = u.FirstName,
        LastName = u.LastName,
        PhoneNumber = u.PhoneNumber,
        IsActive = u.IsActive,
        EmailVerified = u.EmailVerified,
        RequirePasswordChangeOnNextLogin = u.RequirePasswordChangeOnNextLogin,
        FailedLoginAttempts = u.FailedLoginAttempts,
        LockedUntil = u.LockedUntil,
        LastLogin = u.LastLogin,
        LastLoginIP = u.LastLoginIP,
        LastLoginUserAgent = u.LastLoginUserAgent,
        CreatedAt = u.CreatedAt,
        UpdatedAt = u.UpdatedAt,
        CreatedBy = u.CreatedBy
    };

    /// <summary>Login paso 1 / verify: coincide email o username (case-insensitive).</summary>
    public async Task<BrandPartnerUser?> GetByEmailOrUsernameAsync(string emailOrUsername)
    {
        var key = emailOrUsername.Trim().ToLowerInvariant();
        var u = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsBrandPartner &&
                (x.Email.ToLower() == key || x.Username.ToLower() == key));
        return u == null ? null : MapFromUser(u);
    }

    public async Task<BrandPartnerUser?> GetByEmailAndCustomerAsync(int customerId, string email)
    {
        var key = email.Trim().ToLowerInvariant();
        var u = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.IsBrandPartner &&
                x.CustomerId == customerId &&
                x.Email.ToLower() == key);
        return u == null ? null : MapFromUser(u);
    }

    public async Task<BrandPartnerUser?> GetByIdAsync(int userId)
    {
        var u = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsBrandPartner && x.UserId == userId);
        return u == null ? null : MapFromUser(u);
    }

    public async Task<List<BrandPartnerUser>> GetUsersByCustomerIdAsync(
        int customerId,
        bool? isActive = null,
        bool? emailVerified = null)
    {
        var query = _context.Users.AsNoTracking()
            .Where(u => u.IsBrandPartner && u.CustomerId == customerId);

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        if (emailVerified.HasValue)
            query = query.Where(u => u.EmailVerified == emailVerified.Value);

        var list = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
        return list.Select(MapFromUser).ToList();
    }

    public async Task<bool> EmailExistsForCustomerAsync(int customerId, string email)
    {
        var key = email.Trim().ToLowerInvariant();
        return await _context.Users.AnyAsync(u =>
            u.IsBrandPartner &&
            u.CustomerId == customerId &&
            u.Email.ToLower() == key);
    }
}
