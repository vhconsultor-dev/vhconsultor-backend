namespace BusinessLayer.BrandPartner.Commands;

/// <summary>
/// Command para cambiar contraseña Brand Partner
/// </summary>
public class ChangePasswordBrandPartnerCommand
{
    public int BrandPartnerUserId { get; set; }
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Resultado de cambio de contraseña
/// </summary>
public class ChangePasswordBrandPartnerResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
