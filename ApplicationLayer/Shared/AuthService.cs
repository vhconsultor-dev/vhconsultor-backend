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
    private readonly RBACQueryRepository _rbacQueryRepository;

    public AuthService(
        UserQueryRepository userQueryRepository,
        UserLoginHistoryQueryRepository loginHistoryQueryRepository,
        UserAuthCommandRepository authCommandRepository,
        RBACQueryRepository rbacQueryRepository)
    {
        _userQueryRepository = userQueryRepository;
        _loginHistoryQueryRepository = loginHistoryQueryRepository;
        _authCommandRepository = authCommandRepository;
        _rbacQueryRepository = rbacQueryRepository;
    }

    #region Login

    /// <summary>
    /// Procesa el inicio de sesión de un usuario
    /// </summary>
    public async Task<LoginResult> LoginAsync(LoginCommand command)
    {
        // Buscar usuario por username o email
        var user = await _userQueryRepository.GetByUsernameOrEmailAsync(command.UsernameOrEmail);

        // Validar si el usuario existe
        if (user == null)
        {
            // No registrar historial si el usuario no existe (para evitar problemas de FK)
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
            // Solo registrar historial si ApplicationId está presente
            if (command.ApplicationId.HasValue)
            {
                try
                {
                    await _authCommandRepository.RecordLoginAttemptAsync(new UserLoginHistory
                    {
                        UserId = user.UserId,
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
                        FailureReason = "Cuenta inactiva",
                        ApplicationId = command.ApplicationId
                    });
                }
                catch
                {
                    // Silenciar error del historial, no debe impedir mostrar el mensaje
                }
            }
            
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
            try
            {
                await _authCommandRepository.RecordLoginAttemptAsync(new UserLoginHistory
                {
                    UserId = user.UserId,
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
                    FailureReason = "Cuenta bloqueada",
                    ApplicationId = command.ApplicationId
                });
            }
            catch
            {
                // Silenciar error del historial, no debe impedir mostrar el mensaje
            }
            
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
            try
            {
                await _authCommandRepository.RecordLoginAttemptAsync(new UserLoginHistory
                {
                    UserId = user.UserId,
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
                    FailureReason = "Contraseña incorrecta",
                    ApplicationId = command.ApplicationId
                });
            }
            catch
            {
                // Silenciar error del historial, no debe impedir la respuesta al usuario
            }
            
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
        try
        {
            await _authCommandRepository.RecordLoginAttemptAsync(new UserLoginHistory
            {
                UserId = user.UserId,
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
                LoginSuccessful = true,
                FailureReason = null,
                ApplicationId = command.ApplicationId
            });
        }
        catch (Exception ex)
        {
            // Si falla el registro del historial, continuar con el login pero loguear el error
            // No queremos que un error en el historial impida el login
            System.Diagnostics.Debug.WriteLine($"Error al registrar historial de login: {ex.Message}");
        }
        
        try
        {
            await _authCommandRepository.UpdateLastLoginInfoAsync(
                user.UserId, 
                command.IPAddress ?? "Unknown", 
                command.Location, 
                command.Country, 
                command.City, 
                command.UserAgent);
        }
        catch (Exception ex)
        {
            // Si falla la actualización de último login, continuar con el login
            System.Diagnostics.Debug.WriteLine($"Error al actualizar último login: {ex.Message}");
        }

        // Obtener aplicaciones disponibles para el usuario
        var applications = await _rbacQueryRepository.GetUserApplicationsAsync(user.UserId);

        // Ocultar información sensible
        user.PasswordHash = string.Empty;

        return new LoginResult
        {
            Success = true,
            Message = "Inicio de sesión exitoso",
            User = user,
            Applications = applications.ToList()
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

    /// <summary>
    /// Resetea la contraseña de un usuario (solo para administradores)
    /// Genera una contraseña aleatoria segura y la retorna
    /// </summary>
    public async Task<AdminResetPasswordResult> AdminResetPasswordAsync(AdminResetPasswordCommand command)
    {
        // Obtener usuario
        var user = await _userQueryRepository.GetByIdAsync(command.UserId);
        if (user == null)
        {
            return new AdminResetPasswordResult
            {
                Success = false,
                Message = "Usuario no encontrado",
                NewPassword = null
            };
        }

        // Generar contraseña aleatoria segura
        var newPassword = GenerateSecureRandomPassword();

        // Cambiar contraseña
        var newPasswordHash = HashPassword(newPassword);
        var success = await _authCommandRepository.ChangePasswordAsync(command.UserId, newPasswordHash);

        if (success)
        {
            return new AdminResetPasswordResult
            {
                Success = true,
                Message = "Contraseña reseteada exitosamente",
                NewPassword = newPassword
            };
        }

        return new AdminResetPasswordResult
        {
            Success = false,
            Message = "Error al resetear la contraseña",
            NewPassword = null
        };
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

    /// <summary>
    /// Genera una contraseña aleatoria segura que cumple con los requisitos:
    /// - Mínimo 12 caracteres
    /// - Al menos una letra mayúscula
    /// - Al menos una letra minúscula
    /// - Al menos un número
    /// - Al menos un carácter especial
    /// </summary>
    private string GenerateSecureRandomPassword()
    {
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string numbers = "0123456789";
        const string special = "!@#$%^&*()_+-=[]{}|;:,.<>?";
        const string allChars = uppercase + lowercase + numbers + special;

        var random = new Random();
        var password = new System.Text.StringBuilder();

        // Asegurar al menos un carácter de cada tipo
        password.Append(uppercase[random.Next(uppercase.Length)]);
        password.Append(lowercase[random.Next(lowercase.Length)]);
        password.Append(numbers[random.Next(numbers.Length)]);
        password.Append(special[random.Next(special.Length)]);

        // Completar hasta 12 caracteres con caracteres aleatorios
        for (int i = password.Length; i < 12; i++)
        {
            password.Append(allChars[random.Next(allChars.Length)]);
        }

        // Mezclar los caracteres para que no siempre estén en el mismo orden
        var passwordArray = password.ToString().ToCharArray();
        for (int i = passwordArray.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (passwordArray[i], passwordArray[j]) = (passwordArray[j], passwordArray[i]);
        }

        return new string(passwordArray);
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
    public List<Application> Applications { get; set; } = new();
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
    public string? NewPassword { get; set; }
}

#endregion

