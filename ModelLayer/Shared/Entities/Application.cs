namespace ModelLayer.Shared.Entities;

/// <summary>
/// Entidad Application - Tabla [Global].[Applications]
/// Define las aplicaciones del sistema (Corporate, BrandPartner, etc.)
/// </summary>
public class Application
{
    public int ApplicationId { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public string ApplicationKey { get; set; } = string.Empty;  // 'corporate' o 'brandpartner'
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

