namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Entidad que representa un país
/// </summary>
public class Country
{
    public int CountryId { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string PhoneCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

