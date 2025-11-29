namespace ModelLayer.Shared.Entities;

/// <summary>
/// Entidad UserRole - Tabla [Global].[UserRoles]
/// Asigna roles a usuarios (muchos a muchos)
/// </summary>
public class UserRole
{
    public int UserRoleId { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public int ApplicationId { get; set; }
    public int? AssignedBy { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
}

