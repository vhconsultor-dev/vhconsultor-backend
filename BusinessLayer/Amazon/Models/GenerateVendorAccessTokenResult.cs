namespace BusinessLayer.Amazon.Models;

/// <summary>
/// Resultado de la generación de un access token de Amazon Vendor
/// </summary>
public class GenerateVendorAccessTokenResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
}

/// <summary>
/// Respuesta de Amazon para generación de token
/// </summary>
public class AmazonVendorTokenResponse
{
    public string access_token { get; set; } = string.Empty;
    public string refresh_token { get; set; } = string.Empty;
    public string token_type { get; set; } = string.Empty;
    public int expires_in { get; set; }
}
