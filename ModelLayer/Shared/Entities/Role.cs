namespace ModelLayer.Shared.Entities;

/// <summary>
/// Entidad Role - Tabla [Global].[Roles]
/// Define los roles del sistema
/// </summary>
public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string RoleKey { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

