namespace BusinessLayer.BrandPartner.Commands;

/// <summary>
/// Request body para login paso 1. Prefer <see cref="EmailOrUsername"/>; <see cref="Email"/> existe por compatibilidad.
/// IPAddress y UserAgent los establece el backend.
/// </summary>
public class BrandPartnerLoginRequest
{
    /// <summary>Email o nombre de usuario (Global.Users.Username).</summary>
    public string EmailOrUsername { get; set; } = string.Empty;

    /// <summary>Cliente legado que envía solo «email».</summary>
    public string? Email { get; set; }

    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Command login paso 1 (interno).
/// </summary>
public class BrandPartnerLoginCommand
{
    public string EmailOrUsername { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Location { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
}

/// <summary>
/// Resultado del paso 1 de login
/// </summary>
public class BrandPartnerLoginResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool RequiresTwoFactor { get; set; }
    /// <summary>Email real para el paso verify-2FA (necesario si el usuario inició sesión por username).</summary>
    public string? Email { get; set; }
    public DateTime? LockedUntil { get; set; }
}
