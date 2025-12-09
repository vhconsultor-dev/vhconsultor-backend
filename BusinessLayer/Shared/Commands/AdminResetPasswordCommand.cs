namespace BusinessLayer.Shared.Commands;

/// <summary>
/// Comando para que un administrador resetee la contraseña de un usuario
/// </summary>
public class AdminResetPasswordCommand
{
    public int UserId { get; set; }
    public int? ResetByUserId { get; set; } // ID del administrador que resetea la contraseña
}


