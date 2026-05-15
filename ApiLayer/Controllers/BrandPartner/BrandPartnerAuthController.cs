using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ApplicationLayer.BrandPartner;
using BusinessLayer.BrandPartner.Commands;
using BusinessLayer.BrandPartner.Validators;
using ModelLayer.Shared;
using FluentValidation;
using ApiLayer.Tools;

namespace ApiLayer.Controllers.BrandPartner;

[ApiController]
[Route("api/brandpartner/auth")]
public class BrandPartnerAuthController : ControllerBase
{
    private readonly BrandPartnerAuthService _authService;
    private readonly CreateBrandPartnerUserValidator _createUserValidator;
    private readonly BrandPartnerLoginValidator _loginValidator;
    private readonly VerifyTwoFactorCodeValidator _verifyCodeValidator;
    private readonly ResetPasswordBrandPartnerValidator _resetPasswordValidator;
    private readonly ChangePasswordBrandPartnerValidator _changePasswordValidator;

    public BrandPartnerAuthController(
        BrandPartnerAuthService authService,
        CreateBrandPartnerUserValidator createUserValidator,
        BrandPartnerLoginValidator loginValidator,
        VerifyTwoFactorCodeValidator verifyCodeValidator,
        ResetPasswordBrandPartnerValidator resetPasswordValidator,
        ChangePasswordBrandPartnerValidator changePasswordValidator)
    {
        _authService = authService;
        _createUserValidator = createUserValidator;
        _loginValidator = loginValidator;
        _verifyCodeValidator = verifyCodeValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _changePasswordValidator = changePasswordValidator;
    }

    /// <summary>
    /// 1. Crear usuario Brand Partner (solo admin Corporate)
    /// POST /api/brandpartner/auth/users
    /// </summary>
    [HttpPost("users")]
    [Authorize]
    public async Task<IActionResult> CreateUser([FromBody] CreateBrandPartnerUserRequest request)
    {
        try
        {
            var validationResult = await _createUserValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Ok(new ResponseStructure<object>
                {
                    Status = false,
                    StatusCode = 400,
                    Message = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)),
                    Data = null,
                    Timestamp = DateTimeService.GetCostaRicaNow()
                });
            }

            var result = await _authService.CreateUserAsync(request);

            return Ok(new ResponseStructure<object>
            {
                Status = result.Success,
                StatusCode = result.Success ? 200 : 400,
                Message = result.Message,
                Data = result.Success ? result.User : null,
                Timestamp = DateTimeService.GetCostaRicaNow()
            });
        }
        catch (Exception)
        {
            return Ok(new ResponseStructure<object>
            {
                Status = false,
                StatusCode = 500,
                Message = "An unexpected error occurred. Please try again later or contact support if the problem persists.",
                Data = null,
                Timestamp = DateTimeService.GetCostaRicaNow()
            });
        }
    }

    /// <summary>
    /// Inactivate a Brand Partner user. Sets the user as inactive and resets their password so they cannot log in. No email is sent.
    /// POST /api/brandpartner/auth/users/inactivate?userId=123 (compat: brandPartnerUserId)
    /// </summary>
    [HttpPost("users/inactivate")]
    [Authorize]
    public async Task<IActionResult> InactivateUser([FromQuery] int? userId = null, [FromQuery] int? brandPartnerUserId = null)
    {
        try
        {
            var id = userId ?? brandPartnerUserId ?? 0;
            if (id <= 0)
            {
                return Ok(new ResponseStructure<object>
                {
                    Status = false,
                    StatusCode = 400,
                    Message = "Invalid user ID. User ID (Global.Users) must be greater than zero.",
                    Data = null,
                    Timestamp = DateTimeService.GetCostaRicaNow()
                });
            }

            var result = await _authService.InactivateUserAsync(id);

            if (!result.Success)
            {
                var statusCode = result.Message.Contains("not exist") ? 404 : 400;
                return Ok(new ResponseStructure<object>
                {
                    Status = false,
                    StatusCode = statusCode,
                    Message = result.Message,
                    Data = null,
                    Timestamp = DateTimeService.GetCostaRicaNow()
                });
            }

            return Ok(new ResponseStructure<object>
            {
                Status = true,
                StatusCode = 200,
                Message = result.Message,
                Data = new { userId = id, isActive = false },
                Timestamp = DateTimeService.GetCostaRicaNow()
            });
        }
        catch (Exception)
        {
            return Ok(new ResponseStructure<object>
            {
                Status = false,
                StatusCode = 500,
                Message = "An unexpected error occurred while deactivating the user. Please try again later or contact support if the problem persists.",
                Data = null,
                Timestamp = DateTimeService.GetCostaRicaNow()
            });
        }
    }

    /// <summary>
    /// 2. Login paso 1: validar email/password y enviar código 2FA. Solo enviar email y password; IP y User-Agent los obtiene el servidor.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] BrandPartnerLoginRequest request)
    {
        try
        {
            var identifier = request.EmailOrUsername?.Trim()
                ?? string.Empty;
            if (string.IsNullOrEmpty(identifier))
                identifier = request.Email?.Trim() ?? string.Empty;

            var command = new BrandPartnerLoginCommand
            {
                EmailOrUsername = identifier,
                Password = request.Password,
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserAgent = Request.Headers["User-Agent"].ToString()
            };

            var validationResult = await _loginValidator.ValidateAsync(command);
            if (!validationResult.IsValid)
            {
                return Ok(new ResponseStructure<object>
                {
                    Status = false,
                    StatusCode = 400,
                    Message = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)),
                    Data = null,
                    Timestamp = DateTimeService.GetCostaRicaNow()
                });
            }

            var result = await _authService.LoginStepOneAsync(command);

            return Ok(new ResponseStructure<object>
            {
                Status = result.Success,
                StatusCode = result.Success ? 200 : 400,
                Message = result.Message,
                Data = result.Success
                    ? new { requiresTwoFactor = result.RequiresTwoFactor, email = result.Email }
                    : null,
                Timestamp = DateTimeService.GetCostaRicaNow()
            });
        }
        catch (Exception)
        {
            // Never expose internal exception details
            return Ok(new ResponseStructure<object>
            {
                Status = false,
                StatusCode = 500,
                Message = "An unexpected error occurred. Please try again later or contact support if the problem persists.",
                Data = null,
                Timestamp = DateTimeService.GetCostaRicaNow()
            });
        }
    }

    /// <summary>
    /// 3. Login paso 2: verificar código 2FA y obtener JWT. Solo enviar email y code; IP, User-Agent y SessionId los obtiene el servidor.
    /// </summary>
    [HttpPost("verify-2fa")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyTwoFactorCode([FromBody] VerifyTwoFactorCodeRequest request)
    {
        try
        {
            var command = new VerifyTwoFactorCodeCommand
            {
                Email = request.Email,
                Code = request.Code,
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserAgent = Request.Headers["User-Agent"].ToString(),
                SessionId = Guid.NewGuid().ToString()
            };

            var validationResult = await _verifyCodeValidator.ValidateAsync(command);
            if (!validationResult.IsValid)
            {
                return Ok(new ResponseStructure<object>
                {
                    Status = false,
                    StatusCode = 400,
                    Message = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)),
                    Data = null,
                    Timestamp = DateTimeService.GetCostaRicaNow()
                });
            }

            var result = await _authService.VerifyTwoFactorCodeAsync(command);

            return Ok(new ResponseStructure<object>
            {
                Status = result.Success,
                StatusCode = result.Success ? 200 : 400,
                Message = result.Message,
                Data = result.Success ? new
                {
                    token = result.Token,
                    user = result.User,
                    requiresPasswordChange = result.RequiresPasswordChange
                } : null,
                Timestamp = DateTimeService.GetCostaRicaNow()
            });
        }
        catch (Exception)
        {
            return Ok(new ResponseStructure<object>
            {
                Status = false,
                StatusCode = 500,
                Message = "An unexpected error occurred. Please try again later or contact support if the problem persists.",
                Data = null,
                Timestamp = DateTimeService.GetCostaRicaNow()
            });
        }
    }

    /// <summary>
    /// 4. Restablecer contraseña (olvidé mi contraseña)
    /// POST /api/brandpartner/auth/reset-password
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordBrandPartnerRequest request)
    {
        try
        {
            var validationResult = await _resetPasswordValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Ok(new ResponseStructure<object>
                {
                    Status = false,
                    StatusCode = 400,
                    Message = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)),
                    Data = null,
                    Timestamp = DateTimeService.GetCostaRicaNow()
                });
            }

            var result = await _authService.ResetPasswordAsync(request);

            return Ok(new ResponseStructure<object>
            {
                Status = result.Success,
                StatusCode = result.Success ? 200 : 400,
                Message = result.Message,
                Data = null,
                Timestamp = DateTimeService.GetCostaRicaNow()
            });
        }
        catch (Exception)
        {
            return Ok(new ResponseStructure<object>
            {
                Status = false,
                StatusCode = 500,
                Message = "An unexpected error occurred. Please try again later or contact support if the problem persists.",
                Data = null,
                Timestamp = DateTimeService.GetCostaRicaNow()
            });
        }
    }

    /// <summary>
    /// 5. Cambiar contraseña (desde perfil o forzado)
    /// POST /api/brandpartner/auth/change-password
    /// </summary>
    [HttpPost("change-password")]
    [Authorize(Roles = "ApiClient,BrandPartner")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordBrandPartnerCommand command)
    {
        try
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int jwtUserId))
            {
                return Ok(new ResponseStructure<object>
                {
                    Status = false,
                    StatusCode = 401,
                    Message = "Invalid or missing user ID in token.",
                    Data = null,
                    Timestamp = DateTimeService.GetCostaRicaNow()
                });
            }

            command.UserId = jwtUserId;

            var validationResult = await _changePasswordValidator.ValidateAsync(command);
            if (!validationResult.IsValid)
            {
                return Ok(new ResponseStructure<object>
                {
                    Status = false,
                    StatusCode = 400,
                    Message = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)),
                    Data = null,
                    Timestamp = DateTimeService.GetCostaRicaNow()
                });
            }

            var result = await _authService.ChangePasswordAsync(command);

            return Ok(new ResponseStructure<object>
            {
                Status = result.Success,
                StatusCode = result.Success ? 200 : 400,
                Message = result.Message,
                Data = null,
                Timestamp = DateTimeService.GetCostaRicaNow()
            });
        }
        catch (Exception)
        {
            return Ok(new ResponseStructure<object>
            {
                Status = false,
                StatusCode = 500,
                Message = "An unexpected error occurred. Please try again later or contact support if the problem persists.",
                Data = null,
                Timestamp = DateTimeService.GetCostaRicaNow()
            });
        }
    }

    /// <summary>
    /// 6. Listar usuarios por Customer
    /// GET /api/brandpartner/auth/users?customerId=1&isActive=true&emailVerified=false
    /// </summary>
    [HttpGet("users")]
    [Authorize]
    public async Task<IActionResult> GetUsersByCustomer(
        [FromQuery] int customerId,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? emailVerified = null)
    {
        try
        {
            if (customerId <= 0)
            {
                return Ok(new ResponseStructure<object>
                {
                    Status = false,
                    StatusCode = 400,
                    Message = "CustomerId must be greater than 0.",
                    Data = null,
                    Timestamp = DateTimeService.GetCostaRicaNow()
                });
            }

            var users = await _authService.GetUsersByCustomerAsync(customerId, isActive, emailVerified);

            return Ok(new ResponseStructure<object>
            {
                Status = true,
                StatusCode = 200,
                Message = $"Retrieved {users.Count} users.",
                Data = users,
                Timestamp = DateTimeService.GetCostaRicaNow()
            });
        }
        catch (Exception ex)
        {
            return Ok(new ResponseStructure<object>
            {
                Status = false,
                StatusCode = 500,
                Message = $"An error occurred: {ex.Message}",
                Data = null,
                Timestamp = DateTimeService.GetCostaRicaNow()
            });
        }
    }

    /// <summary>
    /// 7. Obtener historial de login
    /// GET /api/brandpartner/auth/login-history?userId=1 (compat: brandPartnerUserId)
    /// </summary>
    [HttpGet("login-history")]
    [Authorize(Roles = "Admin,Corporate,BrandPartner,ApiClient")]
    public async Task<IActionResult> GetLoginHistory(
        [FromQuery] int? userId = null,
        [FromQuery] int? brandPartnerUserId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? ipAddress = null,
        [FromQuery] string? country = null,
        [FromQuery] string? city = null,
        [FromQuery] bool? loginSuccessful = null,
        [FromQuery] int? limit = 100)
    {
        try
        {
            // Si es BrandPartner, solo puede ver su propio historial
            var filterUserId = userId ?? brandPartnerUserId;

            if (User.IsInRole("BrandPartner"))
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int tokenUserId))
                {
                    return Ok(new ResponseStructure<object>
                    {
                        Status = false,
                        StatusCode = 401,
                        Message = "Invalid or missing user ID in token.",
                        Data = null,
                        Timestamp = DateTimeService.GetCostaRicaNow()
                    });
                }
                filterUserId = tokenUserId;
            }

            var history = await _authService.GetLoginHistoryAsync(
                filterUserId, dateFrom, dateTo, ipAddress, country, city, loginSuccessful, limit);

            return Ok(new ResponseStructure<object>
            {
                Status = true,
                StatusCode = 200,
                Message = $"Retrieved {history.Count} login history records.",
                Data = history,
                Timestamp = DateTimeService.GetCostaRicaNow()
            });
        }
        catch (Exception)
        {
            return Ok(new ResponseStructure<object>
            {
                Status = false,
                StatusCode = 500,
                Message = "An unexpected error occurred. Please try again later or contact support if the problem persists.",
                Data = null,
                Timestamp = DateTimeService.GetCostaRicaNow()
            });
        }
    }
}
