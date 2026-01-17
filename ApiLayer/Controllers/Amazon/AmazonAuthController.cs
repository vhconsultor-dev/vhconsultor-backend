using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using ApplicationLayer.Amazon;
using ApplicationLayer.Shared;
using ApiLayer.Tools;
using BusinessLayer.Amazon.Commands;

namespace ApiLayer.Controllers.Amazon;

/// <summary>
/// Controlador para autenticación con Amazon SP-API
/// </summary>
[ApiController]
[Route("api/amazon/[controller]")]
public class AmazonAuthController : ControllerBase
{
    private readonly AmazonAuthService _amazonAuthService;
    private readonly ValidationService _validationService;
    private readonly IConfiguration _configuration;

    public AmazonAuthController(
        AmazonAuthService amazonAuthService,
        ValidationService validationService,
        IConfiguration configuration)
    {
        _amazonAuthService = amazonAuthService;
        _validationService = validationService;
        _configuration = configuration;
    }

    #region POST

    /// <summary>
    /// Genera un nuevo access token usando el refresh token de Amazon
    /// Si no se proporcionan credenciales en el body, usa las variables de entorno
    /// </summary>
    /// <param name="command">Datos para generar el access token (opcional, puede usar variables de entorno)</param>
    /// <returns>Access token generado</returns>
    [HttpPost("generate-token")]
    [AllowAnonymous]
    public async Task<IActionResult> GenerateAccessToken([FromBody] GenerateAccessTokenCommand? command = null)
    {
        // Si no se proporciona el comando o está vacío, usar variables de entorno
        if (command == null || 
            (string.IsNullOrEmpty(command.RefreshToken) && 
             string.IsNullOrEmpty(command.ClientId) && 
             string.IsNullOrEmpty(command.ClientSecret)))
        {
            var amazonSettings = _configuration.GetSection("Amazon");
            command = new GenerateAccessTokenCommand
            {
                RefreshToken = amazonSettings["RefreshToken"] ?? string.Empty,
                ClientId = amazonSettings["ClientId"] ?? string.Empty,
                ClientSecret = amazonSettings["ClientSecret"] ?? string.Empty
            };
        }
        else
        {
            // Completar valores faltantes con variables de entorno
            var amazonSettings = _configuration.GetSection("Amazon");
            if (string.IsNullOrEmpty(command.RefreshToken))
                command.RefreshToken = amazonSettings["RefreshToken"] ?? string.Empty;
            if (string.IsNullOrEmpty(command.ClientId))
                command.ClientId = amazonSettings["ClientId"] ?? string.Empty;
            if (string.IsNullOrEmpty(command.ClientSecret))
                command.ClientSecret = amazonSettings["ClientSecret"] ?? string.Empty;
        }

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
            var result = await _amazonAuthService.GenerateAccessTokenAsync(command);

            if (!result.Success)
            {
                var errorResponse = ResponseStructure<object>.BadRequest(result.Message);
                return BadRequest(errorResponse);
            }

            var response = ResponseStructure<object>.Success(
                new
                {
                    tokenId = result.TokenId,
                    accessToken = result.AccessToken,
                    refreshToken = result.RefreshToken,
                    tokenType = result.TokenType,
                    expiresIn = result.ExpiresIn,
                    expiresAt = result.ExpiresAt,
                    message = result.Message
                },
                result.Message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al generar el token: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Desactiva tokens expirados
    /// </summary>
    /// <returns>Cantidad de tokens desactivados</returns>
    [HttpPost("deactivate-expired")]
    [Authorize]
    public async Task<IActionResult> DeactivateExpiredTokens()
    {
        try
        {
            var count = await _amazonAuthService.DeactivateExpiredTokensAsync();

            var response = ResponseStructure<object>.Success(
                new { tokensDeactivated = count },
                $"{count} token(s) desactivado(s)");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al desactivar tokens: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region GET

    /// <summary>
    /// Obtiene el token activo más reciente
    /// </summary>
    /// <returns>Token activo</returns>
    [HttpGet("active-token")]
    [Authorize]
    public async Task<IActionResult> GetActiveToken()
    {
        try
        {
            var token = await _amazonAuthService.GetActiveTokenAsync();

            if (token == null)
            {
                var notFoundResponse = ResponseStructure<object>.NotFound("No hay tokens activos disponibles");
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(
                new
                {
                    tokenId = token.TokenId,
                    accessToken = token.AccessToken,
                    tokenType = token.TokenType,
                    expiresIn = token.ExpiresIn,
                    createdAt = token.CreatedAt,
                    expiresAt = token.ExpiresAt,
                    clientId = token.ClientId
                },
                "Token activo obtenido exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al obtener el token activo: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Obtiene tokens con filtros opcionales
    /// </summary>
    /// <param name="tokenId">ID del token</param>
    /// <param name="clientId">Client ID de Amazon</param>
    /// <param name="isActive">Filtrar por estado activo</param>
    /// <param name="createdFrom">Fecha de creación desde</param>
    /// <param name="createdTo">Fecha de creación hasta</param>
    /// <param name="limit">Límite de registros (por defecto 50)</param>
    /// <returns>Lista de tokens</returns>
    [HttpGet("tokens")]
    [Authorize]
    public async Task<IActionResult> GetTokens(
        [FromQuery] int? tokenId = null,
        [FromQuery] string? clientId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] DateTime? createdFrom = null,
        [FromQuery] DateTime? createdTo = null,
        [FromQuery] int? limit = 50)
    {
        try
        {
            var tokens = await _amazonAuthService.GetTokensAsync(
                tokenId, clientId, isActive, createdFrom, createdTo, limit);

            var tokensList = tokens.ToList();

            if (!tokensList.Any())
            {
                var notFoundResponse = ResponseStructure<object>.NotFound("No se encontraron tokens con los criterios especificados");
                return NotFound(notFoundResponse);
            }

            // Si se especificó un ID y solo hay un resultado, devolver el objeto directamente
            if (tokenId.HasValue && tokensList.Count == 1)
            {
                var token = tokensList.First();
                var response = ResponseStructure<object>.Success(
                    new
                    {
                        tokenId = token.TokenId,
                        accessToken = token.AccessToken,
                        tokenType = token.TokenType,
                        expiresIn = token.ExpiresIn,
                        createdAt = token.CreatedAt,
                        expiresAt = token.ExpiresAt,
                        isActive = token.IsActive,
                        clientId = token.ClientId,
                        notes = token.Notes
                    },
                    "Token obtenido exitosamente");
                return Ok(response);
            }

            // Devolver lista de tokens
            var tokensData = tokensList.Select(t => new
            {
                tokenId = t.TokenId,
                accessToken = t.AccessToken,
                tokenType = t.TokenType,
                expiresIn = t.ExpiresIn,
                createdAt = t.CreatedAt,
                expiresAt = t.ExpiresAt,
                isActive = t.IsActive,
                clientId = t.ClientId,
                notes = t.Notes
            });

            var listResponse = ResponseStructure<object>.Success(
                tokensData,
                $"{tokensList.Count} token(s) encontrado(s)");
            return Ok(listResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al obtener tokens: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    #endregion
}
