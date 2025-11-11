namespace ModelLayer.Shared.Entities;

/// <summary>
/// Entidad UserPermissionDenial - Tabla [Global].[UserPermissionDenials]
/// Permite denegar permisos específicos a usuarios
/// </summary>
public class UserPermissionDenial
{
    public int UserPermissionDenialId { get; set; }
    public int UserId { get; set; }
    public int PermissionId { get; set; }
    public int? DeniedBy { get; set; }
    public DateTime DeniedAt { get; set; }
    public string? Reason { get; set; }
    public bool IsActive { get; set; }
}

