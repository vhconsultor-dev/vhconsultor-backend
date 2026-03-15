namespace BusinessLayer.BrandPartner.Commands;

/// <summary>
/// Result of inactivating a Brand Partner user.
/// </summary>
public class InactivateBrandPartnerUserResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
