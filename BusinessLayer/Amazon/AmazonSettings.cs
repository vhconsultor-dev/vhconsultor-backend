namespace BusinessLayer.Amazon;

/// <summary>
/// Configuración para Amazon SP-API
/// </summary>
public class AmazonSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = "https://api.amazon.com/auth/o2/token";
    public string GrantType { get; set; } = "refresh_token";
}
