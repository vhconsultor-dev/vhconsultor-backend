using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Shared.Queries;

/// <summary>
/// Query repository for permissions
/// </summary>
public class PermissionDto
{
    public int PermissionId { get; set; }
    public int ResourceId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public string ResourceKey { get; set; } = string.Empty;
    public int ActionId { get; set; }
    public string ActionName { get; set; } = string.Empty;
    public string ActionKey { get; set; } = string.Empty;
    public string PermissionName { get; set; } = string.Empty;
    public string PermissionKey { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PermissionQueryRepository
{
    private readonly ModelLayer.DBcontext _context;

    public PermissionQueryRepository(ModelLayer.DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all permissions with optional filters
    /// </summary>
    public async Task<List<PermissionDto>> GetAllPermissionsAsync(
        int? resourceId = null,
        int? actionId = null,
        bool? isActive = null,
        string? searchTerm = null)
    {
        var query = _context.Permissions
            .Join(_context.Resources,
                p => p.ResourceId,
                r => r.ResourceId,
                (p, r) => new { Permission = p, Resource = r })
            .Join(_context.Actions,
                pr => pr.Permission.ActionId,
                a => a.ActionId,
                (pr, a) => new { pr.Permission, pr.Resource, Action = a })
            .AsQueryable();

        // Apply filters
        if (resourceId.HasValue)
        {
            query = query.Where(x => x.Permission.ResourceId == resourceId.Value);
        }

        if (actionId.HasValue)
        {
            query = query.Where(x => x.Permission.ActionId == actionId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.Permission.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(x =>
                x.Permission.PermissionName.ToLower().Contains(search) ||
                x.Permission.PermissionKey.ToLower().Contains(search) ||
                (x.Permission.Description != null && x.Permission.Description.ToLower().Contains(search)));
        }

        var permissions = await query
            .OrderBy(x => x.Resource.ResourceName)
            .ThenBy(x => x.Action.ActionName)
            .Select(x => new PermissionDto
            {
                PermissionId = x.Permission.PermissionId,
                ResourceId = x.Permission.ResourceId,
                ResourceName = x.Resource.ResourceName,
                ResourceKey = x.Resource.ResourceKey,
                ActionId = x.Permission.ActionId,
                ActionName = x.Action.ActionName,
                ActionKey = x.Action.ActionKey,
                PermissionName = x.Permission.PermissionName,
                PermissionKey = x.Permission.PermissionKey,
                Description = x.Permission.Description,
                IsActive = x.Permission.IsActive,
                CreatedAt = x.Permission.CreatedAt,
                UpdatedAt = x.Permission.UpdatedAt
            })
            .ToListAsync();

        return permissions;
    }

    /// <summary>
    /// Get permission by ID
    /// </summary>
    public async Task<PermissionDto?> GetPermissionByIdAsync(int permissionId)
    {
        var permission = await _context.Permissions
            .Join(_context.Resources,
                p => p.ResourceId,
                r => r.ResourceId,
                (p, r) => new { Permission = p, Resource = r })
            .Join(_context.Actions,
                pr => pr.Permission.ActionId,
                a => a.ActionId,
                (pr, a) => new { pr.Permission, pr.Resource, Action = a })
            .Where(x => x.Permission.PermissionId == permissionId)
            .Select(x => new PermissionDto
            {
                PermissionId = x.Permission.PermissionId,
                ResourceId = x.Permission.ResourceId,
                ResourceName = x.Resource.ResourceName,
                ResourceKey = x.Resource.ResourceKey,
                ActionId = x.Permission.ActionId,
                ActionName = x.Action.ActionName,
                ActionKey = x.Action.ActionKey,
                PermissionName = x.Permission.PermissionName,
                PermissionKey = x.Permission.PermissionKey,
                Description = x.Permission.Description,
                IsActive = x.Permission.IsActive,
                CreatedAt = x.Permission.CreatedAt,
                UpdatedAt = x.Permission.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return permission;
    }
}

