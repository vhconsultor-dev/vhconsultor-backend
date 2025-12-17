using ModelLayer;
using ModelLayer.Shared;
using ModelLayer.Shared.Entities;

namespace BusinessLayer.Shared.Commands;

/// <summary>
/// Command Repository para operaciones RBAC usando Entity Framework
/// </summary>
public class RBACCommandRepository
{
    private readonly DBcontext _context;

    public RBACCommandRepository(DBcontext context)
    {
        _context = context;
    }

    #region Resources

    public async Task<int> CreateResourceAsync(Resource resource)
    {
        _context.Resources.Add(resource);
        await _context.SaveChangesAsync();
        return resource.ResourceId;
    }

    public async Task<bool> UpdateResourceAsync(Resource resource)
    {
        var existing = await _context.Resources.FindAsync(resource.ResourceId);
        if (existing == null) return false;

        existing.ResourceName = resource.ResourceName;
        existing.ResourceKey = resource.ResourceKey;
        existing.Description = resource.Description;
        existing.Module = resource.Module;
        existing.IsActive = resource.IsActive;
        existing.UpdatedAt = DateTimeService.GetCostaRicaNow();

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteResourceAsync(int resourceId)
    {
        var resource = await _context.Resources.FindAsync(resourceId);
        if (resource == null) return false;

        resource.IsActive = false;
        resource.UpdatedAt = DateTimeService.GetCostaRicaNow();
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Actions

    public async Task<int> CreateActionAsync(ModelLayer.Shared.Entities.Action action)
    {
        _context.Actions.Add(action);
        await _context.SaveChangesAsync();
        return action.ActionId;
    }

    #endregion

    #region Permissions

    public async Task<int> CreatePermissionAsync(Permission permission)
    {
        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync();
        return permission.PermissionId;
    }

    public async Task<bool> UpdatePermissionAsync(Permission permission)
    {
        var existing = await _context.Permissions.FindAsync(permission.PermissionId);
        if (existing == null) return false;

        existing.ResourceId = permission.ResourceId;
        existing.ActionId = permission.ActionId;
        existing.PermissionName = permission.PermissionName;
        existing.PermissionKey = permission.PermissionKey;
        existing.Description = permission.Description;
        existing.IsActive = permission.IsActive;
        existing.UpdatedAt = DateTimeService.GetCostaRicaNow();

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeletePermissionAsync(int permissionId)
    {
        var permission = await _context.Permissions.FindAsync(permissionId);
        if (permission == null) return false;

        permission.IsActive = false;
        permission.UpdatedAt = DateTimeService.GetCostaRicaNow();
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Roles

    public async Task<int> CreateRoleAsync(Role role)
    {
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
        return role.RoleId;
    }

    public async Task<bool> UpdateRoleAsync(Role role)
    {
        var existing = await _context.Roles.FindAsync(role.RoleId);
        if (existing == null) return false;

        existing.RoleName = role.RoleName;
        existing.RoleKey = role.RoleKey;
        existing.Description = role.Description;
        existing.IsActive = role.IsActive;
        existing.UpdatedAt = DateTimeService.GetCostaRicaNow();

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteRoleAsync(int roleId)
    {
        var role = await _context.Roles.FindAsync(roleId);
        if (role == null) return false;

        role.IsActive = false;
        role.UpdatedAt = DateTimeService.GetCostaRicaNow();
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region RolePermissions

    public async Task<int> AssignPermissionToRoleAsync(RolePermission rolePermission)
    {
        _context.RolePermissions.Add(rolePermission);
        await _context.SaveChangesAsync();
        return rolePermission.RolePermissionId;
    }

    public async Task<bool> RemovePermissionFromRoleAsync(int roleId, int permissionId)
    {
        var rolePermission = _context.RolePermissions
            .FirstOrDefault(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
        
        if (rolePermission == null) return false;

        _context.RolePermissions.Remove(rolePermission);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region UserRoles

    public async Task<int> AssignRoleToUserAsync(UserRole userRole)
    {
        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync();
        return userRole.UserRoleId;
    }

    public async Task<bool> RemoveRoleFromUserAsync(int userId, int roleId)
    {
        var userRole = _context.UserRoles
            .FirstOrDefault(ur => ur.UserId == userId && ur.RoleId == roleId && ur.IsActive);
        
        if (userRole == null) return false;

        userRole.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region UserPermissions

    public async Task<int> GrantPermissionToUserAsync(UserPermission userPermission)
    {
        _context.UserPermissions.Add(userPermission);
        await _context.SaveChangesAsync();
        return userPermission.UserPermissionId;
    }

    public async Task<bool> RevokePermissionFromUserAsync(int userId, int permissionId)
    {
        var userPermission = _context.UserPermissions
            .FirstOrDefault(up => up.UserId == userId && up.PermissionId == permissionId && up.IsActive);
        
        if (userPermission == null) return false;

        userPermission.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region UserPermissionDenials

    public async Task<int> DenyPermissionToUserAsync(UserPermissionDenial denial)
    {
        _context.UserPermissionDenials.Add(denial);
        await _context.SaveChangesAsync();
        return denial.UserPermissionDenialId;
    }

    public async Task<bool> RemoveDenialFromUserAsync(int userId, int permissionId)
    {
        var denial = _context.UserPermissionDenials
            .FirstOrDefault(upd => upd.UserId == userId && upd.PermissionId == permissionId && upd.IsActive);
        
        if (denial == null) return false;

        denial.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion
}

