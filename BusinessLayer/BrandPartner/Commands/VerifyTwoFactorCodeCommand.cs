namespace BusinessLayer.BrandPartner.Commands;

/// <summary>
/// Command para verificar código 2FA (paso 2 del login)
/// </summary>
public class VerifyTwoFactorCodeCommand
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? SessionId { get; set; }
}

/// <summary>
/// Resultado de verificación 2FA
/// </summary>
public class VerifyTwoFactorCodeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public object? User { get; set; }
    public bool RequiresPasswordChange { get; set; }
}
