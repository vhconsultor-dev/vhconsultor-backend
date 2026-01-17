using System.Text;
using System.Text.Json;
using BusinessLayer.Amazon.Commands;
using BusinessLayer.Amazon.Queries;
using ModelLayer.Amazon.Entities;
using ModelLayer.Shared;

namespace ApplicationLayer.Amazon;

/// <summary>
/// Servicio de aplicación para autenticación con Amazon SP-API
/// </summary>
public class AmazonAuthService
{
    private readonly AmazonTokenCommandRepository _tokenCommandRepository;
    private readonly AmazonTokenQueryRepository _tokenQueryRepository;
    private readonly HttpClient _httpClient;

    public AmazonAuthService(
        AmazonTokenCommandRepository tokenCommandRepository,
        AmazonTokenQueryRepository tokenQueryRepository,
        HttpClient httpClient)
    {
        _tokenCommandRepository = tokenCommandRepository;
        _tokenQueryRepository = tokenQueryRepository;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Genera un nuevo access token usando el refresh token
    /// </summary>
    public async Task<GenerateAccessTokenResult> GenerateAccessTokenAsync(GenerateAccessTokenCommand command)
    {
        try
        {
            // Preparar el request body
            var requestBody = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", command.RefreshToken },
                { "client_id", command.ClientId },
                { "client_secret", command.ClientSecret }
            };

            var content = new FormUrlEncodedContent(requestBody);

            // Hacer la petición a Amazon
            var response = await _httpClient.PostAsync(
                "https://api.amazon.com/auth/o2/token", 
                content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                
                // Intentar parsear el error de Amazon para dar un mensaje más claro
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<AmazonErrorResponse>(errorContent, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (errorResponse != null)
                    {
                        var errorMessage = errorResponse.ErrorDescription ?? errorResponse.Error ?? "Error desconocido";
                        
                        if (errorResponse.Error == "invalid_grant")
                        {
                            errorMessage = "El refresh token no es válido. Puede haber sido revocado o expirado. Verifica que el refresh token en las variables de entorno de Azure sea correcto.";
                        }
                        
                        return new GenerateAccessTokenResult
                        {
                            Success = false,
                            Message = $"Error de Amazon: {errorMessage}"
                        };
                    }
                }
                catch
                {
                    // Si no se puede parsear, usar el mensaje original
                }
                
                return new GenerateAccessTokenResult
                {
                    Success = false,
                    Message = $"Error al obtener el token de Amazon: {response.StatusCode} - {errorContent}"
                };
            }

            // Parsear la respuesta
            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<AmazonTokenResponse>(responseContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                return new GenerateAccessTokenResult
                {
                    Success = false,
                    Message = "La respuesta de Amazon no contiene un access token válido"
                };
            }

            // Crear el objeto de token para guardar
            var now = DateTimeService.GetCostaRicaNow();
            var amazonToken = new AmazonToken
            {
                RefreshToken = command.RefreshToken,
                AccessToken = tokenResponse.AccessToken,
                TokenType = tokenResponse.TokenType ?? "bearer",
                ExpiresIn = tokenResponse.ExpiresIn,
                CreatedAt = now,
                ExpiresAt = now.AddSeconds(tokenResponse.ExpiresIn),
                IsActive = true,
                ClientId = command.ClientId,
                Notes = "Token generado automáticamente"
            };

            // Guardar en la base de datos
            var tokenId = await _tokenCommandRepository.SaveTokenAsync(amazonToken);

            return new GenerateAccessTokenResult
            {
                Success = true,
                Message = "Access token generado exitosamente",
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken ?? command.RefreshToken,
                TokenType = tokenResponse.TokenType ?? "bearer",
                ExpiresIn = tokenResponse.ExpiresIn,
                TokenId = tokenId,
                ExpiresAt = amazonToken.ExpiresAt
            };
        }
        catch (HttpRequestException ex)
        {
            return new GenerateAccessTokenResult
            {
                Success = false,
                Message = $"Error de conexión con Amazon: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new GenerateAccessTokenResult
            {
                Success = false,
                Message = $"Error inesperado: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Obtiene el token activo más reciente
    /// </summary>
    public async Task<AmazonToken?> GetActiveTokenAsync()
    {
        return await _tokenQueryRepository.GetActiveTokenAsync();
    }

    /// <summary>
    /// Obtiene tokens con filtros
    /// </summary>
    public async Task<IEnumerable<AmazonToken>> GetTokensAsync(
        int? tokenId = null,
        string? clientId = null,
        bool? isActive = null,
        DateTime? createdFrom = null,
        DateTime? createdTo = null,
        int? limit = 50)
    {
        return await _tokenQueryRepository.GetTokensAsync(
            tokenId, clientId, isActive, createdFrom, createdTo, limit);
    }

    /// <summary>
    /// Desactiva tokens expirados
    /// </summary>
    public async Task<int> DeactivateExpiredTokensAsync()
    {
        return await _tokenCommandRepository.DeactivateExpiredTokensAsync();
    }
}

#region Response Classes

/// <summary>
/// Respuesta de Amazon al solicitar un token
/// </summary>
public class AmazonTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string? TokenType { get; set; }
    public int ExpiresIn { get; set; }
}

/// <summary>
/// Respuesta de error de Amazon
/// </summary>
public class AmazonErrorResponse
{
    public string? Error { get; set; }
    public string? ErrorDescription { get; set; }
    public string? ErrorIndex { get; set; }
    public string? RequestId { get; set; }
}

/// <summary>
/// Resultado de la generación de access token
/// </summary>
public class GenerateAccessTokenResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? TokenType { get; set; }
    public int ExpiresIn { get; set; }
    public int? TokenId { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

#endregion
