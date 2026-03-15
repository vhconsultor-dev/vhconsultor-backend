namespace BusinessLayer.BrandPartner.Commands;

/// <summary>
/// Command para login Brand Partner (paso 1: validar credenciales)
/// </summary>
public class BrandPartnerLoginCommand
{
    public string Email { get; set; } = string.Empty;
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
    public int? BrandPartnerUserId { get; set; }
    public DateTime? LockedUntil { get; set; }
}
