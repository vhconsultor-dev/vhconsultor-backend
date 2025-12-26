using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Shared.Commands;

/// <summary>
/// Command to update an existing permission
/// </summary>
public class UpdatePermissionRequest
{
    public string PermissionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class UpdatePermissionResponse
{
    public int PermissionId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class UpdatePermissionCommand
{
    private readonly ModelLayer.DBcontext _context;

    public UpdatePermissionCommand(ModelLayer.DBcontext context)
    {
        _context = context;
    }

    public async Task<UpdatePermissionResponse> ExecuteAsync(int permissionId, UpdatePermissionRequest request)
    {
        var permission = await _context.Permissions
            .FirstOrDefaultAsync(p => p.PermissionId == permissionId);

        if (permission == null)
        {
            throw new KeyNotFoundException($"Permission with ID {permissionId} not found");
        }

        permission.PermissionName = request.PermissionName;
        permission.Description = request.Description;
        permission.IsActive = request.IsActive;
        permission.UpdatedAt = DateTime.UtcNow.AddHours(-6); // Costa Rica time

        await _context.SaveChangesAsync();

        return new UpdatePermissionResponse
        {
            PermissionId = permission.PermissionId,
            PermissionName = permission.PermissionName,
            Message = "Permission updated successfully"
        };
    }
}

