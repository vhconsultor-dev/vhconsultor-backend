namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Define rangos de presupuesto anual y porcentajes para calcular comisiones basadas en porcentaje (% Scope Platform)
/// </summary>
public class ServiceBudgetRange
{
    public int ServiceBudgetRangeId { get; set; }
    public int ServiceId { get; set; }
    public int BusinessTypeId { get; set; }
    public int? PlatformId { get; set; }
    public decimal MinBudgetValue { get; set; }
    public decimal? MaxBudgetValue { get; set; }
    public decimal Percentage { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

