namespace ModelLayer.Shared.Entities;

/// <summary>
/// Entidad Permission - Tabla [Global].[Permissions]
/// Combina Recurso + Acción = Permiso específico
/// </summary>
public class Permission
{
    public int PermissionId { get; set; }
    public int ResourceId { get; set; }
    public int ActionId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string PermissionKey { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

