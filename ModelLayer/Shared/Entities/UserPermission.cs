namespace ModelLayer.Shared.Entities;

/// <summary>
/// Entidad UserPermission - Tabla [Global].[UserPermissions]
/// Permisos directos por usuario (para casos especiales)
/// </summary>
public class UserPermission
{
    public int UserPermissionId { get; set; }
    public int UserId { get; set; }
    public int PermissionId { get; set; }
    public int? GrantedBy { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
}

