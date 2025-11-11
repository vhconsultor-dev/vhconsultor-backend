namespace ModelLayer.Shared.Entities;

/// <summary>
/// Entidad Action - Tabla [Global].[Actions]
/// Define las acciones que se pueden realizar sobre recursos
/// </summary>
public class Action
{
    public int ActionId { get; set; }
    public string ActionName { get; set; } = string.Empty;
    public string ActionKey { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

