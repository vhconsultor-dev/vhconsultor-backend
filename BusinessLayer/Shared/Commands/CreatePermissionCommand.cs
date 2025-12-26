using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Shared.Commands;

/// <summary>
/// Command to create a new permission
/// </summary>
public class CreatePermissionRequest
{
    public int ResourceId { get; set; }
    public int ActionId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string PermissionKey { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreatePermissionResponse
{
    public int PermissionId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string PermissionKey { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class CreatePermissionCommand
{
    private readonly ModelLayer.DBcontext _context;

    public CreatePermissionCommand(ModelLayer.DBcontext context)
    {
        _context = context;
    }

    public async Task<CreatePermissionResponse> ExecuteAsync(CreatePermissionRequest request)
    {
        // Check if permission key already exists
        var existingPermission = await _context.Permissions
            .FirstOrDefaultAsync(p => p.PermissionKey == request.PermissionKey);

        if (existingPermission != null)
        {
            throw new InvalidOperationException($"Permission with key '{request.PermissionKey}' already exists");
        }

        // Verify resource exists
        var resource = await _context.Resources
            .FirstOrDefaultAsync(r => r.ResourceId == request.ResourceId);
        if (resource == null)
        {
            throw new KeyNotFoundException($"Resource with ID {request.ResourceId} not found");
        }

        // Verify action exists
        var action = await _context.Actions
            .FirstOrDefaultAsync(a => a.ActionId == request.ActionId);
        if (action == null)
        {
            throw new KeyNotFoundException($"Action with ID {request.ActionId} not found");
        }

        var permission = new ModelLayer.Shared.Entities.Permission
        {
            ResourceId = request.ResourceId,
            ActionId = request.ActionId,
            PermissionName = request.PermissionName,
            PermissionKey = request.PermissionKey,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddHours(-6) // Costa Rica time
        };

        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync();

        return new CreatePermissionResponse
        {
            PermissionId = permission.PermissionId,
            PermissionName = permission.PermissionName,
            PermissionKey = permission.PermissionKey,
            Message = "Permission created successfully"
        };
    }
}

