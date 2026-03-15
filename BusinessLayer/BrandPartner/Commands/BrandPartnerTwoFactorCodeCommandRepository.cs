using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.BrandPartner.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.BrandPartner.Commands;

/// <summary>
/// Repository de comandos para códigos 2FA de Brand Partner
/// </summary>
public class BrandPartnerTwoFactorCodeCommandRepository
{
    private readonly DBcontext _context;
    private static readonly Random _random = new Random();

    public BrandPartnerTwoFactorCodeCommandRepository(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Crea un código 2FA: 1 letra mayúscula (A-Z) + 4 dígitos aleatorios
    /// Expira en 3 minutos
    /// </summary>
    public async Task<BrandPartnerTwoFactorCode> CreateCodeAsync(int brandPartnerUserId)
    {
        var code = GenerateTwoFactorCode();
        var now = DateTimeService.GetCostaRicaNow();

        var twoFactorCode = new BrandPartnerTwoFactorCode
        {
            BrandPartnerUserId = brandPartnerUserId,
            Code = code,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(3),
            IsUsed = false
        };

        _context.BrandPartnerTwoFactorCodes.Add(twoFactorCode);
        await _context.SaveChangesAsync();
        return twoFactorCode;
    }

    public async Task<bool> MarkCodeAsUsedAsync(int twoFactorCodeId)
    {
        var code = await _context.BrandPartnerTwoFactorCodes.FindAsync(twoFactorCodeId);
        if (code == null) return false;

        code.IsUsed = true;
        code.UsedAt = DateTimeService.GetCostaRicaNow();
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task DeleteExpiredCodesAsync(int brandPartnerUserId)
    {
        var expiredCodes = await _context.BrandPartnerTwoFactorCodes
            .Where(c => c.BrandPartnerUserId == brandPartnerUserId && 
                       c.ExpiresAt < DateTimeService.GetCostaRicaNow())
            .ToListAsync();

        _context.BrandPartnerTwoFactorCodes.RemoveRange(expiredCodes);
        await _context.SaveChangesAsync();
    }

    private string GenerateTwoFactorCode()
    {
        char letter = (char)_random.Next('A', 'Z' + 1);
        string digits = _random.Next(0, 10000).ToString("D4");
        return $"{letter}{digits}";
    }
}
