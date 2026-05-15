using BusinessLayer.BrandPartner.Commands;
using BusinessLayer.BrandPartner.Queries;
using BusinessLayer.Corporate.Queries;
using BusinessLayer.Shared.Commands;
using BusinessLayer.Shared.Queries;
using ApplicationLayer.Shared;
using ModelLayer.BrandPartner.Entities;
using ModelLayer.Shared;
using ModelLayer.Shared.Entities;
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
    private readonly UserAuthCommandRepository _globalUserCommandRepository;
    private readonly UserQueryRepository _globalUserQueryRepository;
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
        UserAuthCommandRepository globalUserCommandRepository,
        UserQueryRepository globalUserQueryRepository,
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
        _globalUserCommandRepository = globalUserCommandRepository;
        _globalUserQueryRepository = globalUserQueryRepository;
        _sendGridService = sendGridService;
        _jwtService = jwtService;
        _sendGridSettings = sendGridSettings;
        _jwtSettings = jwtSettings;
    }

    /// <summary>
    /// Crea un nuevo usuario Brand Partner en [Global].[Users]
    /// </summary>
    public async Task<CreateBrandPartnerUserResult> CreateUserAsync(CreateBrandPartnerUserRequest request)
    {
        var customer = await _customerQueryRepository.GetByIdAsync(request.CustomerId);
        if (customer == null)
        {
            return new CreateBrandPartnerUserResult
            {
                Success = false,
                Message = $"Customer with ID {request.CustomerId} does not exist."
            };
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _userQueryRepository.EmailExistsForCustomerAsync(request.CustomerId, normalizedEmail);
        if (emailExists)
        {
            return new CreateBrandPartnerUserResult
            {
                Success = false,
                Message = $"A user with email '{request.Email}' already exists for this customer."
            };
        }

        var existingGlobal = await _globalUserQueryRepository.GetByUsernameOrEmailAsync(normalizedEmail);
        if (existingGlobal != null)
        {
            return new CreateBrandPartnerUserResult
            {
                Success = false,
                Message = $"The email '{request.Email}' is already registered in the system."
            };
        }

        var temporaryPassword = GenerateTemporaryPassword();
        var passwordHash = _userCommandRepository.HashPassword(temporaryPassword);

        var globalUser = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = normalizedEmail,
            Username = normalizedEmail,
            PasswordHash = passwordHash,
            PhoneNumber = request.PhoneNumber,
            IsCorporate = false,
            IsBrandPartner = true,
            CustomerId = request.CustomerId,
            IsActive = true,
            EmailVerified = false,
            RequirePasswordChangeOnNextLogin = true,
            FailedLoginAttempts = 0,
            CreatedAt = DateTimeService.GetCostaRicaNow(),
            CreatedBy = request.CreatedBy
        };

        var userId = await _globalUserCommandRepository.CreateUserAsync(globalUser);

        var fullName = $"{globalUser.FirstName} {globalUser.LastName}".Trim();
        var templateData = new Dictionary<string, object>
        {
            { "fullName", fullName },
            { "username", globalUser.Email },
            { "password", temporaryPassword }
        };
        try
        {
            await _sendGridService.SendTemplateEmailAsync(
                globalUser.Email,
                _sendGridSettings.Value.BrandPartnerResetPasswordTemplateId,
                templateData,
                null);
        }
        catch
        {
            // Usuario creado aunque falle el correo
        }

        return new CreateBrandPartnerUserResult
        {
            Success = true,
            Message = "Brand Partner user created successfully. A temporary password has been sent to the user's email.",
            UserId = userId,
            User = new
            {
                userId,
                customerId = request.CustomerId,
                email = globalUser.Email,
                firstName = globalUser.FirstName,
                lastName = globalUser.LastName,
                phoneNumber = globalUser.PhoneNumber,
                isActive = globalUser.IsActive,
                emailVerified = globalUser.EmailVerified,
                requirePasswordChangeOnNextLogin = globalUser.RequirePasswordChangeOnNextLogin
            }
        };
    }

    /// <summary>
    /// Paso 1 del login: email o username + password; envía código 2FA
    /// </summary>
    public async Task<BrandPartnerLoginResult> LoginStepOneAsync(BrandPartnerLoginCommand command)
    {
        try
        {
            var user = await _userQueryRepository.GetByEmailOrUsernameAsync(command.EmailOrUsername);

            if (user == null)
            {
                return new BrandPartnerLoginResult
                {
                    Success = false,
                    Message = "Invalid email or password."
                };
            }

            var loginHistoryRecord = new BrandPartnerUserLoginHistory
            {
                UserId = user.UserId,
                LoginDate = DateTimeService.GetCostaRicaNow(),
                IPAddress = command.IPAddress ?? "Unknown",
                UserAgent = command.UserAgent,
                Location = command.Location,
                Country = command.Country,
                City = command.City,
                LoginSuccessful = false,
                FailureReason = null
            };

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

            if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTimeService.GetCostaRicaNow())
            {
                loginHistoryRecord.FailureReason = "Account locked";
                try { await _userCommandRepository.RecordLoginAttemptAsync(loginHistoryRecord); } catch { }
                return new BrandPartnerLoginResult
                {
                    Success = false,
                    Message = $"Account is locked until {user.LockedUntil.Value:yyyy-MM-dd HH:mm:ss}",
                    LockedUntil = user.LockedUntil.Value,
                    Email = user.Email
                };
            }

            var passwordHash = _userCommandRepository.HashPassword(command.Password);
            if (user.PasswordHash != passwordHash)
            {
                loginHistoryRecord.FailureReason = "Incorrect password";
                try { await _userCommandRepository.RecordLoginAttemptAsync(loginHistoryRecord); } catch { }

                try { await _userCommandRepository.IncrementFailedLoginAttemptsAsync(user.UserId); } catch { }

                var failedAttempts = user.FailedLoginAttempts + 1;
                if (failedAttempts >= 5)
                {
                    return new BrandPartnerLoginResult
                    {
                        Success = false,
                        Message = "Too many failed attempts. Account locked for 30 minutes.",
                        Email = user.Email
                    };
                }

                return new BrandPartnerLoginResult
                {
                    Success = false,
                    Message = $"Invalid email or password. Remaining attempts: {5 - failedAttempts}",
                    Email = user.Email
                };
            }

            try { await _twoFactorCommandRepository.DeleteExpiredCodesAsync(user.UserId); } catch { }

            var twoFactorCode = await _twoFactorCommandRepository.CreateCodeAsync(user.UserId);

            var fullNameTf = $"{user.FirstName} {user.LastName}".Trim();
            var templateDataTf = new Dictionary<string, object>
            {
                { "fullName", fullNameTf },
                { "code", twoFactorCode.Code },
                { "expiresIn", "3 minutes" }
            };

            try
            {
                await _sendGridService.SendTemplateEmailAsync(
                    user.Email,
                    _sendGridSettings.Value.BrandPartnerTwoFactorCodeTemplateId,
                    templateDataTf,
                    null);
            }
            catch
            {
                return new BrandPartnerLoginResult
                {
                    Success = false,
                    Message = "Failed to send verification code. Please check your email configuration or contact support.",
                    Email = user.Email
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
                Email = user.Email
            };
        }
        catch
        {
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
        var user = await _userQueryRepository.GetByEmailOrUsernameAsync(command.Email);
        if (user == null)
        {
            return new VerifyTwoFactorCodeResult
            {
                Success = false,
                Message = "Invalid email."
            };
        }

        var validCode = await _twoFactorQueryRepository.ValidateCodeAsync(user.UserId, command.Code);
        if (validCode == null)
        {
            return new VerifyTwoFactorCodeResult
            {
                Success = false,
                Message = "Invalid or expired 2FA code."
            };
        }

        await _twoFactorCommandRepository.MarkCodeAsUsedAsync(validCode.TwoFactorCodeId);

        await _userCommandRepository.ResetFailedLoginAttemptsAsync(user.UserId);
        await _userCommandRepository.UpdateLastLoginInfoAsync(user.UserId, command.IPAddress ?? "Unknown", command.UserAgent);

        var loginHistory = new BrandPartnerUserLoginHistory
        {
            UserId = user.UserId,
            LoginDate = DateTimeService.GetCostaRicaNow(),
            IPAddress = command.IPAddress ?? "Unknown",
            UserAgent = command.UserAgent,
            LoginSuccessful = true,
            SessionId = command.SessionId
        };
        await _userCommandRepository.RecordLoginAttemptAsync(loginHistory);

        var timestampUtcMinus6 = DateTimeOffset.UtcNow.AddHours(-6).ToUnixTimeMilliseconds();
        var encryptedPayload = EncryptPayload($"{_jwtSettings.Value.SecretKey}|{timestampUtcMinus6}");
        var jwtCommand = new BusinessLayer.Shared.Commands.GenerateJwtCommand
        {
            EncryptedPayload = encryptedPayload,
            UserId = user.UserId
        };
        var tokenResult = await _jwtService.GenerateTokenAsync(jwtCommand);

        if (!tokenResult.Success)
        {
            return new VerifyTwoFactorCodeResult
            {
                Success = false,
                Message = tokenResult.Message ?? "Could not generate authentication token."
            };
        }

        return new VerifyTwoFactorCodeResult
        {
            Success = true,
            Message = "Login successful.",
            Token = tokenResult.Token,
            RequiresPasswordChange = user.RequirePasswordChangeOnNextLogin,
            User = new
            {
                userId = user.UserId,
                customerId = user.CustomerId,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                phoneNumber = user.PhoneNumber,
                isActive = user.IsActive,
                emailVerified = user.EmailVerified,
                requirePasswordChangeOnNextLogin = user.RequirePasswordChangeOnNextLogin
            }
        };
    }

    public async Task<ResetPasswordBrandPartnerResult> ResetPasswordAsync(ResetPasswordBrandPartnerRequest request)
    {
        var user = await _userQueryRepository.GetByEmailOrUsernameAsync(request.Email);
        if (user == null)
        {
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

        var temporaryPassword = GenerateTemporaryPassword();
        var newPasswordHash = _userCommandRepository.HashPassword(temporaryPassword);

        await _userCommandRepository.UpdatePasswordAsync(user.UserId, newPasswordHash);
        await _userCommandRepository.UpdateRequirePasswordChangeAsync(user.UserId, true);

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        var templateData = new Dictionary<string, object>
        {
            { "fullName", fullName },
            { "username", user.Email },
            { "password", temporaryPassword }
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

    public async Task<ChangePasswordBrandPartnerResult> ChangePasswordAsync(ChangePasswordBrandPartnerCommand command)
    {
        var user = await _userQueryRepository.GetByIdAsync(command.UserId);
        if (user == null)
        {
            return new ChangePasswordBrandPartnerResult
            {
                Success = false,
                Message = "User not found."
            };
        }

        var oldPasswordHash = _userCommandRepository.HashPassword(command.OldPassword);
        if (user.PasswordHash != oldPasswordHash)
        {
            return new ChangePasswordBrandPartnerResult
            {
                Success = false,
                Message = "Current password is incorrect."
            };
        }

        var newPasswordHash = _userCommandRepository.HashPassword(command.NewPassword);
        await _userCommandRepository.UpdatePasswordAsync(command.UserId, newPasswordHash);
        await _userCommandRepository.UpdateRequirePasswordChangeAsync(command.UserId, false);

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
        int? globalUserId, DateTime? dateFrom, DateTime? dateTo, string? ipAddress,
        string? country, string? city, bool? loginSuccessful, int? limit)
    {
        return await _loginHistoryQueryRepository.GetLoginHistoryAsync(
            globalUserId, dateFrom, dateTo, ipAddress, country, city, loginSuccessful, limit);
    }

    public async Task<InactivateBrandPartnerUserResult> InactivateUserAsync(int globalUserId)
    {
        var user = await _userQueryRepository.GetByIdAsync(globalUserId);
        if (user == null)
        {
            return new InactivateBrandPartnerUserResult
            {
                Success = false,
                Message = "User not found. The specified user ID does not exist."
            };
        }

        if (!user.IsActive)
        {
            return new InactivateBrandPartnerUserResult
            {
                Success = false,
                Message = "User is already inactive. No changes were applied."
            };
        }

        var updated = await _userCommandRepository.SetUserInactiveAsync(globalUserId);
        if (!updated)
        {
            return new InactivateBrandPartnerUserResult
            {
                Success = false,
                Message = "Unable to deactivate the user. Please try again or contact support."
            };
        }

        return new InactivateBrandPartnerUserResult
        {
            Success = true,
            Message = "User has been successfully deactivated. Access has been revoked and the account password has been reset for security."
        };
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

        return new string(password.ToString().OrderBy(_ => Guid.NewGuid()).ToArray());
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

        var key = DeriveKey(_jwtSettings.Value.ValidationKey, 32);

        aes.GenerateIV();
        aes.Key = key;

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();

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
