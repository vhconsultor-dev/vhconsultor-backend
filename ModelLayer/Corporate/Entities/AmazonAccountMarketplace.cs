namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Entity for Corporate.AmazonAccountMarketplaces table (links Amazon Accounts to Marketplaces).
/// </summary>
public class AmazonAccountMarketplace
{
    public int AmazonAccountMarketplaceId { get; set; }
    public int AmazonAccountId { get; set; }
    public int AmazonMarketplaceId { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
