namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Entity for Corporate.ReplenishmentCategories table (catálogo).
/// </summary>
public class ReplenishmentCategory
{
    public int ReplenishmentCategoryId { get; set; }
    public string ReplenishmentCategoryCode { get; set; } = string.Empty;
    public string ReplenishmentCategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}
