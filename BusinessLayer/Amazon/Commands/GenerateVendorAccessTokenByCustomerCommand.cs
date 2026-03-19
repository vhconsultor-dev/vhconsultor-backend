namespace BusinessLayer.Amazon.Commands;

/// <summary>
/// Comando para generar un access token de Amazon Vendor usando credenciales de Brand Partner
/// </summary>
public class GenerateVendorAccessTokenByCustomerCommand
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
