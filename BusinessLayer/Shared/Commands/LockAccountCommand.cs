namespace BusinessLayer.Shared.Commands;

/// <summary>
/// Comando para bloquear cuenta manualmente
/// </summary>
public class LockAccountCommand
{
    public int UserId { get; set; }
    public int LockDurationMinutes { get; set; } = 30; // Por defecto 30 minutos
    public string? Reason { get; set; }
    public string? LockedBy { get; set; }
}

