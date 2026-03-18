namespace BusinessLayer.Amazon.Commands;

/// <summary>
/// Comando para generar un access token de Amazon Vendor
/// </summary>
public class GenerateVendorAccessTokenCommand
{
    public string RefreshToken { get; set; } = string.Empty;
}
