namespace ModelLayer.BrandPartner.Entities;

/// <summary>
/// Encabezado de settlement de Amazon
/// </summary>
public class SettlementHeader
{
    public long SettlementHeaderId { get; set; }
    public int AmazonAccountId { get; set; }
    public string SettlementId { get; set; } = string.Empty;
    public DateTime? SettlementStartDate { get; set; }
    public DateTime? SettlementEndDate { get; set; }
    public DateTime? DepositDate { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? Currency { get; set; }
    public string? SourceFileName { get; set; }
    public DateTime ImportedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
