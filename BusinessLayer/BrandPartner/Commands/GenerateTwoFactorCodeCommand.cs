namespace BusinessLayer.BrandPartner.Commands;

/// <summary>
/// Command para generar código 2FA
/// </summary>
public class GenerateTwoFactorCodeCommand
{
    public int BrandPartnerUserId { get; set; }
}

/// <summary>
/// Resultado de generación de código 2FA
/// </summary>
public class GenerateTwoFactorCodeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
