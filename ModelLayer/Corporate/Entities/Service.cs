namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Entidad que representa un servicio corporativo
/// </summary>
public class Service
{
    public int ServiceId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string? ServiceDescription { get; set; }
    public decimal? DefaultUnitPrice { get; set; }
    public string? BillingUnit { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

