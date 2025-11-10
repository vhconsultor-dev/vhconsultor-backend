using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Shared;
using ModelLayer.Shared.Entities;

namespace BusinessLayer.Shared.Commands;

/// <summary>
/// Command Repository para operaciones de autenticación de usuarios usando Entity Framework
/// </summary>
public class UserAuthCommandRepository
{
    private readonly DBcontext _context;

    public UserAuthCommandRepository(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Registra un intento de login en el historial
    /// </summary>
    public async Task<int> RecordLoginAttemptAsync(UserLoginHistory loginHistory)
    {
        _context.UserLoginHistory.Add(loginHistory);
        await _context.SaveChangesAsync();
        return loginHistory.LoginHistoryId;
    }

    /// <summary>
    /// Actualiza la información del último login del usuario
    /// </summary>
    public async Task UpdateLastLoginInfoAsync(int userId, string ipAddress, string? location, 
        string? country, string? city, string? userAgent)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastLogin = DateTimeService.GetCostaRicaNow();
            user.LastLoginIP = ipAddress;
            user.LastLoginLocation = location;
            user.LastLoginCountry = country;
            user.LastLoginCity = city;
            user.LastLoginUserAgent = userAgent;
            user.FailedLoginAttempts = 0; // Resetear intentos fallidos en login exitoso
            user.UpdatedAt = DateTimeService.GetCostaRicaNow();

            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Incrementa los intentos fallidos de login
    /// </summary>
    public async Task IncrementFailedLoginAttemptsAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.FailedLoginAttempts++;
            user.UpdatedAt = DateTimeService.GetCostaRicaNow();

            // Bloquear cuenta si supera 5 intentos fallidos
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockedUntil = DateTimeService.GetCostaRicaNow().AddMinutes(30);
            }

            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Cambia la contraseña de un usuario
    /// </summary>
    public async Task<bool> ChangePasswordAsync(int userId, string newPasswordHash)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        user.PasswordHash = newPasswordHash;
        user.UpdatedAt = DateTimeService.GetCostaRicaNow();
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Bloquea una cuenta manualmente
    /// </summary>
    public async Task<bool> LockAccountAsync(int userId, int lockDurationMinutes)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        user.LockedUntil = DateTimeService.GetCostaRicaNow().AddMinutes(lockDurationMinutes);
        user.UpdatedAt = DateTimeService.GetCostaRicaNow();
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Desbloquea una cuenta
    /// </summary>
    public async Task<bool> UnlockAccountAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        user.LockedUntil = null;
        user.FailedLoginAttempts = 0;
        user.UpdatedAt = DateTimeService.GetCostaRicaNow();
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Resetea los intentos fallidos de login
    /// </summary>
    public async Task ResetFailedLoginAttemptsAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.FailedLoginAttempts = 0;
            user.UpdatedAt = DateTimeService.GetCostaRicaNow();
            await _context.SaveChangesAsync();
        }
    }
}

