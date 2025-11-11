namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Define rangos de presupuesto de publicidad y valores fijos (Quote Paid Advertising)
/// </summary>
public class ServiceAdBudgetRange
{
    public int ServiceAdBudgetRangeId { get; set; }
    public int ServiceId { get; set; }
    public int BusinessTypeId { get; set; }
    public int? PlatformId { get; set; }
    public decimal MinAdBudgetValue { get; set; }
    public decimal? MaxAdBudgetValue { get; set; }
    public decimal FixedQuote { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

