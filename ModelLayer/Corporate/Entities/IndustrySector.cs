namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Entidad que representa un sector industrial
/// </summary>
public class IndustrySector
{
    public int SectorId { get; set; }
    public string SectorName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

