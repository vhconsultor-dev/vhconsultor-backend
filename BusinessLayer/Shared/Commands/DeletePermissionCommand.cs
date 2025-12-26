using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Shared.Commands;

/// <summary>
/// Command to delete a permission (soft delete by setting IsActive = false)
/// </summary>
public class DeletePermissionResponse
{
    public int PermissionId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class DeletePermissionCommand
{
    private readonly ModelLayer.DBcontext _context;

    public DeletePermissionCommand(ModelLayer.DBcontext context)
    {
        _context = context;
    }

    public async Task<DeletePermissionResponse> ExecuteAsync(int permissionId)
    {
        var permission = await _context.Permissions
            .FirstOrDefaultAsync(p => p.PermissionId == permissionId);

        if (permission == null)
        {
            throw new KeyNotFoundException($"Permission with ID {permissionId} not found");
        }

        // Check if permission is assigned to any role
        var isAssignedToRole = await _context.RolePermissions
            .AnyAsync(rp => rp.PermissionId == permissionId);

        if (isAssignedToRole)
        {
            throw new InvalidOperationException(
                $"Cannot delete permission '{permission.PermissionName}' because it is assigned to one or more roles. " +
                "Please remove it from all roles first.");
        }

        // Check if permission is assigned to any user
        var isAssignedToUser = await _context.UserPermissions
            .AnyAsync(up => up.PermissionId == permissionId && up.IsActive);

        if (isAssignedToUser)
        {
            throw new InvalidOperationException(
                $"Cannot delete permission '{permission.PermissionName}' because it is assigned to one or more users. " +
                "Please remove it from all users first.");
        }

        // Soft delete
        permission.IsActive = false;
        permission.UpdatedAt = DateTime.UtcNow.AddHours(-6); // Costa Rica time

        await _context.SaveChangesAsync();

        return new DeletePermissionResponse
        {
            PermissionId = permission.PermissionId,
            Message = "Permission deleted successfully"
        };
    }
}

