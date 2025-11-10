namespace BusinessLayer.Shared.Commands;

/// <summary>
/// Comando para desbloquear cuenta
/// </summary>
public class UnlockAccountCommand
{
    public int UserId { get; set; }
    public string? UnlockedBy { get; set; }
}

