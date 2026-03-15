using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.BrandPartner.Entities;
using ModelLayer.Shared;
using System.Security.Cryptography;
using System.Text;

namespace BusinessLayer.BrandPartner.Commands;

/// <summary>
/// Repository de comandos para usuarios Brand Partner
/// </summary>
public class BrandPartnerUserCommandRepository
{
    private readonly DBcontext _context;

    public BrandPartnerUserCommandRepository(DBcontext context)
    {
        _context = context;
    }

    public async Task<int> CreateUserAsync(BrandPartnerUser user)
    {
        _context.BrandPartnerUsers.Add(user);
        await _context.SaveChangesAsync();
        return user.BrandPartnerUserId;
    }

    public async Task<bool> UpdatePasswordAsync(int brandPartnerUserId, string newPasswordHash)
    {
        var user = await _context.BrandPartnerUsers.FindAsync(brandPartnerUserId);
        if (user == null) return false;

        user.PasswordHash = newPasswordHash;
        user.UpdatedAt = DateTimeService.GetCostaRicaNow();
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateRequirePasswordChangeAsync(int brandPartnerUserId, bool requireChange)
    {
        var user = await _context.BrandPartnerUsers.FindAsync(brandPartnerUserId);
        if (user == null) return false;

        user.RequirePasswordChangeOnNextLogin = requireChange;
        user.UpdatedAt = DateTimeService.GetCostaRicaNow();
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task IncrementFailedLoginAttemptsAsync(int brandPartnerUserId)
    {
        var user = await _context.BrandPartnerUsers.FindAsync(brandPartnerUserId);
        if (user == null) return;

        user.FailedLoginAttempts++;
        user.UpdatedAt = DateTimeService.GetCostaRicaNow();

        if (user.FailedLoginAttempts >= 5)
        {
            user.LockedUntil = DateTimeService.GetCostaRicaNow().AddMinutes(30);
        }

        await _context.SaveChangesAsync();
    }

    public async Task ResetFailedLoginAttemptsAsync(int brandPartnerUserId)
    {
        var user = await _context.BrandPartnerUsers.FindAsync(brandPartnerUserId);
        if (user == null) return;

        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.UpdatedAt = DateTimeService.GetCostaRicaNow();
        await _context.SaveChangesAsync();
    }

    public async Task UpdateLastLoginInfoAsync(int brandPartnerUserId, string ipAddress, string? userAgent)
    {
        var user = await _context.BrandPartnerUsers.FindAsync(brandPartnerUserId);
        if (user == null) return;

        user.LastLogin = DateTimeService.GetCostaRicaNow();
        user.LastLoginIP = ipAddress;
        user.LastLoginUserAgent = userAgent;
        user.UpdatedAt = DateTimeService.GetCostaRicaNow();
        await _context.SaveChangesAsync();
    }

    public async Task RecordLoginAttemptAsync(BrandPartnerUserLoginHistory loginHistory)
    {
        _context.BrandPartnerUserLoginHistory.Add(loginHistory);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Sets user as inactive and replaces password with a random hash so the user cannot log in.
    /// </summary>
    public async Task<bool> SetUserInactiveAsync(int brandPartnerUserId)
    {
        var user = await _context.BrandPartnerUsers.FindAsync(brandPartnerUserId);
        if (user == null) return false;

        user.IsActive = false;
        user.UpdatedAt = DateTimeService.GetCostaRicaNow();
        // Set a random password hash so the previous password no longer works
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
