using BusinessLayer.Shared.Commands;
using BusinessLayer.Shared.Queries;
using ModelLayer.Shared;
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
    private readonly SendGridService _sendGridService;
    private readonly Microsoft.Extensions.Options.IOptions<BusinessLayer.Shared.SendGridSettings> _sendGridSettings;
    private readonly RBACQueryRepository _rbacQueryRepository;
    private readonly JwtService _jwtService;

    public AuthService(
        UserQueryRepository userQueryRepository,
        UserLoginHistoryQueryRepository loginHistoryQueryRepository,
        UserAuthCommandRepository authCommandRepository,
        SendGridService sendGridService,
        Microsoft.Extensions.Options.IOptions<BusinessLayer.Shared.SendGridSettings> sendGridSettings,
        RBACQueryRepository rbacQueryRepository,
        JwtService jwtService)
    {
        _userQueryRepository = userQueryRepository;
        _loginHistoryQueryRepository = loginHistoryQueryRepository;
        _authCommandRepository = authCommandRepository;
        _sendGridService = sendGridService;
        _sendGridSettings = sendGridSettings;
        _rbacQueryRepository = rbacQueryRepository;
        _jwtService = jwtService;
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
            LoginDate = DateTimeService.GetCostaRicaNow(),
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
        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTimeService.GetCostaRicaNow())
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

        // Obtener permisos efectivos y generar JWT de sesión con claims
        // SuperAdmin recibe lista vacía (el filter lo detecta por el claim IsSuperAdmin)
        IEnumerable<string> permissionKeys = user.IsSuperAdmin
            ? Enumerable.Empty<string>()
            : (await _rbacQueryRepository.GetEffectiveUserPermissionsAsync(user.UserId))
                .Where(p => p.IsActive)
                .Select(p => p.PermissionKey);

        var sessionToken = _jwtService.GenerateUserSessionToken(
            user.UserId,
            user.IsSuperAdmin,
            permissionKeys);

        // Ocultar información sensible
        user.PasswordHash = string.Empty;

        return new LoginResult
        {
            Success = true,
            Message = "Inicio de sesión exitoso",
            User = user,
            SessionToken = sessionToken
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

    #region Admin Password Reset

    /// <summary>
    /// Admin resets a user's password by generating a new temporary password automatically.
    /// Validates user exists, is active, and is not locked. Sends password via email.
    /// </summary>
    public async Task<AdminResetPasswordResult> AdminResetPasswordAsync(AdminResetPasswordRequest request)
    {
        // Get user
        var user = await _userQueryRepository.GetByIdAsync(request.UserId);
        if (user == null)
        {
            return new AdminResetPasswordResult
            {
                Success = false,
                Message = $"User with ID {request.UserId} was not found."
            };
        }

        // Validate user is active
        if (!user.IsActive)
        {
            return new AdminResetPasswordResult
            {
                Success = false,
                Message = $"Cannot reset password for user '{user.Username}' because the account is inactive."
            };
        }

        // Validate user is not locked
        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTimeService.GetCostaRicaNow())
        {
            return new AdminResetPasswordResult
            {
                Success = false,
                Message = $"Cannot reset password for user '{user.Username}' because the account is locked until {user.LockedUntil.Value:yyyy-MM-dd HH:mm:ss}. Please unlock the account first."
            };
        }

        // Generate temporary password (12 characters: uppercase, lowercase, digits, special chars)
        var temporaryPassword = GenerateTemporaryPassword();

        // Hash the new password
        var newPasswordHash = HashPassword(temporaryPassword);

        // Update password in database
        var success = await _authCommandRepository.ChangePasswordAsync(request.UserId, newPasswordHash);
        if (!success)
        {
            return new AdminResetPasswordResult
            {
                Success = false,
                Message = "Error updating password in database."
            };
        }

        // Send email with new password using SendGrid template
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        var currentYear = DateTime.UtcNow.Year.ToString();
        var templateData = new
        {
            fullName = fullName,
            username = user.Email, // Using email as username for the template
            newPassword = temporaryPassword,
            supportEmail = _sendGridSettings.Value.SupportEmail,
            year = currentYear
        };

        var emailResult = await _sendGridService.SendTemplateEmailAsync(
            user.Email,
            _sendGridSettings.Value.ResetPasswordTemplateId,
            templateData,
            ccEmail: null);

        if (!emailResult.Success)
        {
            // Password was changed in DB, but email failed
            return new AdminResetPasswordResult
            {
                Success = false,
                Message = $"Password was reset in database, but failed to send email to '{user.Email}'. Error: {emailResult.Message}",
                TemporaryPassword = temporaryPassword // Return password so admin can manually share it
            };
        }

        return new AdminResetPasswordResult
        {
            Success = true,
            Message = $"Password reset successfully for user '{user.Username}'. Temporary password sent to '{user.Email}'.",
            TemporaryPassword = temporaryPassword // Optionally return for admin logging
        };
    }

    /// <summary>
    /// Generates a secure temporary password: 12 characters with uppercase, lowercase, digits, and special chars.
    /// </summary>
    private string GenerateTemporaryPassword()
    {
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";
        const int length = 12;

        var random = new Random();
        var password = new char[length];

        // Ensure at least one of each type
        password[0] = uppercase[random.Next(uppercase.Length)];
        password[1] = lowercase[random.Next(lowercase.Length)];
        password[2] = digits[random.Next(digits.Length)];
        password[3] = special[random.Next(special.Length)];

        // Fill remaining with random from all character sets
        var allChars = uppercase + lowercase + digits + special;
        for (int i = 4; i < length; i++)
        {
            password[i] = allChars[random.Next(allChars.Length)];
        }

        // Shuffle the password characters
        for (int i = length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }

    #endregion

    #region User Creation

    /// <summary>
    /// Crea un nuevo usuario
    /// </summary>
    public async Task<CreateUserResult> CreateUserAsync(CreateUserCommand command)
    {
        // Verificar si el email ya existe
        var existingUserByEmail = await _userQueryRepository.GetByUsernameOrEmailAsync(command.Email);
        if (existingUserByEmail != null)
        {
            return new CreateUserResult
            {
                Success = false,
                Message = "El email ya está registrado"
            };
        }

        // Verificar si el username ya existe
        var existingUserByUsername = await _userQueryRepository.GetByUsernameOrEmailAsync(command.Username);
        if (existingUserByUsername != null)
        {
            return new CreateUserResult
            {
                Success = false,
                Message = "El nombre de usuario ya está en uso"
            };
        }

        // Crear el hash de la contraseña
        var passwordHash = HashPassword(command.Password);

        // Crear el nuevo usuario
        var user = new User
        {
            FirstName = command.FirstName,
            LastName = command.LastName,
            Email = command.Email,
            Username = command.Username,
            PasswordHash = passwordHash,
            PhoneNumber = command.PhoneNumber,
            ProfilePictureUrl = command.ProfilePictureUrl,
            IsCorporate = command.IsCorporate,
            IsBrandPartner = command.IsBrandPartner,
            IsActive = true,
            EmailVerified = false,
            FailedLoginAttempts = 0,
            LockedUntil = null,
            CreatedAt = DateTimeService.GetCostaRicaNow(),
            CreatedBy = command.CreatedBy
        };

        try
        {
            var userId = await _authCommandRepository.CreateUserAsync(user);
            
            // Ocultar información sensible
            user.PasswordHash = string.Empty;

            return new CreateUserResult
            {
                Success = true,
                Message = "Usuario creado exitosamente",
                User = user,
                UserId = userId
            };
        }
        catch (Exception ex)
        {
            return new CreateUserResult
            {
                Success = false,
                Message = $"Error al crear el usuario: {ex.Message}"
            };
        }
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
            var lockedUntil = DateTimeService.GetCostaRicaNow().AddMinutes(command.LockDurationMinutes);
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
    /// <summary>
    /// JWT de sesión con los permission claims del usuario.
    /// El frontend debe usar este token en el header Authorization: Bearer {SessionToken}
    /// para todas las llamadas subsiguientes a la API.
    /// </summary>
    public string? SessionToken { get; set; }
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

public class CreateUserResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public User? User { get; set; }
    public int? UserId { get; set; }
}

public class AdminResetPasswordResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? TemporaryPassword { get; set; }
}

#endregion

