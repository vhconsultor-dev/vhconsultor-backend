using BusinessLayer.Shared.Commands;
using BusinessLayer.Shared.Queries;
using ModelLayer.Shared.Entities;
using System.Security.Cryptography;
using System.Text;

namespace ApplicationLayer.Shared;

/// <summary>
/// Servicio de aplicación para autenticación y gestión de usuarios
/// </summary>
public class AuthService
{
    private readonly UserQueryRepository _userQueryRepository;
    private readonly UserLoginHistoryQueryRepository _loginHistoryQueryRepository;
    private readonly UserAuthCommandRepository _authCommandRepository;

    public AuthService(
        UserQueryRepository userQueryRepository,
        UserLoginHistoryQueryRepository loginHistoryQueryRepository,
        UserAuthCommandRepository authCommandRepository)
    {
        _userQueryRepository = userQueryRepository;
        _loginHistoryQueryRepository = loginHistoryQueryRepository;
        _authCommandRepository = authCommandRepository;
    }

    #region Login

    /// <summary>
    /// Procesa el inicio de sesión de un usuario
    /// </summary>
    public async Task<LoginResult> LoginAsync(LoginCommand command)
    {
        // Buscar usuario por username o email
        var user = await _userQueryRepository.GetByUsernameOrEmailAsync(command.UsernameOrEmail);

        // Registrar intento de login
        var loginHistory = new UserLoginHistory
        {
            UserId = user?.UserId ?? 0,
            LoginDate = DateTime.UtcNow,
            IPAddress = command.IPAddress ?? "Unknown",
            Location = command.Location,
            Country = command.Country,
            City = command.City,
            Region = command.Region,
            UserAgent = command.UserAgent,
            DeviceType = command.DeviceType,
            Browser = command.Browser,
            OperatingSystem = command.OperatingSystem,
            LoginSuccessful = false,
            FailureReason = null
        };

        // Validar si el usuario existe
        if (user == null)
        {
            loginHistory.FailureReason = "Usuario no encontrado";
            await _authCommandRepository.RecordLoginAttemptAsync(loginHistory);
            return new LoginResult
            {
                Success = false,
                Message = "Usuario o contraseña incorrectos",
                User = null
            };
        }

        // Verificar si la cuenta está activa
        if (!user.IsActive)
        {
            loginHistory.UserId = user.UserId;
            loginHistory.FailureReason = "Cuenta inactiva";
            await _authCommandRepository.RecordLoginAttemptAsync(loginHistory);
            return new LoginResult
            {
                Success = false,
                Message = "La cuenta está inactiva. Contacte al administrador.",
                User = null
            };
        }

        // Verificar si la cuenta está bloqueada
        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
        {
            loginHistory.UserId = user.UserId;
            loginHistory.FailureReason = "Cuenta bloqueada";
            await _authCommandRepository.RecordLoginAttemptAsync(loginHistory);
            return new LoginResult
            {
                Success = false,
                Message = $"La cuenta está bloqueada hasta {user.LockedUntil.Value:yyyy-MM-dd HH:mm:ss}",
                User = null,
                LockedUntil = user.LockedUntil.Value
            };
        }

        // Verificar contraseña
        var passwordHash = HashPassword(command.Password);
        if (user.PasswordHash != passwordHash)
        {
            loginHistory.UserId = user.UserId;
            loginHistory.FailureReason = "Contraseña incorrecta";
            await _authCommandRepository.RecordLoginAttemptAsync(loginHistory);
            await _authCommandRepository.IncrementFailedLoginAttemptsAsync(user.UserId);

            // Verificar si se debe bloquear la cuenta
            var failedAttempts = user.FailedLoginAttempts + 1;
            if (failedAttempts >= 5)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = "Demasiados intentos fallidos. La cuenta ha sido bloqueada por 30 minutos.",
                    User = null
                };
            }

            return new LoginResult
            {
                Success = false,
                Message = $"Usuario o contraseña incorrectos. Intentos restantes: {5 - failedAttempts}",
                User = null
            };
        }

        // Login exitoso
        loginHistory.UserId = user.UserId;
        loginHistory.LoginSuccessful = true;
        await _authCommandRepository.RecordLoginAttemptAsync(loginHistory);
        await _authCommandRepository.UpdateLastLoginInfoAsync(
            user.UserId, 
            command.IPAddress ?? "Unknown", 
            command.Location, 
            command.Country, 
            command.City, 
            command.UserAgent);

        // Ocultar información sensible
        user.PasswordHash = string.Empty;

        return new LoginResult
        {
            Success = true,
            Message = "Inicio de sesión exitoso",
            User = user
        };
    }

    #endregion

    #region Password Management

    /// <summary>
    /// Cambia la contraseña de un usuario
    /// </summary>
    public async Task<ChangePasswordResult> ChangePasswordAsync(ChangePasswordCommand command)
    {
        // Obtener usuario
        var user = await _userQueryRepository.GetByIdAsync(command.UserId);
        if (user == null)
        {
            return new ChangePasswordResult
            {
                Success = false,
                Message = "Usuario no encontrado"
            };
        }

        // Verificar contraseña actual
        var currentPasswordHash = HashPassword(command.CurrentPassword);
        if (user.PasswordHash != currentPasswordHash)
        {
            return new ChangePasswordResult
            {
                Success = false,
                Message = "La contraseña actual es incorrecta"
            };
        }

        // Cambiar contraseña
        var newPasswordHash = HashPassword(command.NewPassword);
        var success = await _authCommandRepository.ChangePasswordAsync(command.UserId, newPasswordHash);

        if (success)
        {
            return new ChangePasswordResult
            {
                Success = true,
                Message = "Contraseña cambiada exitosamente"
            };
        }

        return new ChangePasswordResult
        {
            Success = false,
            Message = "Error al cambiar la contraseña"
        };
    }

    #endregion

    #region Account Lock Management

    /// <summary>
    /// Bloquea una cuenta manualmente
    /// </summary>
    public async Task<AccountLockResult> LockAccountAsync(LockAccountCommand command)
    {
        var user = await _userQueryRepository.GetByIdAsync(command.UserId);
        if (user == null)
        {
            return new AccountLockResult
            {
                Success = false,
                Message = "Usuario no encontrado"
            };
        }

        var success = await _authCommandRepository.LockAccountAsync(command.UserId, command.LockDurationMinutes);
        if (success)
        {
            var lockedUntil = DateTime.UtcNow.AddMinutes(command.LockDurationMinutes);
            return new AccountLockResult
            {
                Success = true,
                Message = $"Cuenta bloqueada hasta {lockedUntil:yyyy-MM-dd HH:mm:ss}",
                LockedUntil = lockedUntil
            };
        }

        return new AccountLockResult
        {
            Success = false,
            Message = "Error al bloquear la cuenta"
        };
    }

    /// <summary>
    /// Desbloquea una cuenta
    /// </summary>
    public async Task<AccountLockResult> UnlockAccountAsync(UnlockAccountCommand command)
    {
        var user = await _userQueryRepository.GetByIdAsync(command.UserId);
        if (user == null)
        {
            return new AccountLockResult
            {
                Success = false,
                Message = "Usuario no encontrado"
            };
        }

        var success = await _authCommandRepository.UnlockAccountAsync(command.UserId);
        if (success)
        {
            return new AccountLockResult
            {
                Success = true,
                Message = "Cuenta desbloqueada exitosamente"
            };
        }

        return new AccountLockResult
        {
            Success = false,
            Message = "Error al desbloquear la cuenta"
        };
    }

    #endregion

    #region User Queries

    /// <summary>
    /// Obtiene usuarios con filtros opcionales
    /// </summary>
    public async Task<IEnumerable<User>> GetUsersAsync(
        int? userId = null,
        string? email = null,
        string? username = null,
        bool? isActive = null,
        bool? isCorporate = null,
        bool? isBrandPartner = null,
        bool? emailVerified = null)
    {
        var users = await _userQueryRepository.GetUsersAsync(
            userId, email, username, isActive, isCorporate, isBrandPartner, emailVerified);

        // Ocultar contraseñas
        foreach (var user in users)
        {
            user.PasswordHash = string.Empty;
        }

        return users;
    }

    #endregion

    #region Login History

    /// <summary>
    /// Obtiene el historial de login con filtros opcionales
    /// </summary>
    public async Task<IEnumerable<UserLoginHistory>> GetLoginHistoryAsync(
        int? userId = null,
        DateTime? loginDateFrom = null,
        DateTime? loginDateTo = null,
        string? ipAddress = null,
        string? country = null,
        string? city = null,
        bool? loginSuccessful = null,
        int? limit = 100)
    {
        return await _loginHistoryQueryRepository.GetLoginHistoryAsync(
            userId, loginDateFrom, loginDateTo, ipAddress, country, city, loginSuccessful, limit);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Genera un hash SHA256 de la contraseña
    /// </summary>
    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    #endregion
}

#region Result Classes

public class LoginResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public User? User { get; set; }
    public DateTime? LockedUntil { get; set; }
}

public class ChangePasswordResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class AccountLockResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? LockedUntil { get; set; }
}

#endregion

