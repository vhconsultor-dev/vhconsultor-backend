namespace ModelLayer.Shared.Entities;

/// <summary>
/// Asignación de un usuario corporate a un cliente Brand Partner.
/// Tabla [Global].[UserCustomerAssignments].
/// </summary>
public class UserCustomerAssignment
{
    public int UserCustomerAssignmentId { get; set; }
    public int UserId { get; set; }
    public int CustomerId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime AssignedAt { get; set; }
    public int? AssignedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
