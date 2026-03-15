namespace ModelLayer.BrandPartner.Entities;

/// <summary>
/// Entidad para Brand Partner Users - Tabla [BrandPartner].[BrandPartnerUsers]
/// Usuarios de clientes Brand Partner que inician sesión con email.
/// </summary>
public class BrandPartnerUser
{
    public int BrandPartnerUserId { get; set; }
    public int CustomerId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public bool EmailVerified { get; set; }
    public bool RequirePasswordChangeOnNextLogin { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastLogin { get; set; }
    public string? LastLoginIP { get; set; }
    public string? LastLoginUserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
