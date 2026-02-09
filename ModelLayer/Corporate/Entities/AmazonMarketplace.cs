namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Entity for Corporate.AmazonMarketplaces table.
/// </summary>
public class AmazonMarketplace
{
    public int AmazonMarketplaceId { get; set; }
    public string AmazonMarketplaceCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public string AmazonRegion { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
