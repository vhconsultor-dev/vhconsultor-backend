namespace ModelLayer.Shared.Entities;

/// <summary>
/// Entidad User - Tabla [Global].[Users]
/// </summary>
public class User
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public bool IsCorporate { get; set; }
    public bool IsBrandPartner { get; set; }
    public bool IsActive { get; set; }
    public bool EmailVerified { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastLogin { get; set; }
    public string? LastLoginIP { get; set; }
    public string? LastLoginLocation { get; set; }
    public string? LastLoginCountry { get; set; }
    public string? LastLoginCity { get; set; }
    public string? LastLoginUserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? LastModifiedBy { get; set; }
}

