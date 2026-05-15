using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.BrandPartner.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.BrandPartner.Queries;

/// <summary>
/// Repository de queries para códigos 2FA de Brand Partner
/// </summary>
public class BrandPartnerTwoFactorCodeQueryRepository
{
    private readonly DBcontext _context;

    public BrandPartnerTwoFactorCodeQueryRepository(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene el código activo más reciente (no usado y no expirado)
    /// </summary>
    public async Task<BrandPartnerTwoFactorCode?> GetActiveCodeAsync(int globalUserId)
    {
        var now = DateTimeService.GetCostaRicaNow();
        return await _context.BrandPartnerTwoFactorCodes
            .Where(c => c.UserId == globalUserId &&
                       !c.IsUsed &&
                       c.ExpiresAt > now)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Valida un código 2FA
    /// </summary>
    public async Task<BrandPartnerTwoFactorCode?> ValidateCodeAsync(int globalUserId, string code)
    {
        var now = DateTimeService.GetCostaRicaNow();
        return await _context.BrandPartnerTwoFactorCodes
            .Where(c => c.UserId == globalUserId &&
                       c.Code.ToUpper() == code.ToUpper() &&
                       !c.IsUsed &&
                       c.ExpiresAt > now)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();
    }
}
