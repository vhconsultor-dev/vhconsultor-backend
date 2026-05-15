namespace ModelLayer.BrandPartner.Entities;

/// <summary>
/// DTO de usuario Brand Partner; datos provienen de <see cref="Shared.Entities.User"/> ([Global].[Users]).
/// </summary>
public class BrandPartnerUser
{
    /// <summary>Igual a <see cref="Shared.Entities.User.UserId"/> para RBAC y JWT.</summary>
    public int UserId { get; set; }
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
