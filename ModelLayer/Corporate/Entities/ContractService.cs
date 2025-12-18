namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Entidad que representa un servicio dentro de un contrato
/// </summary>
public class ContractService
{
    public int ContractServiceId { get; set; }
    public int ContractId { get; set; }
    public int? ServiceId { get; set; }
    public string? ServiceDescription { get; set; }
    public string? Regions { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public decimal? FinalPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public int? ServiceOrder { get; set; }
    public string? BillingFrequency { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

