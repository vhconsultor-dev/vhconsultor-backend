using ApplicationLayer.Security;
using ApiLayer.Tools;
using Microsoft.AspNetCore.Mvc;
using ModelLayer.Security;

namespace ApiLayer.Controllers.Security;

[ApiController]
[Route("api/[controller]")]
public class SecurityController : ControllerBase
{
    private readonly AuthenticationService _authenticationService;

    public SecurityController(AuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    #region Generate JWT

    [HttpPost("generate-jwt")]
    public async Task<IActionResult> GenerateJwt([FromBody] LoginRequest loginRequest)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            
            var response = ResponseStructure<object>.BadRequest(string.Join(", ", errors));
            return BadRequest(response);
        }

        try
        {
            var loginResponse = await _authenticationService.AuthenticateAsync(loginRequest);

            if (loginResponse == null)
            {
                var response = ResponseStructure<object>.BadRequest("Credenciales inválidas");
                return BadRequest(response);
            }

            // Si IsCookie = 1, generar cookie segura
            if (loginRequest.IsCookie == 1)
            {
                // Obtener configuración de cookies desde appsettings
                var cookieName = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:CookieName"] ?? "UnitsTrackingAuth";
                var cookieDomain = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:CookieDomain"];
                var cookieMaxAgeMinutes = int.Parse(HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:CookieMaxAgeMinutes"] ?? "60");
                var cookieSecurePolicy = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:CookieSecurePolicy"] ?? "Always";
                var cookieHttpOnly = bool.Parse(HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:CookieHttpOnly"] ?? "true");
                var cookieSameSite = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:CookieSameSite"] ?? "Strict";

                // Configurar opciones de la cookie
                var cookieOptions = new CookieOptions
                {
                    Domain = cookieDomain,
                    HttpOnly = cookieHttpOnly,
                    Secure = cookieSecurePolicy == "Always" || (cookieSecurePolicy == "SameAsRequest" && Request.IsHttps),
                    SameSite = cookieSameSite switch
                    {
                        "None" => SameSiteMode.None,
                        "Lax" => SameSiteMode.Lax,
                        "Strict" => SameSiteMode.Strict,
                        _ => SameSiteMode.Strict
                    },
                    MaxAge = TimeSpan.FromMinutes(cookieMaxAgeMinutes),
                    Path = "/"
                };

                // Agregar la cookie al response
                Response.Cookies.Append(cookieName, loginResponse.Token, cookieOptions);

                // Devolver respuesta sin el token en el body
                var successResponse = ResponseStructure<object>.Success(
                    new { 
                        message = "Autenticación exitosa. Cookie generada.",
                        user = new {
                            userId = loginResponse.UserId,
                            role = loginResponse.Role,
                            companyId = loginResponse.CompanyId,
                            branchId = loginResponse.BranchId,
                            expiresAt = loginResponse.ExpiresAt
                        }
                    },
                    "Autenticación exitosa"
                );

                return Ok(successResponse);
            }
            else
            {
                // IsCookie = 0, retornar JWT directamente en el body usando ResponseStructure
                var successResponse = ResponseStructure<LoginResponse>.Success(
                    loginResponse,
                    "Autenticación exitosa"
                );

                return Ok(successResponse);
            }
        }
        catch (Exception ex)
        {
            var response = ResponseStructure<object>.Error($"Error interno del servidor: {ex.Message}", 500);
            return StatusCode(500, response);
        }
    }

    #endregion
    
    #region Logout

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        try
        {
            // Obtener configuración de cookies desde appsettings
            var cookieName = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:CookieName"] ?? "UnitsTrackingAuth";
            var cookieDomain = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:CookieDomain"];

            // Configurar opciones de la cookie para eliminarla
            var cookieOptions = new CookieOptions
            {
                Domain = cookieDomain,
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(-1) // Fecha en el pasado para eliminar
            };

            // Eliminar la cookie
            Response.Cookies.Delete(cookieName, cookieOptions);

            var successResponse = ResponseStructure<object>.Success(
                new { message = "Sesión cerrada exitosamente" },
                "Logout exitoso"
            );

            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            var response = ResponseStructure<object>.Error($"Error interno del servidor: {ex.Message}", 500);
            return StatusCode(500, response);
        }
    }

    #endregion
    
    #region Refresh Cookie

    [HttpPost("refresh-cookie")]
    public async Task<IActionResult> RefreshCookie()
    {
        try
        {
            // Obtener configuración de cookies desde appsettings
            var cookieName = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:CookieName"] ?? "UnitsTrackingAuth";
            var cookieDomain = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:CookieDomain"];
            var cookieMaxAgeMinutes = int.Parse(HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:CookieMaxAgeMinutes"] ?? "60");
            var cookieSecurePolicy = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:CookieSecurePolicy"] ?? "Always";
            var cookieHttpOnly = bool.Parse(HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:CookieHttpOnly"] ?? "true");
            var cookieSameSite = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Jwt:CookieSameSite"] ?? "Strict";

            // Obtener el token actual de la cookie
            var currentToken = Request.Cookies[cookieName];
            
            if (string.IsNullOrEmpty(currentToken))
            {
                var response = ResponseStructure<object>.BadRequest("No se encontró token de autenticación");
                return BadRequest(response);
            }

            // Validar el token actual
            var jwtService = HttpContext.RequestServices.GetRequiredService<JwtService>();
            var principal = jwtService.ValidateToken(currentToken);

            if (principal == null)
            {
                // Token inválido, eliminar cookie y redirigir a login
                var cookieOptions = new CookieOptions
                {
                    Domain = cookieDomain,
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/",
                    Expires = DateTime.UtcNow.AddDays(-1)
                };
                Response.Cookies.Delete(cookieName, cookieOptions);

                var response = ResponseStructure<object>.BadRequest("Token expirado o inválido");
                return BadRequest(response);
            }

            // Generar nuevo token con la misma información del usuario
            var userId = principal.FindFirst("user_id")?.Value;
            var role = principal.FindFirst("role")?.Value;
            var companyId = principal.FindFirst("company_id")?.Value;
            var branchId = principal.FindFirst("branch_id")?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
            {
                var response = ResponseStructure<object>.BadRequest("Token no contiene información válida del usuario");
                return BadRequest(response);
            }

            // Convertir companyId y branchId a int
            if (!int.TryParse(companyId, out int companyIdInt) || !int.TryParse(branchId, out int branchIdInt))
            {
                var response = ResponseStructure<object>.BadRequest("Token no contiene información válida de empresa o sucursal");
                return BadRequest(response);
            }

            // Generar nuevo token
            var newToken = jwtService.GenerateToken(userId, role, companyIdInt, branchIdInt);

            // Configurar opciones de la nueva cookie
            var newCookieOptions = new CookieOptions
            {
                Domain = cookieDomain,
                HttpOnly = cookieHttpOnly,
                Secure = cookieSecurePolicy == "Always" || (cookieSecurePolicy == "SameAsRequest" && Request.IsHttps),
                SameSite = cookieSameSite switch
                {
                    "None" => SameSiteMode.None,
                    "Lax" => SameSiteMode.Lax,
                    "Strict" => SameSiteMode.Strict,
                    _ => SameSiteMode.Strict
                },
                MaxAge = TimeSpan.FromMinutes(cookieMaxAgeMinutes),
                Path = "/"
            };

            // Reemplazar la cookie con el nuevo token
            Response.Cookies.Append(cookieName, newToken, newCookieOptions);

            var successResponse = ResponseStructure<object>.Success(
                new { 
                    message = "Cookie renovada exitosamente",
                    user = new {
                        userId = userId,
                        role = role,
                        companyId = companyId,
                        branchId = branchId
                    }
                },
                "Renovación exitosa"
            );

            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            var response = ResponseStructure<object>.Error($"Error interno del servidor: {ex.Message}", 500);
            return StatusCode(500, response);
        }
    }

    #endregion
    
    #region ValidateToken

    [HttpPost("validate")]
    public IActionResult ValidateToken([FromBody] string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                var response = ResponseStructure<object>.BadRequest("Token es requerido");
                return BadRequest(response);
            }

            // Remover "Bearer " si está presente
            if (token.StartsWith("Bearer "))
            {
                token = token.Substring(7);
            }

            var jwtService = HttpContext.RequestServices.GetRequiredService<JwtService>();
            var principal = jwtService.ValidateToken(token);

            if (principal == null)
            {
                var response = ResponseStructure<object>.BadRequest("Token inválido");
                return BadRequest(response);
            }

            var tokenInfo = new
            {
                IsValid = true,
                UserId = principal.FindFirst("user_id")?.Value,
                Role = principal.FindFirst("role")?.Value,
                CompanyId = principal.FindFirst("company_id")?.Value,
                BranchId = principal.FindFirst("branch_id")?.Value
            };

            var successResponse = ResponseStructure<object>.Success(tokenInfo, "Token válido");
            return Ok(successResponse);
        }
        catch (Exception)
        {
            var response = ResponseStructure<object>.Error("Error interno del servidor", 500);
            return StatusCode(500, response);
        }
    }

    #endregion

} 