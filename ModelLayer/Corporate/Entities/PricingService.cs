namespace ModelLayer.Corporate.Entities;

public class PricingService
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceKey { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ServiceCategory { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
