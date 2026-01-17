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

    /// <summary>
    /// Genera un nuevo access token usando las credenciales configuradas en variables de entorno
    /// </summary>
    /// <returns>Access token generado</returns>
    [HttpPost("generate-token")]
    [AllowAnonymous]
    public async Task<IActionResult> GenerateAccessToken()
    {
        // Usar variables de entorno configuradas en Azure
        var amazonSettings = _configuration.GetSection("Amazon");
        var command = new GenerateAccessTokenCommand
        {
            RefreshToken = amazonSettings["RefreshToken"] ?? string.Empty,
            ClientId = amazonSettings["ClientId"] ?? string.Empty,
            ClientSecret = amazonSettings["ClientSecret"] ?? string.Empty
        };

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
                    accessToken = result.AccessToken,
                    tokenType = result.TokenType,
                    expiresIn = result.ExpiresIn,
                    expiresAt = result.ExpiresAt
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
}
