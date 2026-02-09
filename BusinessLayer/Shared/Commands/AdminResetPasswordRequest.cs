namespace BusinessLayer.Shared.Commands;

/// <summary>
/// Command for admin to reset a user's password. Generates a new temporary password automatically.
/// </summary>
public class AdminResetPasswordRequest
{
    public int UserId { get; set; }
}
