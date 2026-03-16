using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BusinessLayer.Shared;
using BusinessLayer.Shared.Commands;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ApplicationLayer.Shared;

public class JwtService
{
    #region Fields
    private readonly JwtSettings _jwtSettings;
    private readonly ValidationService _validationService;
    #endregion

    #region Constructor
    public JwtService(IOptions<JwtSettings> jwtSettings, ValidationService validationService)
    {
        _jwtSettings = jwtSettings.Value;
        _validationService = validationService;
    }
    #endregion

    #region Public Methods
    
    /// <summary>
    /// Genera un JWT basado en un payload encriptado
    /// </summary>
    public async Task<GenerateJwtResponse> GenerateTokenAsync(GenerateJwtCommand command)
    {
        try
        {
            // Validar el command
            var validationResult = await _validationService.ValidateAsync(command);
            if (!validationResult.IsValid)
            {
                return new GenerateJwtResponse
                {
                    Success = false,
                    Message = string.Join(", ", validationResult.Errors)
                };
            }

            // Desencriptar el payload
            var decryptedPayload = DecryptPayload(command.EncryptedPayload);
            if (string.IsNullOrEmpty(decryptedPayload))
            {
                return new GenerateJwtResponse
                {
                    Success = false,
                    Message = "No se pudo desencriptar el payload"
                };
            }

            // Extraer y validar los datos del payload
            var parts = decryptedPayload.Split('|');
            if (parts.Length != 2)
            {
                return new GenerateJwtResponse
                {
                    Success = false,
                    Message = "El formato del payload es incorrecto"
                };
            }

            var secretKey = parts[0];
            var timestampStr = parts[1];

            // Validar el SecretKey
            if (secretKey != _jwtSettings.SecretKey)
            {
                return new GenerateJwtResponse
                {
                    Success = false,
                    Message = "Clave de validación incorrecta"
                };
            }

            // Validar el timestamp (debe estar en UTC-6 y dentro de ±1 minuto)
            if (!long.TryParse(timestampStr, out var timestamp))
            {
                return new GenerateJwtResponse
                {
                    Success = false,
                    Message = "El timestamp no tiene un formato válido"
                };
            }

            var payloadTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
            var currentTimeUtcMinus6 = DateTime.UtcNow.AddHours(-6);
            var timeDifference = Math.Abs((currentTimeUtcMinus6 - payloadTime).TotalMinutes);

            if (timeDifference > 1)
            {
                return new GenerateJwtResponse
                {
                    Success = false,
                    Message = $"El timestamp ha expirado. Diferencia: {timeDifference:F2} minutos"
                };
            }

            // Generar el JWT (opcionalmente con UserId para Brand Partner)
            var token = GenerateJwtToken(command.UserId);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

            return new GenerateJwtResponse
            {
                Success = true,
                Token = token,
                ExpiresAt = expiresAt,
                Message = "Token generado exitosamente"
            };
        }
        catch (Exception ex)
        {
            return new GenerateJwtResponse
            {
                Success = false,
                Message = $"Error al generar el token: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Valida un JWT token
    /// </summary>
    public bool ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extrae información del JWT token
    /// </summary>
    public JwtSecurityToken? DecodeToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.ReadJwtToken(token);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Desencripta el payload usando AES-256
    /// </summary>
    private string DecryptPayload(string encryptedPayload)
    {
        try
        {
            var fullCipher = Convert.FromBase64String(encryptedPayload);

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Derivar la clave de 32 bytes (256 bits) desde la ValidationKey
            var key = DeriveKey(_jwtSettings.ValidationKey, 32);
            
            // Extraer IV (primeros 16 bytes) y datos encriptados
            var iv = fullCipher.Take(16).ToArray();
            var cipherText = fullCipher.Skip(16).ToArray();

            aes.Key = key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipherText);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Genera el JWT token. Si userId tiene valor (Brand Partner), se añade el claim "UserId".
    /// </summary>
    private string GenerateJwtToken(int? userId = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "VHConsultor"),
            new Claim(ClaimTypes.Role, "ApiClient"),
            new Claim("Timestamp", DateTime.UtcNow.ToString("O"))
        };
        if (userId.HasValue)
            claims.Add(new Claim("UserId", userId.Value.ToString()));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
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
}

