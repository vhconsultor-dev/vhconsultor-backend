namespace BusinessLayer.Shared.Commands;

/// <summary>
/// Comando para crear un nuevo usuario
/// </summary>
public class CreateUserCommand
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public bool IsCorporate { get; set; }
    public bool IsBrandPartner { get; set; }
    public string? CreatedBy { get; set; }
}

