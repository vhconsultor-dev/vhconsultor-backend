namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Entity for Corporate.Categories table (catálogo).
/// </summary>
public class Category
{
    public int CategoryId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}
