namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Entity for Corporate.AmazonAccountAsins table.
/// </summary>
public class AmazonAccountAsin
{
    public int AmazonAccountAsinId { get; set; }
    public int AmazonAccountId { get; set; }
    public string Asin { get; set; } = string.Empty;
    public string? ProductTitle { get; set; }
    public string? ManufacturerCode { get; set; }
    public string? ParentAsin { get; set; }
    public string? Upc { get; set; }
    public string? Ean { get; set; }
    public string? Isbn { get; set; }
    public string? ModelNumber { get; set; }
    public int? CategoryId { get; set; }
    public int? SubCategoryId { get; set; }
    public int? ProductGroupId { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public int? ReplenishmentCategoryId { get; set; }
    public string? PrepInstructionsRequired { get; set; }
    public string? PrepInstructionsVendorState { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
}
