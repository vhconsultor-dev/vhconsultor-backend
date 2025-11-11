namespace ModelLayer.Shared.Entities;

/// <summary>
/// Entidad Resource - Tabla [Global].[Resources]
/// Define los recursos/módulos del sistema
/// </summary>
public class Resource
{
    public int ResourceId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public string ResourceKey { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Module { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

