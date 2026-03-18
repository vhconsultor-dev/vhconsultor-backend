namespace BusinessLayer.Amazon.Commands;

/// <summary>
/// Comando para generar un access token de Amazon Vendor usando el CustomerId
/// </summary>
public class GenerateVendorAccessTokenByCustomerCommand
{
    public int CustomerId { get; set; }
}
