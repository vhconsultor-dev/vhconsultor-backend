using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using ApplicationLayer.Amazon;
using ApplicationLayer.Shared;
using ApiLayer.Tools;
using BusinessLayer.Amazon.Commands;

namespace ApiLayer.Controllers.Amazon;

/// <summary>
/// Controlador para autenticación de Vendor con Amazon SP-API
/// </summary>
[ApiController]
[Route("api/amazon/[controller]")]
public class VendorAccessTokenController : ControllerBase
{
    private readonly AmazonVendorAuthService _vendorAuthService;
    private readonly ValidationService _validationService;
    private readonly IConfiguration _configuration;

    public VendorAccessTokenController(
        AmazonVendorAuthService vendorAuthService,
        ValidationService validationService,
        IConfiguration configuration)
    {
        _vendorAuthService = vendorAuthService;
        _validationService = validationService;
        _configuration = configuration;
    }

    /// <summary>
    /// Genera un nuevo access token de Vendor usando el refresh token proporcionado en el body
    /// </summary>
    /// <param name="command">Comando con el refresh token</param>
    /// <returns>Access token generado</returns>
    [HttpPost("generate-token")]
    [Authorize]
    public async Task<IActionResult> GenerateVendorAccessToken([FromBody] GenerateVendorAccessTokenCommand command)
    {
        // Validación del comando usando FluentValidation
        var validationResult = await _validationService.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var errorResponse = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(errorResponse);
        }

        // Leer configuración de variables de entorno de Azure
        var vendorSettings = _configuration.GetSection("AmazonVendor");
        var clientId = vendorSettings["ClientId"] ?? string.Empty;
        var clientSecret = vendorSettings["ClientSecret"] ?? string.Empty;
        var grantType = vendorSettings["GrantType"] ?? "refresh_token";
        var amazonTokenUrl = vendorSettings["TokenUrl"] ?? "https://api.amazon.com/auth/o2/token";

        // Validar que las variables de entorno estén configuradas
        if (string.IsNullOrEmpty(clientId))
        {
            var errorResponse = ResponseStructure<object>.BadRequest(
                "La variable de entorno AmazonVendor__ClientId no está configurada");
            return BadRequest(errorResponse);
        }

        if (string.IsNullOrEmpty(clientSecret))
        {
            var errorResponse = ResponseStructure<object>.BadRequest(
                "La variable de entorno AmazonVendor__ClientSecret no está configurada");
            return BadRequest(errorResponse);
        }

        try
        {
            var result = await _vendorAuthService.GenerateVendorAccessTokenAsync(
                command,
                clientId,
                clientSecret,
                grantType,
                amazonTokenUrl);

            if (!result.Success)
            {
                var errorResponse = ResponseStructure<object>.BadRequest(result.Message);
                return BadRequest(errorResponse);
            }

            var response = ResponseStructure<object>.Success(
                new
                {
                    access_token = result.AccessToken,
                    refresh_token = result.RefreshToken,
                    token_type = result.TokenType,
                    expires_in = result.ExpiresIn
                },
                result.Message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al generar el vendor token: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Genera un nuevo access token de Vendor validando credenciales de Brand Partner
    /// </summary>
    /// <param name="command">Comando con email y password del usuario Brand Partner</param>
    /// <returns>Access token generado</returns>
    [HttpPost("generate-token-by-customer")]
    [Authorize]
    public async Task<IActionResult> GenerateVendorAccessTokenByCustomer([FromBody] GenerateVendorAccessTokenByCustomerCommand command)
    {
        // Validación del comando usando FluentValidation
        var validationResult = await _validationService.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var errorResponse = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(errorResponse);
        }

        // Leer configuración de variables de entorno de Azure
        var vendorSettings = _configuration.GetSection("AmazonVendor");
        var clientId = vendorSettings["ClientId"] ?? string.Empty;
        var clientSecret = vendorSettings["ClientSecret"] ?? string.Empty;
        var grantType = vendorSettings["GrantType"] ?? "refresh_token";
        var amazonTokenUrl = vendorSettings["TokenUrl"] ?? "https://api.amazon.com/auth/o2/token";

        // Validar que las variables de entorno estén configuradas
        if (string.IsNullOrEmpty(clientId))
        {
            var errorResponse = ResponseStructure<object>.BadRequest(
                "La variable de entorno AmazonVendor__ClientId no está configurada en Azure");
            return BadRequest(errorResponse);
        }

        if (string.IsNullOrEmpty(clientSecret))
        {
            var errorResponse = ResponseStructure<object>.BadRequest(
                "La variable de entorno AmazonVendor__ClientSecret no está configurada en Azure");
            return BadRequest(errorResponse);
        }

        try
        {
            var result = await _vendorAuthService.GenerateVendorAccessTokenByCustomerAsync(
                command,
                clientId,
                clientSecret,
                grantType,
                amazonTokenUrl);

            if (!result.Success)
            {
                var errorResponse = ResponseStructure<object>.BadRequest(result.Message);
                return BadRequest(errorResponse);
            }

            var response = ResponseStructure<object>.Success(
                new
                {
                    access_token = result.AccessToken,
                    refresh_token = result.RefreshToken,
                    token_type = result.TokenType,
                    expires_in = result.ExpiresIn
                },
                result.Message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al generar el vendor token por customer: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }
}
