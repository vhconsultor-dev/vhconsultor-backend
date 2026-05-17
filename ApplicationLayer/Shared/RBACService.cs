using BusinessLayer.Shared.Commands;
using BusinessLayer.Shared.Queries;
using ModelLayer.Shared;
using ModelLayer.Shared.Entities;

namespace ApplicationLayer.Shared;

/// <summary>
/// Servicio de aplicación para gestión de RBAC (Roles y Permisos)
/// </summary>
public class RBACService
{
    private readonly RBACCommandRepository _commandRepository;
    private readonly RBACQueryRepository _queryRepository;

    public RBACService(
        RBACCommandRepository commandRepository,
        RBACQueryRepository queryRepository)
    {
        _commandRepository = commandRepository;
        _queryRepository = queryRepository;
    }

    #region Resources

    public async Task<IEnumerable<Resource>> GetResourcesAsync(
        int? resourceId = null,
        string? resourceName = null,
        string? resourceKey = null,
        string? module = null,
        bool? isActive = true,
        string? applicationKey = null)
    {
        return await _queryRepository.GetResourcesAsync(resourceId, resourceName, resourceKey, module, isActive, applicationKey);
    }

    public async Task<int> CreateResourceAsync(Resource resource)
    {
        resource.CreatedAt = DateTimeService.GetCostaRicaNow();
        resource.IsActive = true;
        return await _commandRepository.CreateResourceAsync(resource);
    }

    public async Task<bool> UpdateResourceAsync(Resource resource)
    {
        return await _commandRepository.UpdateResourceAsync(resource);
    }

    public async Task<bool> DeleteResourceAsync(int resourceId)
    {
        return await _commandRepository.DeleteResourceAsync(resourceId);
    }

    #endregion

    #region Actions

    public async Task<IEnumerable<ModelLayer.Shared.Entities.Action>> GetActionsAsync(
        int? actionId = null,
        string? actionName = null,
        string? actionKey = null,
        bool? isActive = true)
    {
        return await _queryRepository.GetActionsAsync(actionId, actionName, actionKey, isActive);
    }

    public async Task<int> CreateActionAsync(ModelLayer.Shared.Entities.Action action)
    {
        action.CreatedAt = DateTimeService.GetCostaRicaNow();
        action.IsActive = true;
        return await _commandRepository.CreateActionAsync(action);
    }

    #endregion

    #region Permissions

    public async Task<IEnumerable<Permission>> GetPermissionsAsync(
        int? permissionId = null,
        int? resourceId = null,
        int? actionId = null,
        string? permissionKey = null,
        bool? isActive = true,
        int? applicationId = null,
        string? applicationKey = null)
    {
        return await _queryRepository.GetPermissionsAsync(
            permissionId, resourceId, actionId, permissionKey, isActive, applicationId, applicationKey);
    }

    public async Task<int> CreatePermissionAsync(Permission permission, string? applicationKey = null)
    {
        await PreparePermissionAsync(permission, applicationKey, excludePermissionId: null);
        permission.CreatedAt = DateTimeService.GetCostaRicaNow();
        permission.IsActive = true;
        return await _commandRepository.CreatePermissionAsync(permission);
    }

    public async Task<bool> UpdatePermissionAsync(Permission permission, string? applicationKey = null)
    {
        await PreparePermissionAsync(permission, applicationKey, excludePermissionId: permission.PermissionId);
        return await _commandRepository.UpdatePermissionAsync(permission);
    }

    public async Task<IEnumerable<Application>> GetApplicationsAsync(bool? isActive = true)
    {
        return await _queryRepository.GetApplicationsAsync(isActive);
    }

    public async Task<bool> DeletePermissionAsync(int permissionId)
    {
        return await _commandRepository.DeletePermissionAsync(permissionId);
    }

    #endregion

    #region Roles

    public async Task<IEnumerable<Role>> GetRolesAsync(
        int? roleId = null,
        string? roleName = null,
        string? roleKey = null,
        bool? isSystemRole = null,
        bool? isActive = true,
        string? applicationKey = null)
    {
        return await _queryRepository.GetRolesAsync(roleId, roleName, roleKey, isSystemRole, isActive, applicationKey);
    }

    public async Task<int> CreateRoleAsync(Role role)
    {
        role.CreatedAt = DateTimeService.GetCostaRicaNow();
        role.IsActive = true;
        return await _commandRepository.CreateRoleAsync(role);
    }

    public async Task<bool> UpdateRoleAsync(Role role)
    {
        return await _commandRepository.UpdateRoleAsync(role);
    }

    public async Task<bool> DeleteRoleAsync(int roleId)
    {
        return await _commandRepository.DeleteRoleAsync(roleId);
    }

    #endregion

    #region RolePermissions

    public async Task<IEnumerable<RolePermission>> GetRolePermissionsAsync(
        int? roleId = null,
        int? permissionId = null,
        int? applicationId = null,
        string? applicationKey = null)
    {
        return await _queryRepository.GetRolePermissionsAsync(roleId, permissionId, applicationId, applicationKey);
    }

    public async Task<int> AssignPermissionToRoleAsync(int roleId, int permissionId, int? grantedBy = null)
    {
        var role = await _queryRepository.GetRoleByIdAsync(roleId)
            ?? throw new InvalidOperationException($"Role with ID {roleId} not found.");

        var permission = await _queryRepository.GetPermissionByIdAsync(permissionId)
            ?? throw new InvalidOperationException($"Permission with ID {permissionId} not found.");

        if (role.ApplicationId != permission.ApplicationId)
        {
            throw new InvalidOperationException(
                $"Cannot assign permission to role: they belong to different applications " +
                $"(role ApplicationId={role.ApplicationId}, permission ApplicationId={permission.ApplicationId}).");
        }

        var rolePermission = new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId,
            GrantedBy = grantedBy,
            GrantedAt = DateTimeService.GetCostaRicaNow()
        };
        return await _commandRepository.AssignPermissionToRoleAsync(rolePermission);
    }

    public async Task<bool> RemovePermissionFromRoleAsync(int roleId, int permissionId)
    {
        return await _commandRepository.RemovePermissionFromRoleAsync(roleId, permissionId);
    }

    #endregion

    #region UserRoles

    public async Task<IEnumerable<UserRole>> GetUserRolesAsync(
        int? userId = null,
        int? roleId = null,
        bool? isActive = true,
        int? applicationId = null)
    {
        return await _queryRepository.GetUserRolesAsync(userId, roleId, isActive, applicationId);
    }

    public async Task<int> AssignRoleToUserAsync(int userId, int roleId, int applicationId = 0, int? assignedBy = null, DateTime? expiresAt = null)
    {
        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            ApplicationId = applicationId,
            AssignedBy = assignedBy,
            AssignedAt = DateTimeService.GetCostaRicaNow(),
            ExpiresAt = expiresAt,
            IsActive = true
        };
        return await _commandRepository.AssignRoleToUserAsync(userRole);
    }

    public async Task<bool> RemoveRoleFromUserAsync(int userId, int roleId)
    {
        return await _commandRepository.RemoveRoleFromUserAsync(userId, roleId);
    }

    #endregion

    #region UserPermissions

    public async Task<IEnumerable<UserPermission>> GetUserPermissionsAsync(
        int? userId = null,
        int? permissionId = null,
        bool? isActive = true)
    {
        return await _queryRepository.GetUserPermissionsAsync(userId, permissionId, isActive);
    }

    public async Task<int> GrantPermissionToUserAsync(int userId, int permissionId, int? grantedBy = null, DateTime? expiresAt = null)
    {
        var userPermission = new UserPermission
        {
            UserId = userId,
            PermissionId = permissionId,
            GrantedBy = grantedBy,
            GrantedAt = DateTimeService.GetCostaRicaNow(),
            ExpiresAt = expiresAt,
            IsActive = true
        };
        return await _commandRepository.GrantPermissionToUserAsync(userPermission);
    }

    public async Task<bool> RevokePermissionFromUserAsync(int userId, int permissionId)
    {
        return await _commandRepository.RevokePermissionFromUserAsync(userId, permissionId);
    }

    #endregion

    #region UserPermissionDenials

    public async Task<IEnumerable<UserPermissionDenial>> GetUserPermissionDenialsAsync(
        int? userId = null,
        int? permissionId = null,
        bool? isActive = true)
    {
        return await _queryRepository.GetUserPermissionDenialsAsync(userId, permissionId, isActive);
    }

    public async Task<int> DenyPermissionToUserAsync(int userId, int permissionId, int? deniedBy = null, string? reason = null)
    {
        var denial = new UserPermissionDenial
        {
            UserId = userId,
            PermissionId = permissionId,
            DeniedBy = deniedBy,
            DeniedAt = DateTimeService.GetCostaRicaNow(),
            Reason = reason,
            IsActive = true
        };
        return await _commandRepository.DenyPermissionToUserAsync(denial);
    }

    public async Task<bool> RemoveDenialFromUserAsync(int userId, int permissionId)
    {
        return await _commandRepository.RemoveDenialFromUserAsync(userId, permissionId);
    }

    #endregion

    #region Helper Methods

    public async Task<IEnumerable<Permission>> GetEffectiveUserPermissionsAsync(int userId, string? applicationKey = null)
    {
        return await _queryRepository.GetEffectiveUserPermissionsAsync(userId, applicationKey);
    }

    /// <summary>
    /// Resuelve applicationKey → applicationId contra [Global].[Applications].
    /// Lanza InvalidOperationException si no se encuentra.
    /// </summary>
    public async Task<int> ResolveApplicationIdAsync(string applicationKey)
    {
        var applicationId = await _queryRepository.ResolveApplicationIdAsync(applicationKey);
        if (applicationId == 0)
            throw new InvalidOperationException($"Application with key '{applicationKey}' not found or is inactive.");
        return applicationId;
    }

    /// <summary>
    /// Valida recurso/acción, resuelve ApplicationId y completa PermissionKey/PermissionName si faltan.
    /// </summary>
    private async Task PreparePermissionAsync(Permission permission, string? applicationKey, int? excludePermissionId)
    {
        if (permission.ResourceId <= 0)
            throw new InvalidOperationException("ResourceId is required.");

        if (permission.ActionId <= 0)
            throw new InvalidOperationException("ActionId is required.");

        var resource = await _queryRepository.GetResourceByIdAsync(permission.ResourceId)
            ?? throw new InvalidOperationException($"Resource with ID {permission.ResourceId} not found.");

        var action = await _queryRepository.GetActionByIdAsync(permission.ActionId)
            ?? throw new InvalidOperationException($"Action with ID {permission.ActionId} not found.");

        if (permission.ApplicationId <= 0 && !string.IsNullOrWhiteSpace(applicationKey))
            permission.ApplicationId = await ResolveApplicationIdAsync(applicationKey);

        if (permission.ApplicationId <= 0)
            permission.ApplicationId = resource.ApplicationId;

        if (permission.ApplicationId != resource.ApplicationId)
        {
            throw new InvalidOperationException(
                $"Permission ApplicationId ({permission.ApplicationId}) must match the resource application " +
                $"({resource.ApplicationId}, resource '{resource.ResourceKey}').");
        }

        if (string.IsNullOrWhiteSpace(permission.PermissionKey))
            permission.PermissionKey = $"{resource.ResourceKey}.{action.ActionKey}";

        if (string.IsNullOrWhiteSpace(permission.PermissionName))
            permission.PermissionName = $"{resource.ResourceName} · {action.ActionName}";

        permission.PermissionKey = permission.PermissionKey.Trim();

        if (await _queryRepository.PermissionKeyExistsAsync(permission.PermissionKey, excludePermissionId))
        {
            throw new InvalidOperationException(
                $"PermissionKey '{permission.PermissionKey}' already exists.");
        }
    }

    #endregion
}

