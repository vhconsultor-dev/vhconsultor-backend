namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Entity for Corporate.ProductGroups table (catálogo).
/// </summary>
public class ProductGroup
{
    public int ProductGroupId { get; set; }
    public string ProductGroupCode { get; set; } = string.Empty;
    public string ProductGroupName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}
