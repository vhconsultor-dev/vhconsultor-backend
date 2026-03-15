namespace BusinessLayer.BrandPartner.Commands;

/// <summary>
/// Request para crear un usuario Brand Partner
/// </summary>
public class CreateBrandPartnerUserRequest
{
    public int CustomerId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Resultado de creación de usuario Brand Partner
/// </summary>
public class CreateBrandPartnerUserResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int BrandPartnerUserId { get; set; }
    public object? User { get; set; }
}
