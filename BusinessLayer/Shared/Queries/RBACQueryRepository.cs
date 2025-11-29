using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Shared;
using ModelLayer.Shared.Entities;

namespace BusinessLayer.Shared.Queries;

/// <summary>
/// Repository para consultas RBAC usando Dapper
/// </summary>
public class RBACQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public RBACQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    #region Resources

    public async Task<IEnumerable<Resource>> GetResourcesAsync(
        int? resourceId = null,
        string? resourceName = null,
        string? resourceKey = null,
        string? module = null,
        bool? isActive = true,
        int? applicationId = null,
        string? applicationKey = null)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT r.ResourceId, r.ApplicationId, r.ResourceName, r.ResourceKey, r.Description, r.Module, r.IsActive, r.CreatedAt, r.UpdatedAt
            FROM [Global].[Resources] r";

        var parameters = new DynamicParameters();

        // Si se proporciona applicationKey, hacer JOIN con Applications
        if (!string.IsNullOrEmpty(applicationKey))
        {
            sql += @"
            INNER JOIN [Global].[Applications] a ON r.ApplicationId = a.ApplicationId";
        }

        sql += " WHERE 1=1";

        if (resourceId.HasValue)
        {
            sql += " AND r.ResourceId = @ResourceId";
            parameters.Add("ResourceId", resourceId.Value);
        }

        if (!string.IsNullOrEmpty(resourceName))
        {
            sql += " AND r.ResourceName LIKE '%' + @ResourceName + '%'";
            parameters.Add("ResourceName", resourceName);
        }

        if (!string.IsNullOrEmpty(resourceKey))
        {
            sql += " AND r.ResourceKey = @ResourceKey";
            parameters.Add("ResourceKey", resourceKey);
        }

        if (!string.IsNullOrEmpty(module))
        {
            sql += " AND r.Module = @Module";
            parameters.Add("Module", module);
        }

        if (isActive.HasValue)
        {
            sql += " AND r.IsActive = @IsActive";
            parameters.Add("IsActive", isActive.Value);
        }

        // Filtros por aplicación (REQUERIDO según documentación)
        if (applicationId.HasValue)
        {
            sql += " AND r.ApplicationId = @ApplicationId";
            parameters.Add("ApplicationId", applicationId.Value);
        }
        else if (!string.IsNullOrEmpty(applicationKey))
        {
            sql += " AND a.ApplicationKey = @ApplicationKey";
            parameters.Add("ApplicationKey", applicationKey);
        }

        sql += " ORDER BY r.ResourceName";

        return await connection.QueryAsync<Resource>(sql, parameters);
    }

    #endregion

    #region Actions

    public async Task<IEnumerable<ModelLayer.Shared.Entities.Action>> GetActionsAsync(
        int? actionId = null,
        string? actionName = null,
        string? actionKey = null,
        bool? isActive = true)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT ActionId, ActionName, ActionKey, Description, IsActive, CreatedAt
            FROM [Global].[Actions]
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (actionId.HasValue)
        {
            sql += " AND ActionId = @ActionId";
            parameters.Add("ActionId", actionId.Value);
        }

        if (!string.IsNullOrEmpty(actionName))
        {
            sql += " AND ActionName LIKE '%' + @ActionName + '%'";
            parameters.Add("ActionName", actionName);
        }

        if (!string.IsNullOrEmpty(actionKey))
        {
            sql += " AND ActionKey = @ActionKey";
            parameters.Add("ActionKey", actionKey);
        }

        if (isActive.HasValue)
        {
            sql += " AND IsActive = @IsActive";
            parameters.Add("IsActive", isActive.Value);
        }

        sql += " ORDER BY ActionName";

        return await connection.QueryAsync<ModelLayer.Shared.Entities.Action>(sql, parameters);
    }

    #endregion

    #region Permissions

    public async Task<IEnumerable<Permission>> GetPermissionsAsync(
        int? permissionId = null,
        int? resourceId = null,
        int? actionId = null,
        string? permissionKey = null,
        bool? isActive = true)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT PermissionId, ResourceId, ActionId, PermissionName, PermissionKey, Description, IsActive, CreatedAt, UpdatedAt
            FROM [Global].[Permissions]
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (permissionId.HasValue)
        {
            sql += " AND PermissionId = @PermissionId";
            parameters.Add("PermissionId", permissionId.Value);
        }

        if (resourceId.HasValue)
        {
            sql += " AND ResourceId = @ResourceId";
            parameters.Add("ResourceId", resourceId.Value);
        }

        if (actionId.HasValue)
        {
            sql += " AND ActionId = @ActionId";
            parameters.Add("ActionId", actionId.Value);
        }

        if (!string.IsNullOrEmpty(permissionKey))
        {
            sql += " AND PermissionKey = @PermissionKey";
            parameters.Add("PermissionKey", permissionKey);
        }

        if (isActive.HasValue)
        {
            sql += " AND IsActive = @IsActive";
            parameters.Add("IsActive", isActive.Value);
        }

        sql += " ORDER BY PermissionName";

        return await connection.QueryAsync<Permission>(sql, parameters);
    }

    #endregion

    #region Roles

    public async Task<IEnumerable<Role>> GetRolesAsync(
        int? roleId = null,
        string? roleName = null,
        string? roleKey = null,
        bool? isSystemRole = null,
        bool? isActive = true,
        int? applicationId = null,
        string? applicationKey = null)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT ro.RoleId, ro.ApplicationId, ro.RoleName, ro.RoleKey, ro.Description, ro.IsSystemRole, ro.IsActive, ro.CreatedAt, ro.UpdatedAt
            FROM [Global].[Roles] ro";

        var parameters = new DynamicParameters();

        // Si se proporciona applicationKey, hacer JOIN con Applications
        if (!string.IsNullOrEmpty(applicationKey))
        {
            sql += @"
            INNER JOIN [Global].[Applications] a ON ro.ApplicationId = a.ApplicationId";
        }

        sql += " WHERE 1=1";

        if (roleId.HasValue)
        {
            sql += " AND ro.RoleId = @RoleId";
            parameters.Add("RoleId", roleId.Value);
        }

        if (!string.IsNullOrEmpty(roleName))
        {
            sql += " AND ro.RoleName LIKE '%' + @RoleName + '%'";
            parameters.Add("RoleName", roleName);
        }

        if (!string.IsNullOrEmpty(roleKey))
        {
            sql += " AND ro.RoleKey = @RoleKey";
            parameters.Add("RoleKey", roleKey);
        }

        if (isSystemRole.HasValue)
        {
            sql += " AND ro.IsSystemRole = @IsSystemRole";
            parameters.Add("IsSystemRole", isSystemRole.Value);
        }

        if (isActive.HasValue)
        {
            sql += " AND ro.IsActive = @IsActive";
            parameters.Add("IsActive", isActive.Value);
        }

        // Filtros por aplicación (REQUERIDO según documentación)
        if (applicationId.HasValue)
        {
            sql += " AND ro.ApplicationId = @ApplicationId";
            parameters.Add("ApplicationId", applicationId.Value);
        }
        else if (!string.IsNullOrEmpty(applicationKey))
        {
            sql += " AND a.ApplicationKey = @ApplicationKey";
            parameters.Add("ApplicationKey", applicationKey);
        }

        sql += " ORDER BY ro.RoleName";

        return await connection.QueryAsync<Role>(sql, parameters);
    }

    #endregion

    #region RolePermissions

    public async Task<IEnumerable<RolePermission>> GetRolePermissionsAsync(
        int? roleId = null,
        int? permissionId = null)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT RolePermissionId, RoleId, PermissionId, GrantedBy, GrantedAt
            FROM [Global].[RolePermissions]
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (roleId.HasValue)
        {
            sql += " AND RoleId = @RoleId";
            parameters.Add("RoleId", roleId.Value);
        }

        if (permissionId.HasValue)
        {
            sql += " AND PermissionId = @PermissionId";
            parameters.Add("PermissionId", permissionId.Value);
        }

        sql += " ORDER BY GrantedAt DESC";

        return await connection.QueryAsync<RolePermission>(sql, parameters);
    }

    #endregion

    #region UserRoles

    public async Task<IEnumerable<UserRole>> GetUserRolesAsync(
        int? userId = null,
        int? roleId = null,
        bool? isActive = true,
        int? applicationId = null)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT UserRoleId, UserId, RoleId, ApplicationId, AssignedBy, AssignedAt, ExpiresAt, IsActive
            FROM [Global].[UserRoles]
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (userId.HasValue)
        {
            sql += " AND UserId = @UserId";
            parameters.Add("UserId", userId.Value);
        }

        if (roleId.HasValue)
        {
            sql += " AND RoleId = @RoleId";
            parameters.Add("RoleId", roleId.Value);
        }

        if (isActive.HasValue)
        {
            sql += " AND IsActive = @IsActive";
            parameters.Add("IsActive", isActive.Value);
        }

        if (applicationId.HasValue)
        {
            sql += " AND ApplicationId = @ApplicationId";
            parameters.Add("ApplicationId", applicationId.Value);
        }

        sql += " ORDER BY AssignedAt DESC";

        return await connection.QueryAsync<UserRole>(sql, parameters);
    }

    #endregion

    #region UserPermissions

    public async Task<IEnumerable<UserPermission>> GetUserPermissionsAsync(
        int? userId = null,
        int? permissionId = null,
        bool? isActive = true)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT UserPermissionId, UserId, PermissionId, GrantedBy, GrantedAt, ExpiresAt, IsActive
            FROM [Global].[UserPermissions]
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (userId.HasValue)
        {
            sql += " AND UserId = @UserId";
            parameters.Add("UserId", userId.Value);
        }

        if (permissionId.HasValue)
        {
            sql += " AND PermissionId = @PermissionId";
            parameters.Add("PermissionId", permissionId.Value);
        }

        if (isActive.HasValue)
        {
            sql += " AND IsActive = @IsActive";
            parameters.Add("IsActive", isActive.Value);
        }

        sql += " ORDER BY GrantedAt DESC";

        return await connection.QueryAsync<UserPermission>(sql, parameters);
    }

    #endregion

    #region UserPermissionDenials

    public async Task<IEnumerable<UserPermissionDenial>> GetUserPermissionDenialsAsync(
        int? userId = null,
        int? permissionId = null,
        bool? isActive = true)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT UserPermissionDenialId, UserId, PermissionId, DeniedBy, DeniedAt, Reason, IsActive
            FROM [Global].[UserPermissionDenials]
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (userId.HasValue)
        {
            sql += " AND UserId = @UserId";
            parameters.Add("UserId", userId.Value);
        }

        if (permissionId.HasValue)
        {
            sql += " AND PermissionId = @PermissionId";
            parameters.Add("PermissionId", permissionId.Value);
        }

        if (isActive.HasValue)
        {
            sql += " AND IsActive = @IsActive";
            parameters.Add("IsActive", isActive.Value);
        }

        sql += " ORDER BY DeniedAt DESC";

        return await connection.QueryAsync<UserPermissionDenial>(sql, parameters);
    }

    #endregion

    #region UserApplications

    /// <summary>
    /// Obtiene las aplicaciones disponibles para un usuario
    /// </summary>
    public async Task<IEnumerable<Application>> GetUserApplicationsAsync(int userId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                a.ApplicationId,
                a.ApplicationKey,
                a.ApplicationName,
                a.Description,
                a.IsActive,
                a.CreatedAt,
                a.UpdatedAt
            FROM [Global].[Applications] a
            INNER JOIN [Global].[UserApplications] ua ON a.ApplicationId = ua.ApplicationId
            WHERE ua.UserId = @UserId 
              AND ua.IsActive = 1
              AND a.IsActive = 1
            ORDER BY a.ApplicationName";

        return await connection.QueryAsync<Application>(sql, new { UserId = userId });
    }

    /// <summary>
    /// Valida si un usuario tiene acceso a una aplicación
    /// </summary>
    public async Task<bool> ValidateUserApplicationAccessAsync(int userId, int applicationId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT CASE WHEN COUNT(*) > 0 THEN 1 ELSE 0 END AS HasAccess
            FROM [Global].[UserApplications]
            WHERE UserId = @UserId 
              AND ApplicationId = @ApplicationId 
              AND IsActive = 1";

        var hasAccess = await connection.QueryFirstOrDefaultAsync<int>(sql, new { UserId = userId, ApplicationId = applicationId });
        return hasAccess > 0;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Obtiene todos los permisos efectivos de un usuario (roles + permisos directos - denegaciones)
    /// Filtrado por aplicación si se proporciona applicationKey
    /// </summary>
    public async Task<IEnumerable<Permission>> GetEffectiveUserPermissionsAsync(int userId, string? applicationKey = null)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT DISTINCT 
                p.PermissionId, p.ResourceId, p.ActionId, p.PermissionName, p.PermissionKey, p.Description, p.IsActive, p.CreatedAt, p.UpdatedAt
            FROM [Global].[Permissions] p
            INNER JOIN [Global].[Resources] r ON p.ResourceId = r.ResourceId";

        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId);

        // Si se proporciona applicationKey, hacer JOIN con Applications y filtrar
        if (!string.IsNullOrEmpty(applicationKey))
        {
            sql += @"
            INNER JOIN [Global].[Applications] app ON r.ApplicationId = app.ApplicationId";
        }

        sql += @"
            WHERE p.IsActive = 1
            AND (
                -- Permisos desde roles
                p.PermissionId IN (
                    SELECT rp.PermissionId
                    FROM [Global].[UserRoles] ur
                    INNER JOIN [Global].[RolePermissions] rp ON ur.RoleId = rp.RoleId
                    WHERE ur.UserId = @UserId 
                    AND ur.IsActive = 1
                    AND (ur.ExpiresAt IS NULL OR ur.ExpiresAt > GETDATE())
                )
                -- Permisos directos
                OR p.PermissionId IN (
                    SELECT up.PermissionId
                    FROM [Global].[UserPermissions] up
                    WHERE up.UserId = @UserId 
                    AND up.IsActive = 1
                    AND (up.ExpiresAt IS NULL OR up.ExpiresAt > GETDATE())
                )
            )
            -- Excluir permisos denegados
            AND p.PermissionId NOT IN (
                SELECT upd.PermissionId
                FROM [Global].[UserPermissionDenials] upd
                WHERE upd.UserId = @UserId 
                AND upd.IsActive = 1
            )";

        // Filtrar por aplicación si se proporciona
        if (!string.IsNullOrEmpty(applicationKey))
        {
            sql += " AND app.ApplicationKey = @ApplicationKey";
            parameters.Add("ApplicationKey", applicationKey);
        }

        sql += " ORDER BY p.PermissionName";

        return await connection.QueryAsync<Permission>(sql, parameters);
    }

    #endregion
}

