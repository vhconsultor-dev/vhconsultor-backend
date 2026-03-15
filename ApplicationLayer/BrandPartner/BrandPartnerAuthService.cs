using BusinessLayer.BrandPartner.Commands;
using BusinessLayer.BrandPartner.Queries;
using BusinessLayer.Corporate.Queries;
using ApplicationLayer.Shared;
using ModelLayer.BrandPartner.Entities;
using ModelLayer.Shared;
using Microsoft.Extensions.Options;
using BusinessLayer.Shared;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace ApplicationLayer.BrandPartner;

/// <summary>
/// Servicio de autenticación para Brand Partner con 2FA
/// </summary>
public class BrandPartnerAuthService
{
    private readonly BrandPartnerUserQueryRepository _userQueryRepository;
    private readonly BrandPartnerLoginHistoryQueryRepository _loginHistoryQueryRepository;
    private readonly BrandPartnerTwoFactorCodeQueryRepository _twoFactorQueryRepository;
    private readonly BrandPartnerUserCommandRepository _userCommandRepository;
    private readonly BrandPartnerTwoFactorCodeCommandRepository _twoFactorCommandRepository;
    private readonly CustomerQueryRepository _customerQueryRepository;
    private readonly SendGridService _sendGridService;
    private readonly JwtService _jwtService;
    private readonly IOptions<SendGridSettings> _sendGridSettings;
    private readonly IOptions<JwtSettings> _jwtSettings;

    public BrandPartnerAuthService(
        BrandPartnerUserQueryRepository userQueryRepository,
        BrandPartnerLoginHistoryQueryRepository loginHistoryQueryRepository,
        BrandPartnerTwoFactorCodeQueryRepository twoFactorQueryRepository,
        BrandPartnerUserCommandRepository userCommandRepository,
        BrandPartnerTwoFactorCodeCommandRepository twoFactorCommandRepository,
        CustomerQueryRepository customerQueryRepository,
        SendGridService sendGridService,
        JwtService jwtService,
        IOptions<SendGridSettings> sendGridSettings,
        IOptions<JwtSettings> jwtSettings)
    {
        _userQueryRepository = userQueryRepository;
        _loginHistoryQueryRepository = loginHistoryQueryRepository;
        _twoFactorQueryRepository = twoFactorQueryRepository;
        _userCommandRepository = userCommandRepository;
        _twoFactorCommandRepository = twoFactorCommandRepository;
        _customerQueryRepository = customerQueryRepository;
        _sendGridService = sendGridService;
        _jwtService = jwtService;
        _sendGridSettings = sendGridSettings;
        _jwtSettings = jwtSettings;
    }

    /// <summary>
    /// Crea un nuevo usuario Brand Partner
    /// </summary>
    public async Task<CreateBrandPartnerUserResult> CreateUserAsync(CreateBrandPartnerUserRequest request)
    {
        // Validar que el Customer existe
        var customer = await _customerQueryRepository.GetByIdAsync(request.CustomerId);
        if (customer == null)
        {
            return new CreateBrandPartnerUserResult
            {
                Success = false,
                Message = $"Customer with ID {request.CustomerId} does not exist."
            };
        }

        // Validar que el email no existe para este Customer
        var emailExists = await _userQueryRepository.EmailExistsForCustomerAsync(request.CustomerId, request.Email);
        if (emailExists)
        {
            return new CreateBrandPartnerUserResult
            {
                Success = false,
                Message = $"A user with email '{request.Email}' already exists for this customer."
            };
        }

        // Crear usuario
        var user = new BrandPartnerUser
        {
            CustomerId = request.CustomerId,
            Email = request.Email.ToLower(),
            PasswordHash = _userCommandRepository.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            EmailVerified = false,
            RequirePasswordChangeOnNextLogin = false,
            FailedLoginAttempts = 0,
            CreatedAt = DateTimeService.GetCostaRicaNow(),
            CreatedBy = request.CreatedBy
        };

        var userId = await _userCommandRepository.CreateUserAsync(user);

        return new CreateBrandPartnerUserResult
        {
            Success = true,
            Message = "Brand Partner user created successfully.",
            BrandPartnerUserId = userId,
            User = new
            {
                user.BrandPartnerUserId,
                user.CustomerId,
                user.Email,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.IsActive,
                user.EmailVerified
            }
        };
    }

    /// <summary>
    /// Paso 1 del login: validar email/password y generar código 2FA
    /// </summary>
    public async Task<BrandPartnerLoginResult> LoginStepOneAsync(BrandPartnerLoginCommand command)
    {
        try
        {
            var user = await _userQueryRepository.GetByEmailAsync(command.Email);

            if (user == null)
            {
                // Intentar registrar historial (sin fallar si la tabla no existe)
                try
                {
                    var loginHistory = new BrandPartnerUserLoginHistory
                    {
                        BrandPartnerUserId = 0,
                        LoginDate = DateTimeService.GetCostaRicaNow(),
                        IPAddress = command.IPAddress ?? "Unknown",
                        UserAgent = command.UserAgent,
                        Location = command.Location,
                        Country = command.Country,
                        City = command.City,
                        LoginSuccessful = false,
                        FailureReason = "User not found"
                    };
                    await _userCommandRepository.RecordLoginAttemptAsync(loginHistory);
                }
                catch
                {
                    // Ignorar errores de historial
                }

                return new BrandPartnerLoginResult
                {
                    Success = false,
                    Message = "Invalid email or password."
                };
            }

            // Registrar intento
            var loginHistoryRecord = new BrandPartnerUserLoginHistory
            {
                BrandPartnerUserId = user.BrandPartnerUserId,
                LoginDate = DateTimeService.GetCostaRicaNow(),
                IPAddress = command.IPAddress ?? "Unknown",
                UserAgent = command.UserAgent,
                Location = command.Location,
                Country = command.Country,
                City = command.City,
                LoginSuccessful = false,
                FailureReason = null
            };

            // Verificar cuenta activa
            if (!user.IsActive)
            {
                loginHistoryRecord.FailureReason = "Account inactive";
                try { await _userCommandRepository.RecordLoginAttemptAsync(loginHistoryRecord); } catch { }
                return new BrandPartnerLoginResult
                {
                    Success = false,
                    Message = "Account is inactive. Please contact support."
                };
            }

            // Verificar bloqueo
            if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTimeService.GetCostaRicaNow())
            {
                loginHistoryRecord.FailureReason = "Account locked";
                try { await _userCommandRepository.RecordLoginAttemptAsync(loginHistoryRecord); } catch { }
                return new BrandPartnerLoginResult
                {
                    Success = false,
                    Message = $"Account is locked until {user.LockedUntil.Value:yyyy-MM-dd HH:mm:ss}",
                    LockedUntil = user.LockedUntil.Value
                };
            }

            // Verificar contraseña
            var passwordHash = _userCommandRepository.HashPassword(command.Password);
            if (user.PasswordHash != passwordHash)
            {
                loginHistoryRecord.FailureReason = "Incorrect password";
                try { await _userCommandRepository.RecordLoginAttemptAsync(loginHistoryRecord); } catch { }
                
                try
                {
                    await _userCommandRepository.IncrementFailedLoginAttemptsAsync(user.BrandPartnerUserId);
                }
                catch
                {
                    // Si falla incrementar, continuar
                }

                var failedAttempts = user.FailedLoginAttempts + 1;
                if (failedAttempts >= 5)
                {
                    return new BrandPartnerLoginResult
                    {
                        Success = false,
                        Message = "Too many failed attempts. Account locked for 30 minutes."
                    };
                }

                return new BrandPartnerLoginResult
                {
                    Success = false,
                    Message = $"Invalid email or password. Remaining attempts: {5 - failedAttempts}"
                };
            }

            // Credenciales válidas: generar y enviar código 2FA
            try
            {
                await _twoFactorCommandRepository.DeleteExpiredCodesAsync(user.BrandPartnerUserId);
            }
            catch
            {
                // Ignorar si falla limpiar códigos expirados
            }

            var twoFactorCode = await _twoFactorCommandRepository.CreateCodeAsync(user.BrandPartnerUserId);

            // Enviar email con código
            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            var templateData = new
            {
                fullName,
                code = twoFactorCode.Code,
                expiresIn = "3 minutos"
            };

            try
            {
                await _sendGridService.SendTemplateEmailAsync(
                    user.Email,
                    _sendGridSettings.Value.BrandPartnerTwoFactorCodeTemplateId,
                    templateData,
                    null);
            }
            catch
            {
                return new BrandPartnerLoginResult
                {
                    Success = false,
                    Message = "Failed to send verification code. Please check your email configuration or contact support."
                };
            }

            loginHistoryRecord.LoginSuccessful = false;
            loginHistoryRecord.FailureReason = "Waiting for 2FA code";
            try { await _userCommandRepository.RecordLoginAttemptAsync(loginHistoryRecord); } catch { }

            return new BrandPartnerLoginResult
            {
                Success = true,
                Message = "2FA code sent to your email. Please check your inbox.",
                RequiresTwoFactor = true,
                BrandPartnerUserId = user.BrandPartnerUserId
            };
        }
        catch
        {
            // Log the actual error for debugging but return a user-friendly message
            // TODO: Add proper logging here (ILogger)
            return new BrandPartnerLoginResult
            {
                Success = false,
                Message = "An error occurred during login. Please try again or contact support if the problem persists."
            };
        }
    }

    /// <summary>
    /// Paso 2 del login: verificar código 2FA y generar JWT
    /// </summary>
    public async Task<VerifyTwoFactorCodeResult> VerifyTwoFactorCodeAsync(VerifyTwoFactorCodeCommand command)
    {
        var user = await _userQueryRepository.GetByEmailAsync(command.Email);
        if (user == null)
        {
            return new VerifyTwoFactorCodeResult
            {
                Success = false,
                Message = "Invalid email."
            };
        }

        // Validar código
        var validCode = await _twoFactorQueryRepository.ValidateCodeAsync(user.BrandPartnerUserId, command.Code);
        if (validCode == null)
        {
            return new VerifyTwoFactorCodeResult
            {
                Success = false,
                Message = "Invalid or expired 2FA code."
            };
        }

        // Marcar código como usado
        await _twoFactorCommandRepository.MarkCodeAsUsedAsync(validCode.TwoFactorCodeId);

        // Resetear intentos fallidos y actualizar último login
        await _userCommandRepository.ResetFailedLoginAttemptsAsync(user.BrandPartnerUserId);
        await _userCommandRepository.UpdateLastLoginInfoAsync(user.BrandPartnerUserId, command.IPAddress ?? "Unknown", command.UserAgent);

        // Registrar login exitoso
        var loginHistory = new BrandPartnerUserLoginHistory
        {
            BrandPartnerUserId = user.BrandPartnerUserId,
            LoginDate = DateTimeService.GetCostaRicaNow(),
            IPAddress = command.IPAddress ?? "Unknown",
            UserAgent = command.UserAgent,
            LoginSuccessful = true,
            SessionId = command.SessionId
        };
        await _userCommandRepository.RecordLoginAttemptAsync(loginHistory);

        // Generar JWT
        var encryptedPayload = EncryptPayload($"{user.BrandPartnerUserId}|{user.Email}|BrandPartner");
        var jwtCommand = new BusinessLayer.Shared.Commands.GenerateJwtCommand
        {
            EncryptedPayload = encryptedPayload
        };
        var token = await _jwtService.GenerateTokenAsync(jwtCommand);

        return new VerifyTwoFactorCodeResult
        {
            Success = true,
            Message = "Login successful.",
            Token = token.Token,
            RequiresPasswordChange = user.RequirePasswordChangeOnNextLogin,
            User = new
            {
                user.BrandPartnerUserId,
                user.CustomerId,
                user.Email,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.IsActive,
                user.EmailVerified,
                user.RequirePasswordChangeOnNextLogin
            }
        };
    }

    /// <summary>
    /// Restablecer contraseña (olvidé mi contraseña)
    /// </summary>
    public async Task<ResetPasswordBrandPartnerResult> ResetPasswordAsync(ResetPasswordBrandPartnerRequest request)
    {
        var user = await _userQueryRepository.GetByEmailAsync(request.Email);
        if (user == null)
        {
            // Por seguridad, no revelar si el email existe
            return new ResetPasswordBrandPartnerResult
            {
                Success = true,
                Message = "If the email exists, a password reset link has been sent."
            };
        }

        if (!user.IsActive)
        {
            return new ResetPasswordBrandPartnerResult
            {
                Success = false,
                Message = "Account is inactive."
            };
        }

        // Generar contraseña temporal
        var temporaryPassword = GenerateTemporaryPassword();
        var newPasswordHash = _userCommandRepository.HashPassword(temporaryPassword);

        // Actualizar contraseña y marcar para cambio obligatorio
        await _userCommandRepository.UpdatePasswordAsync(user.BrandPartnerUserId, newPasswordHash);
        await _userCommandRepository.UpdateRequirePasswordChangeAsync(user.BrandPartnerUserId, true);

        // Enviar email
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        var templateData = new
        {
            fullName,
            email = user.Email,
            newPassword = temporaryPassword
        };

        await _sendGridService.SendTemplateEmailAsync(
            user.Email,
            _sendGridSettings.Value.BrandPartnerResetPasswordTemplateId,
            templateData,
            null);

        return new ResetPasswordBrandPartnerResult
        {
            Success = true,
            Message = "A temporary password has been sent to your email.",
            TemporaryPassword = temporaryPassword
        };
    }

    /// <summary>
    /// Cambiar contraseña
    /// </summary>
    public async Task<ChangePasswordBrandPartnerResult> ChangePasswordAsync(ChangePasswordBrandPartnerCommand command)
    {
        var user = await _userQueryRepository.GetByIdAsync(command.BrandPartnerUserId);
        if (user == null)
        {
            return new ChangePasswordBrandPartnerResult
            {
                Success = false,
                Message = "User not found."
            };
        }

        // Verificar contraseña actual
        var oldPasswordHash = _userCommandRepository.HashPassword(command.OldPassword);
        if (user.PasswordHash != oldPasswordHash)
        {
            return new ChangePasswordBrandPartnerResult
            {
                Success = false,
                Message = "Current password is incorrect."
            };
        }

        // Actualizar contraseña
        var newPasswordHash = _userCommandRepository.HashPassword(command.NewPassword);
        await _userCommandRepository.UpdatePasswordAsync(command.BrandPartnerUserId, newPasswordHash);
        await _userCommandRepository.UpdateRequirePasswordChangeAsync(command.BrandPartnerUserId, false);

        return new ChangePasswordBrandPartnerResult
        {
            Success = true,
            Message = "Password changed successfully."
        };
    }

    public async Task<List<BrandPartnerUser>> GetUsersByCustomerAsync(int customerId, bool? isActive, bool? emailVerified)
    {
        return await _userQueryRepository.GetUsersByCustomerIdAsync(customerId, isActive, emailVerified);
    }

    public async Task<List<BrandPartnerUserLoginHistory>> GetLoginHistoryAsync(
        int? brandPartnerUserId, DateTime? dateFrom, DateTime? dateTo, string? ipAddress,
        string? country, string? city, bool? loginSuccessful, int? limit)
    {
        return await _loginHistoryQueryRepository.GetLoginHistoryAsync(
            brandPartnerUserId, dateFrom, dateTo, ipAddress, country, city, loginSuccessful, limit);
    }

    private string GenerateTemporaryPassword()
    {
        const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";
        var all = upperCase + lowerCase + digits + special;

        var password = new StringBuilder();
        var rng = RandomNumberGenerator.Create();

        password.Append(GetRandomChar(upperCase, rng));
        password.Append(GetRandomChar(lowerCase, rng));
        password.Append(GetRandomChar(digits, rng));
        password.Append(GetRandomChar(special, rng));

        for (int i = 0; i < 8; i++)
            password.Append(GetRandomChar(all, rng));

        return new string(password.ToString().OrderBy(x => Guid.NewGuid()).ToArray());
    }

    private char GetRandomChar(string chars, RandomNumberGenerator rng)
    {
        var randomBytes = new byte[1];
        rng.GetBytes(randomBytes);
        return chars[randomBytes[0] % chars.Length];
    }

    private string EncryptPayload(string payload)
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Derivar la clave de 32 bytes (256 bits) desde la ValidationKey
        var key = DeriveKey(_jwtSettings.Value.ValidationKey, 32);
        
        // Generar IV aleatorio
        aes.GenerateIV();
        aes.Key = key;

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        
        // Escribir IV al inicio
        msEncrypt.Write(aes.IV, 0, aes.IV.Length);
        
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(payload);
        }
        
        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    private byte[] DeriveKey(string password, int keyLength)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        
        if (hash.Length >= keyLength)
            return hash.Take(keyLength).ToArray();
        
        var key = new byte[keyLength];
        Array.Copy(hash, key, Math.Min(hash.Length, keyLength));
        return key;
    }
}
