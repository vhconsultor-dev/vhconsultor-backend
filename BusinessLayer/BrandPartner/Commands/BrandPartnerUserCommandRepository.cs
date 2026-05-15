using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Shared.Entities;
using ModelLayer.Shared;
using System.Security.Cryptography;
using System.Text;

namespace BusinessLayer.BrandPartner.Commands;

/// <summary>
/// Comandos sobre identidad Brand Partner en [Global].[Users].
/// </summary>
public class BrandPartnerUserCommandRepository
{
    private readonly DBcontext _context;

    public BrandPartnerUserCommandRepository(DBcontext context)
    {
        _context = context;
    }

    public async Task<bool> UpdatePasswordAsync(int userId, string newPasswordHash)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.IsBrandPartner) return false;

        user.PasswordHash = newPasswordHash;
        user.UpdatedAt = DateTimeService.GetCostaRicaNow();
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateRequirePasswordChangeAsync(int userId, bool requireChange)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.IsBrandPartner) return false;

        user.RequirePasswordChangeOnNextLogin = requireChange;
        user.UpdatedAt = DateTimeService.GetCostaRicaNow();
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task IncrementFailedLoginAttemptsAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.IsBrandPartner) return;

        user.FailedLoginAttempts++;
        user.UpdatedAt = DateTimeService.GetCostaRicaNow();

        if (user.FailedLoginAttempts >= 5)
            user.LockedUntil = DateTimeService.GetCostaRicaNow().AddMinutes(30);

        await _context.SaveChangesAsync();
    }

    public async Task ResetFailedLoginAttemptsAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.IsBrandPartner) return;

        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.UpdatedAt = DateTimeService.GetCostaRicaNow();
        await _context.SaveChangesAsync();
    }

    public async Task UpdateLastLoginInfoAsync(int userId, string ipAddress, string? userAgent)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.IsBrandPartner) return;

        user.LastLogin = DateTimeService.GetCostaRicaNow();
        user.LastLoginIP = ipAddress;
        user.LastLoginUserAgent = userAgent;
        user.UpdatedAt = DateTimeService.GetCostaRicaNow();
        await _context.SaveChangesAsync();
    }

    public async Task RecordLoginAttemptAsync(ModelLayer.BrandPartner.Entities.BrandPartnerUserLoginHistory loginHistory)
    {
        _context.BrandPartnerUserLoginHistory.Add(loginHistory);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Desactiva el usuario BP y fuerza hash aleatorio para invalidar contraseña.
    /// </summary>
    public async Task<bool> SetUserInactiveAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.IsBrandPartner) return false;

        user.IsActive = false;
        user.UpdatedAt = DateTimeService.GetCostaRicaNow();
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(randomBytes);
        user.PasswordHash = Convert.ToBase64String(randomBytes);

        await _context.SaveChangesAsync();
        return true;
    }

    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
