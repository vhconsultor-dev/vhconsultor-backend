using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ApplicationLayer.Shared;
using ApiLayer.Tools;
using BusinessLayer.Shared.Commands;

namespace ApiLayer.Controllers.Shared;

/// <summary>
/// Controlador para autenticación y gestión de usuarios
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ValidationService _validationService;

    public AuthController(
        AuthService authService,
        ValidationService validationService)
    {
        _authService = authService;
        _validationService = validationService;
    }

    #region POST

    /// <summary>
    /// Inicia sesión de un usuario
    /// </summary>
    /// <param name="command">Credenciales de login</param>
    /// <returns>Información del usuario si el login es exitoso</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        // Validación usando FluentValidation
        var validationResult = await _validationService.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var errorResponse = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(errorResponse);
        }

        try
        {
            // Capturar información de la solicitud
            command.IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            command.UserAgent = Request.Headers["User-Agent"].ToString();

            var result = await _authService.LoginAsync(command);

            if (!result.Success)
            {
                var errorResponse = ResponseStructure<object>.BadRequest(result.Message);
                return BadRequest(errorResponse);
            }

            var response = ResponseStructure<object>.Success(
                new
                {
                    user = result.User,
                    message = result.Message
                },
                result.Message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al procesar el login: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Crea un nuevo usuario
    /// </summary>
    /// <param name="command">Datos del usuario a crear</param>
    /// <returns>Información del usuario creado</returns>
    [HttpPost("register")]
    [Authorize]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
    {
        // Validación usando FluentValidation
        var validationResult = await _validationService.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var errorResponse = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(errorResponse);
        }

        try
        {
            var result = await _authService.CreateUserAsync(command);

            if (!result.Success)
            {
                var errorResponse = ResponseStructure<object>.BadRequest(result.Message);
                return BadRequest(errorResponse);
            }

            var response = ResponseStructure<object>.Success(
                new
                {
                    userId = result.UserId,
                    user = result.User,
                    message = result.Message
                },
                result.Message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al crear el usuario: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Cambia la contraseña de un usuario
    /// </summary>
    /// <param name="command">Datos para cambio de contraseña</param>
    /// <returns>Resultado del cambio de contraseña</returns>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
    {
        // Validación usando FluentValidation
        var validationResult = await _validationService.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var errorResponse = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(errorResponse);
        }

        try
        {
            var result = await _authService.ChangePasswordAsync(command);

            if (!result.Success)
            {
                var errorResponse = ResponseStructure<object>.BadRequest(result.Message);
                return BadRequest(errorResponse);
            }

            var response = ResponseStructure<object>.Success(
                new { message = result.Message },
                result.Message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al cambiar la contraseña: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Admin resets a user's password by generating a temporary password automatically.
    /// Validates user exists, is active, and is not locked. Sends the new password via email.
    /// </summary>
    /// <param name="userId">ID of the user whose password will be reset.</param>
    [HttpPost("admin/reset-password/{userId}")]
    [Authorize]
    public async Task<IActionResult> AdminResetPassword(int userId)
    {
        try
        {
            var request = new AdminResetPasswordRequest { UserId = userId };

            // Validation
            var validationResult = await _validationService.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errorResponse = ResponseStructure<object>.ValidationError(
                    string.Join(", ", validationResult.Errors));
                return BadRequest(errorResponse);
            }

            var result = await _authService.AdminResetPasswordAsync(request);

            if (!result.Success)
            {
                var errorResponse = ResponseStructure<object>.Error(result.Message, 400);
                return BadRequest(errorResponse);
            }

            // Success - return message and optionally the temporary password for admin logging
            var response = ResponseStructure<object>.Success(
                new { message = result.Message, temporaryPassword = result.TemporaryPassword },
                result.Message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? string.Empty;
            var fullMessage = $"Error resetting user password: {ex.Message}";
            if (!string.IsNullOrEmpty(innerMessage))
                fullMessage += $" | Inner: {innerMessage}";

            var errorResponse = ResponseStructure<object>.Error(fullMessage, 500);
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Bloquea una cuenta de usuario manualmente
    /// </summary>
    /// <param name="command">Datos para bloquear cuenta</param>
    /// <returns>Resultado del bloqueo</returns>
    [HttpPost("lock-account")]
    [Authorize]
    public async Task<IActionResult> LockAccount([FromBody] LockAccountCommand command)
    {
        // Validación usando FluentValidation
        var validationResult = await _validationService.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var errorResponse = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(errorResponse);
        }

        try
        {
            var result = await _authService.LockAccountAsync(command);

            if (!result.Success)
            {
                var errorResponse = ResponseStructure<object>.BadRequest(result.Message);
                return BadRequest(errorResponse);
            }

            var response = ResponseStructure<object>.Success(
                new
                {
                    message = result.Message,
                    lockedUntil = result.LockedUntil
                },
                result.Message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al bloquear la cuenta: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Desbloquea una cuenta de usuario
    /// </summary>
    /// <param name="command">Datos para desbloquear cuenta</param>
    /// <returns>Resultado del desbloqueo</returns>
    [HttpPost("unlock-account")]
    [Authorize]
    public async Task<IActionResult> UnlockAccount([FromBody] UnlockAccountCommand command)
    {
        // Validación usando FluentValidation
        var validationResult = await _validationService.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var errorResponse = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(errorResponse);
        }

        try
        {
            var result = await _authService.UnlockAccountAsync(command);

            if (!result.Success)
            {
                var errorResponse = ResponseStructure<object>.BadRequest(result.Message);
                return BadRequest(errorResponse);
            }

            var response = ResponseStructure<object>.Success(
                new { message = result.Message },
                result.Message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al desbloquear la cuenta: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region GET

    /// <summary>
    /// Obtiene usuarios con búsqueda flexible usando parámetros opcionales
    /// </summary>
    /// <param name="userId">ID del usuario (si se especifica, devuelve solo ese usuario)</param>
    /// <param name="email">Email del usuario para búsqueda parcial</param>
    /// <param name="username">Nombre de usuario para búsqueda parcial</param>
    /// <param name="isActive">Filtrar por estado activo</param>
    /// <param name="isCorporate">Filtrar por tipo corporativo</param>
    /// <param name="isBrandPartner">Filtrar por tipo Brand Partner</param>
    /// <param name="emailVerified">Filtrar por email verificado</param>
    /// <returns>Lista de usuarios que coinciden con los criterios</returns>
    [HttpGet("users")]
    [Authorize]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int? userId = null,
        [FromQuery] string? email = null,
        [FromQuery] string? username = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isCorporate = null,
        [FromQuery] bool? isBrandPartner = null,
        [FromQuery] bool? emailVerified = null)
    {
        try
        {
            var users = await _authService.GetUsersAsync(
                userId, email, username, isActive, isCorporate, isBrandPartner, emailVerified);

            var usersList = users.ToList();

            if (!usersList.Any())
            {
                var notFoundResponse = ResponseStructure<object>.NotFound("No se encontraron usuarios con los criterios especificados");
                return NotFound(notFoundResponse);
            }

            // Si se especificó un ID y solo hay un resultado, devolver el objeto directamente
            if (userId.HasValue && usersList.Count == 1)
            {
                var response = ResponseStructure<object>.Success(usersList.First(), "Usuario obtenido exitosamente");
                return Ok(response);
            }

            // Devolver lista de usuarios
            var listResponse = ResponseStructure<object>.Success(usersList, $"{usersList.Count} usuario(s) encontrado(s)");
            return Ok(listResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al obtener usuarios: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Obtiene el historial de inicio de sesión con filtros opcionales
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="loginDateFrom">Fecha de inicio del rango</param>
    /// <param name="loginDateTo">Fecha de fin del rango</param>
    /// <param name="ipAddress">Dirección IP</param>
    /// <param name="country">País</param>
    /// <param name="city">Ciudad</param>
    /// <param name="loginSuccessful">Filtrar por login exitoso o fallido</param>
    /// <param name="limit">Límite de registros (por defecto 100)</param>
    /// <returns>Lista de registros de historial de login</returns>
    [HttpGet("login-history")]
    [Authorize]
    public async Task<IActionResult> GetLoginHistory(
        [FromQuery] int? userId = null,
        [FromQuery] DateTime? loginDateFrom = null,
        [FromQuery] DateTime? loginDateTo = null,
        [FromQuery] string? ipAddress = null,
        [FromQuery] string? country = null,
        [FromQuery] string? city = null,
        [FromQuery] bool? loginSuccessful = null,
        [FromQuery] int? limit = 100)
    {
        try
        {
            var history = await _authService.GetLoginHistoryAsync(
                userId, loginDateFrom, loginDateTo, ipAddress, country, city, loginSuccessful, limit);

            var historyList = history.ToList();

            if (!historyList.Any())
            {
                var notFoundResponse = ResponseStructure<object>.NotFound("No se encontró historial de login con los criterios especificados");
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(
                historyList,
                $"{historyList.Count} registro(s) de historial encontrado(s)");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al obtener el historial de login: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    #endregion
}

