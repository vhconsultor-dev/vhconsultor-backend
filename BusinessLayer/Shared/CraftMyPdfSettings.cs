namespace BusinessLayer.Shared;

/// <summary>
/// Configuración para la integración con CraftMyPDF
/// </summary>
public class CraftMyPdfSettings
{
    /// <summary>
    /// API Key de CraftMyPDF
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// URL base de la API de CraftMyPDF
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.craftmypdf.com/v1";

    /// <summary>
    /// Timeout en segundos para las peticiones HTTP
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Template ID en CraftMyPDF para generar PDFs de facturas (invoice).
    /// </summary>
    public string InvoiceTemplateId { get; set; } = string.Empty;
}
