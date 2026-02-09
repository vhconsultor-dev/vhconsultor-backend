namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Entity for Corporate.AmazonAccounts table.
/// </summary>
public class AmazonAccount
{
    public int AmazonAccountId { get; set; }
    public int CustomerId { get; set; }
    public string AmazonAccountIdentifier { get; set; } = string.Empty;
    public bool IsSeller { get; set; }
    public bool IsVendor { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public string AmazonRegion { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
