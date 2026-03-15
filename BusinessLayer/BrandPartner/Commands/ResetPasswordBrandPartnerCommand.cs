namespace BusinessLayer.BrandPartner.Commands;

/// <summary>
/// Request para restablecer contraseña Brand Partner (olvidé mi contraseña)
/// </summary>
public class ResetPasswordBrandPartnerRequest
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Resultado de restablecimiento de contraseña
/// </summary>
public class ResetPasswordBrandPartnerResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? TemporaryPassword { get; set; }
}
