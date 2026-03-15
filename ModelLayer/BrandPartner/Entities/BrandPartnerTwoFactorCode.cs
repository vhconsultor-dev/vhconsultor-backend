namespace ModelLayer.BrandPartner.Entities;

/// <summary>
/// Entidad para Brand Partner Two Factor Codes - Tabla [BrandPartner].[BrandPartnerTwoFactorCodes]
/// Códigos de verificación 2FA: 1 letra mayúscula (A-Z) + 4 dígitos.
/// Expiran en 3 minutos desde CreatedAt.
/// </summary>
public class BrandPartnerTwoFactorCode
{
    public int TwoFactorCodeId { get; set; }
    public int BrandPartnerUserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
}
