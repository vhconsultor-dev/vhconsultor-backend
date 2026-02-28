namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Entity for Corporate.SubCategories table (catálogo).
/// </summary>
public class SubCategory
{
    public int SubCategoryId { get; set; }
    public int CategoryId { get; set; }
    public string SubCategoryCode { get; set; } = string.Empty;
    public string SubCategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}
