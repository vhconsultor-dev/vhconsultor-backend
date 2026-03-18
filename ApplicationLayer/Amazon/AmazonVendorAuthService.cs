using System.Text.Json;
using BusinessLayer.Amazon.Commands;
using BusinessLayer.Amazon.Models;
using BusinessLayer.Corporate.Queries;

namespace ApplicationLayer.Amazon;

/// <summary>
/// Servicio de aplicación para autenticación de Vendor con Amazon SP-API
/// </summary>
public class AmazonVendorAuthService
{
    private readonly HttpClient _httpClient;
    private readonly CustomerQueryRepository _customerQueryRepository;
    private readonly AmazonAccountQueryRepository _amazonAccountQueryRepository;

    public AmazonVendorAuthService(
        HttpClient httpClient,
        CustomerQueryRepository customerQueryRepository,
        AmazonAccountQueryRepository amazonAccountQueryRepository)
    {
        _httpClient = httpClient;
        _customerQueryRepository = customerQueryRepository;
        _amazonAccountQueryRepository = amazonAccountQueryRepository;
    }

    /// <summary>
    /// Genera un nuevo access token de Vendor usando el refresh token
    /// </summary>
    public async Task<GenerateVendorAccessTokenResult> GenerateVendorAccessTokenAsync(
        GenerateVendorAccessTokenCommand command,
        string clientId,
        string clientSecret,
        string grantType,
        string amazonTokenUrl)
    {
        try
        {
            // Preparar el request body según la imagen de Postman
            var requestBody = new Dictionary<string, string>
            {
                { "grant_type", grantType },
                { "refresh_token", command.RefreshToken },
                { "client_id", clientId },
                { "client_secret", clientSecret }
            };

            var content = new FormUrlEncodedContent(requestBody);
            
            // Configurar headers necesarios
            var request = new HttpRequestMessage(HttpMethod.Post, amazonTokenUrl)
            {
                Content = content
            };
            
            request.Headers.Add("Accept", "application/json");
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

            // Hacer la petición a Amazon
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                
                // Intentar parsear el error de Amazon para dar un mensaje más claro
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<AmazonVendorErrorResponse>(errorContent, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (errorResponse != null)
                    {
                        var errorMessage = errorResponse.error_description ?? errorResponse.error ?? "Error desconocido";
                        
                        if (errorResponse.error == "invalid_grant")
                        {
                            errorMessage = "El refresh token no es válido. Puede haber sido revocado o expirado. Verifica que el refresh token sea correcto.";
                        }
                        else if (errorResponse.error == "invalid_client")
                        {
                            errorMessage = "Las credenciales del cliente (client_id o client_secret) no son válidas. Verifica las variables de entorno en Azure.";
                        }
                        
                        return new GenerateVendorAccessTokenResult
                        {
                            Success = false,
                            Message = $"Error de Amazon: {errorMessage}"
                        };
                    }
                }
                catch
                {
                    // Si no se puede parsear el error, usar el mensaje crudo
                }
                
                return new GenerateVendorAccessTokenResult
                {
                    Success = false,
                    Message = $"Error al generar token. Status: {response.StatusCode}. Respuesta: {errorContent}"
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<AmazonVendorTokenResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token))
            {
                return new GenerateVendorAccessTokenResult
                {
                    Success = false,
                    Message = "La respuesta de Amazon no contiene un access token válido"
                };
            }

            return new GenerateVendorAccessTokenResult
            {
                Success = true,
                Message = "Vendor access token generado exitosamente",
                AccessToken = tokenResponse.access_token,
                RefreshToken = tokenResponse.refresh_token ?? command.RefreshToken,
                TokenType = tokenResponse.token_type ?? "bearer",
                ExpiresIn = tokenResponse.expires_in
            };
        }
        catch (HttpRequestException ex)
        {
            return new GenerateVendorAccessTokenResult
            {
                Success = false,
                Message = $"Error de conexión con Amazon: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new GenerateVendorAccessTokenResult
            {
                Success = false,
                Message = $"Error inesperado: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Genera un nuevo access token de Vendor buscando el refresh token por CustomerId
    /// </summary>
    public async Task<GenerateVendorAccessTokenResult> GenerateVendorAccessTokenByCustomerAsync(
        GenerateVendorAccessTokenByCustomerCommand command,
        string clientId,
        string clientSecret,
        string grantType,
        string amazonTokenUrl)
    {
        try
        {
            // 1. Validar que el customer existe
            var customer = await _customerQueryRepository.GetByIdAsync(command.CustomerId);
            if (customer == null)
            {
                return new GenerateVendorAccessTokenResult
                {
                    Success = false,
                    Message = $"No se encontró el cliente con ID {command.CustomerId}. Verifica que el CustomerId sea correcto."
                };
            }

            // 2. Buscar la cuenta de Amazon asociada al customer que sea Vendor y esté activa
            var amazonAccounts = await _amazonAccountQueryRepository.GetAmazonAccountsAsync(
                customerId: command.CustomerId,
                isVendor: true,
                isActive: true);

            var amazonAccount = amazonAccounts.FirstOrDefault();

            if (amazonAccount == null)
            {
                return new GenerateVendorAccessTokenResult
                {
                    Success = false,
                    Message = $"No se encontró una cuenta de Amazon Vendor activa para el cliente '{customer.CompanyName}' (ID: {command.CustomerId}). " +
                             "Verifica que el cliente tenga una cuenta de Amazon configurada como Vendor (IsVendor = true) y que esté activa."
                };
            }

            // 3. Validar que tenga refresh token
            if (string.IsNullOrWhiteSpace(amazonAccount.RefreshToken))
            {
                return new GenerateVendorAccessTokenResult
                {
                    Success = false,
                    Message = $"La cuenta de Amazon del cliente '{customer.CompanyName}' (AmazonAccountId: {amazonAccount.AmazonAccountId}) " +
                             "no tiene un Refresh Token configurado. Por favor, configura el Refresh Token en la cuenta de Amazon antes de generar tokens."
                };
            }

            // 4. Preparar el request body
            var requestBody = new Dictionary<string, string>
            {
                { "grant_type", grantType },
                { "refresh_token", amazonAccount.RefreshToken },
                { "client_id", clientId },
                { "client_secret", clientSecret }
            };

            var content = new FormUrlEncodedContent(requestBody);
            
            // 5. Configurar headers necesarios
            var request = new HttpRequestMessage(HttpMethod.Post, amazonTokenUrl)
            {
                Content = content
            };
            
            request.Headers.Add("Accept", "application/json");
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

            // 6. Hacer la petición a Amazon
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                
                // Intentar parsear el error de Amazon para dar un mensaje más claro
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<AmazonVendorErrorResponse>(errorContent, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (errorResponse != null)
                    {
                        var errorMessage = errorResponse.error_description ?? errorResponse.error ?? "Error desconocido";
                        
                        if (errorResponse.error == "invalid_grant")
                        {
                            errorMessage = $"El Refresh Token de la cuenta de Amazon del cliente '{customer.CompanyName}' no es válido o ha expirado. " +
                                         "Es posible que el token haya sido revocado. Por favor, actualiza el Refresh Token en la cuenta de Amazon.";
                        }
                        else if (errorResponse.error == "invalid_client")
                        {
                            errorMessage = "Las credenciales del cliente (Client ID o Client Secret) configuradas en Azure no son válidas. " +
                                         "Verifica las variables de entorno AmazonVendor__ClientId y AmazonVendor__ClientSecret.";
                        }
                        else if (errorResponse.error == "unauthorized_client")
                        {
                            errorMessage = "El cliente no está autorizado para usar este grant type. Verifica la configuración en Amazon Developer Console.";
                        }
                        
                        return new GenerateVendorAccessTokenResult
                        {
                            Success = false,
                            Message = $"Error de Amazon: {errorMessage}"
                        };
                    }
                }
                catch
                {
                    // Si no se puede parsear el error, usar el mensaje crudo
                }
                
                return new GenerateVendorAccessTokenResult
                {
                    Success = false,
                    Message = $"Amazon respondió con error. Status: {response.StatusCode}. " +
                             $"Cliente: '{customer.CompanyName}'. " +
                             $"Detalle: {errorContent}"
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<AmazonVendorTokenResponse>(responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token))
            {
                return new GenerateVendorAccessTokenResult
                {
                    Success = false,
                    Message = $"Amazon respondió correctamente pero no devolvió un access token válido. Cliente: '{customer.CompanyName}'."
                };
            }

            return new GenerateVendorAccessTokenResult
            {
                Success = true,
                Message = $"Vendor access token generado exitosamente para el cliente '{customer.CompanyName}' (CustomerId: {command.CustomerId})",
                AccessToken = tokenResponse.access_token,
                RefreshToken = tokenResponse.refresh_token ?? amazonAccount.RefreshToken,
                TokenType = tokenResponse.token_type ?? "bearer",
                ExpiresIn = tokenResponse.expires_in
            };
        }
        catch (HttpRequestException ex)
        {
            return new GenerateVendorAccessTokenResult
            {
                Success = false,
                Message = $"Error de conexión con Amazon. No se pudo conectar al servicio de autenticación de Amazon. " +
                         $"Verifica la conectividad de red y que la URL de Amazon sea correcta. Detalle: {ex.Message}"
            };
        }
        catch (TaskCanceledException ex)
        {
            return new GenerateVendorAccessTokenResult
            {
                Success = false,
                Message = $"La petición a Amazon excedió el tiempo de espera (timeout). " +
                         $"Amazon no respondió a tiempo. Intenta nuevamente. Detalle: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new GenerateVendorAccessTokenResult
            {
                Success = false,
                Message = $"Error inesperado al generar el token: {ex.Message}"
            };
        }
    }
}

/// <summary>
/// Respuesta de error de Amazon
/// </summary>
internal class AmazonVendorErrorResponse
{
    public string error { get; set; } = string.Empty;
    public string error_description { get; set; } = string.Empty;
}
