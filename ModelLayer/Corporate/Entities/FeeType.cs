namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Entidad que representa un tipo de tarifa
/// </summary>
public class FeeType
{
    public int FeeTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string? TypeDescription { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

