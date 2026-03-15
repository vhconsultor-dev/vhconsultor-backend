namespace BusinessLayer.BrandPartner.Commands;

/// <summary>
/// Request body for login API. Only email and password are sent by the client.
/// IPAddress and UserAgent are set by the backend from the HTTP context.
/// </summary>
public class BrandPartnerLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Command para login Brand Partner (paso 1: validar credenciales). Used internally.
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
