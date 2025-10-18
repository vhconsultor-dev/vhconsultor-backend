using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ApplicationLayer.Shared;
using ApiLayer.Tools;
using BusinessLayer.Shared.Commands;
using System.Security.Cryptography;
using System.Text;

namespace ApiLayer.Controllers.Shared;

[ApiController]
[Route("api/[controller]")]
public class SecurityController : ControllerBase
{
    private readonly JwtService _jwtService;

    public SecurityController(JwtService jwtService)
    {
        _jwtService = jwtService;
    }

    /// <summary>
    /// Genera un JWT basado en un payload encriptado
    /// </summary>
    /// <param name="command">Comando con el payload encriptado</param>
    /// <returns>Token JWT si la validación es exitosa</returns>
    [HttpPost("generate-token")]
    [AllowAnonymous]
    public async Task<IActionResult> GenerateToken([FromBody] GenerateJwtCommand command)
    {
        var result = await _jwtService.GenerateTokenAsync(command);

        if (!result.Success)
        {
            var errorResponse = ResponseStructure<object>.BadRequest(result.Message);
            return BadRequest(errorResponse);
        }

        var response = ResponseStructure<GenerateJwtResponse>.Success(result, result.Message);
        return Ok(response);
    }

    /// <summary>
    /// Valida un JWT token
    /// </summary>
    /// <param name="token">Token JWT a validar</param>
    /// <returns>Información sobre la validez del token</returns>
    [HttpPost("validate-token")]
    [AllowAnonymous]
    public IActionResult ValidateToken([FromBody] ValidateTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.Token))
        {
            var errorResponse = ResponseStructure<object>.BadRequest("El token es requerido");
            return BadRequest(errorResponse);
        }

        var isValid = _jwtService.ValidateToken(request.Token);
        var decodedToken = _jwtService.DecodeToken(request.Token);

        var result = new
        {
            isValid = isValid,
            token = request.Token,
            decoded = decodedToken != null ? new
            {
                issuer = decodedToken.Issuer,
                audience = decodedToken.Audiences.FirstOrDefault(),
                issuedAt = decodedToken.IssuedAt,
                expires = decodedToken.ValidTo,
                claims = decodedToken.Claims.Select(c => new { type = c.Type, value = c.Value }).ToList()
            } : null,
            message = isValid ? "Token válido" : "Token inválido o expirado"
        };

        var response = ResponseStructure<object>.Success(result, result.message);
        return Ok(response);
    }

    /// <summary>
    /// Endpoint de prueba para generar un payload encriptado (SOLO PARA DESARROLLO)
    /// Este endpoint ayuda a los desarrolladores a probar el sistema de generación de JWT
    /// </summary>
    /// <param name="request">Solicitud con el secretKey y opcionalmente un timestamp</param>
    /// <returns>Payload encriptado listo para usar en generate-token</returns>
    [HttpPost("test/encrypt-payload")]
    [AllowAnonymous]
    public IActionResult EncryptPayloadForTesting([FromBody] EncryptPayloadRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.SecretKey))
            {
                var errorResponse = ResponseStructure<object>.BadRequest("El SecretKey es requerido");
                return BadRequest(errorResponse);
            }

            if (string.IsNullOrEmpty(request.ValidationKey))
            {
                var errorResponse = ResponseStructure<object>.BadRequest("El ValidationKey es requerido");
                return BadRequest(errorResponse);
            }

            // Si no se proporciona timestamp, usar el actual en UTC-6
            var timestamp = request.Timestamp ?? DateTimeOffset.UtcNow.AddHours(-6).ToUnixTimeMilliseconds();

            // Construir el payload: SecretKey|Timestamp
            var payload = $"{request.SecretKey}|{timestamp}";

            // Encriptar el payload
            var encryptedPayload = EncryptPayload(payload, request.ValidationKey);

            var result = new
            {
                encryptedPayload = encryptedPayload,
                originalPayload = payload,
                timestamp = timestamp,
                timestampDateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime.ToString("O"),
                validUntil = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).AddMinutes(1).DateTime.ToString("O"),
                warning = "Este payload es válido por aproximadamente 1 minuto desde el timestamp generado"
            };

            var response = ResponseStructure<object>.Success(result, "Payload encriptado generado exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al encriptar el payload: {ex.Message}");
            return BadRequest(errorResponse);
        }
    }

    /// <summary>
    /// Endpoint de prueba completo: genera el payload encriptado y luego el JWT
    /// </summary>
    [HttpPost("test/generate-test-token")]
    [AllowAnonymous]
    public async Task<IActionResult> GenerateTestToken([FromBody] GenerateTestTokenRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.SecretKey))
            {
                var errorResponse = ResponseStructure<object>.BadRequest("El SecretKey es requerido");
                return BadRequest(errorResponse);
            }

            if (string.IsNullOrEmpty(request.ValidationKey))
            {
                var errorResponse = ResponseStructure<object>.BadRequest("El ValidationKey es requerido");
                return BadRequest(errorResponse);
            }

            // Generar timestamp actual en UTC-6
            var timestamp = DateTimeOffset.UtcNow.AddHours(-6).ToUnixTimeMilliseconds();
            var payload = $"{request.SecretKey}|{timestamp}";

            // Encriptar el payload
            var encryptedPayload = EncryptPayload(payload, request.ValidationKey);

            // Generar el token
            var command = new GenerateJwtCommand { EncryptedPayload = encryptedPayload };
            var tokenResult = await _jwtService.GenerateTokenAsync(command);

            if (!tokenResult.Success)
            {
                var errorResponse = ResponseStructure<object>.BadRequest($"Error al generar el token: {tokenResult.Message}");
                return BadRequest(errorResponse);
            }

            var result = new
            {
                step1_encryptedPayload = encryptedPayload,
                step2_jwtToken = tokenResult.Token,
                expiresAt = tokenResult.ExpiresAt,
                timestamp = timestamp,
                timestampDateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime.ToString("O"),
                instructions = new
                {
                    step1 = "Se generó el payload encriptado usando AES-256",
                    step2 = "Se validó el payload (SecretKey + Timestamp)",
                    step3 = "Se generó el JWT exitosamente",
                    usage = "Usa el JWT (step2_jwtToken) en el header Authorization: Bearer {token}"
                }
            };

            var response = ResponseStructure<object>.Success(result, "Token de prueba generado exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al generar el token de prueba: {ex.Message}");
            return BadRequest(errorResponse);
        }
    }

    #region Private Methods

    /// <summary>
    /// Encripta un payload usando AES-256
    /// </summary>
    private string EncryptPayload(string payload, string validationKey)
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Derivar la clave de 32 bytes (256 bits) desde la ValidationKey
        var key = DeriveKey(validationKey, 32);
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        
        // Escribir IV al inicio del stream
        msEncrypt.Write(aes.IV, 0, aes.IV.Length);
        
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(payload);
        }

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    /// <summary>
    /// Deriva una clave de longitud específica usando SHA256
    /// </summary>
    private byte[] DeriveKey(string password, int keyLength)
    {
        using var sha256 = SHA256.Create();
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(passwordBytes);

        if (hash.Length == keyLength)
            return hash;

        var key = new byte[keyLength];
        Array.Copy(hash, key, Math.Min(hash.Length, keyLength));
        return key;
    }

    #endregion

    #region Request Models

    public class ValidateTokenRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    public class EncryptPayloadRequest
    {
        public string SecretKey { get; set; } = string.Empty;
        public string ValidationKey { get; set; } = string.Empty;
        public long? Timestamp { get; set; }
    }

    public class GenerateTestTokenRequest
    {
        public string SecretKey { get; set; } = string.Empty;
        public string ValidationKey { get; set; } = string.Empty;
    }

    #endregion
}

