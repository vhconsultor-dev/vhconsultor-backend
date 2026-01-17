namespace BusinessLayer.Amazon.Commands;

/// <summary>
/// Comando para generar un access token de Amazon
/// </summary>
public class GenerateAccessTokenCommand
{
    public string RefreshToken { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
