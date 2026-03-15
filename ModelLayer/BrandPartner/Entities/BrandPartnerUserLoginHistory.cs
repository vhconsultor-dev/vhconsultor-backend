namespace ModelLayer.BrandPartner.Entities;

/// <summary>
/// Entidad para Brand Partner User Login History - Tabla [BrandPartner].[BrandPartnerUserLoginHistory]
/// Historial de inicios de sesión de usuarios Brand Partner.
/// </summary>
public class BrandPartnerUserLoginHistory
{
    public int LoginHistoryId { get; set; }
    public int BrandPartnerUserId { get; set; }
    public DateTime LoginDate { get; set; }
    public string IPAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string? Location { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public bool LoginSuccessful { get; set; }
    public string? FailureReason { get; set; }
    public string? SessionId { get; set; }
}
