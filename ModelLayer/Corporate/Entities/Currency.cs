namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Entidad que representa una moneda
/// </summary>
public class Currency
{
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencyName { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

