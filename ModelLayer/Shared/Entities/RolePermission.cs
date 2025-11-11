namespace ModelLayer.Shared.Entities;

/// <summary>
/// Entidad RolePermission - Tabla [Global].[RolePermissions]
/// Asigna permisos a roles (muchos a muchos)
/// </summary>
public class RolePermission
{
    public int RolePermissionId { get; set; }
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    public int? GrantedBy { get; set; }
    public DateTime GrantedAt { get; set; }
}

