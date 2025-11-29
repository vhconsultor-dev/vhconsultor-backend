using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
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
        // Validar que ApplicationId esté presente
        if (resource.ApplicationId <= 0)
        {
            throw new ArgumentException("ApplicationId es requerido para crear un Resource");
        }

        _context.Resources.Add(resource);
        await _context.SaveChangesAsync();
        return resource.ResourceId;
    }

    public async Task<bool> UpdateResourceAsync(Resource resource)
    {
        var existing = await _context.Resources.FindAsync(resource.ResourceId);
        if (existing == null) return false;

        existing.ApplicationId = resource.ApplicationId;
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
        // Validar que ApplicationId esté presente
        if (role.ApplicationId <= 0)
        {
            throw new ArgumentException("ApplicationId es requerido para crear un Role");
        }

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
        return role.RoleId;
    }

    public async Task<bool> UpdateRoleAsync(Role role)
    {
        var existing = await _context.Roles.FindAsync(role.RoleId);
        if (existing == null) return false;

        existing.ApplicationId = role.ApplicationId;
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
        try
        {
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
            return userRole.UserRoleId;
        }
        catch (DbUpdateException ex)
        {
            // Capturar errores específicos de base de datos
            var innerException = ex.InnerException as SqlException;
            
            if (innerException != null)
            {
                // Error 2627: Violación de constraint UNIQUE
                // Esto puede ocurrir si existe un registro inactivo con la misma combinación UserId/RoleId
                if (innerException.Number == 2627)
                {
                    // Verificar si existe un registro inactivo que podamos reactivar
                    var existingInactive = _context.UserRoles
                        .FirstOrDefault(ur => ur.UserId == userRole.UserId && 
                                             ur.RoleId == userRole.RoleId && 
                                             !ur.IsActive);
                    
                    if (existingInactive != null)
                    {
                        // Reactivar el registro existente
                        existingInactive.IsActive = true;
                        existingInactive.AssignedBy = userRole.AssignedBy ?? existingInactive.AssignedBy;
                        existingInactive.AssignedAt = DateTimeService.GetCostaRicaNow();
                        existingInactive.ExpiresAt = userRole.ExpiresAt;
                        
                        await _context.SaveChangesAsync();
                        return existingInactive.UserRoleId;
                    }
                    
                    // Si no hay registro inactivo, entonces hay uno activo (aunque la validación debería haberlo detectado)
                    throw new InvalidOperationException(
                        $"El rol con ID {userRole.RoleId} ya está asignado al usuario con ID {userRole.UserId}");
                }
                
                // Error 547: Violación de constraint FOREIGN KEY
                if (innerException.Number == 547)
                {
                    // Determinar qué foreign key falló basándose en el mensaje
                    var errorMessage = innerException.Message.ToLower();
                    if (errorMessage.Contains("userid") || errorMessage.Contains("user"))
                    {
                        throw new ArgumentException($"El usuario con ID {userRole.UserId} no existe");
                    }
                    if (errorMessage.Contains("roleid") || errorMessage.Contains("role"))
                    {
                        throw new ArgumentException($"El rol con ID {userRole.RoleId} no existe");
                    }
                    if (errorMessage.Contains("assignedby"))
                    {
                        throw new ArgumentException($"El usuario asignador con ID {userRole.AssignedBy} no existe");
                    }
                    
                    throw new ArgumentException("Error de integridad referencial. Verifique que el usuario y el rol existan");
                }
                
                // Otros errores de SQL Server
                throw new InvalidOperationException(
                    $"Error de base de datos al asignar rol: {innerException.Message}", ex);
            }
            
            // Si no es SqlException, re-lanzar la excepción original
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error inesperado al asignar rol al usuario: {ex.Message}", ex);
        }
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

    public async Task<int> ReactivateUserRoleAsync(int userRoleId, int? assignedBy = null, DateTime? expiresAt = null)
    {
        var userRole = await _context.UserRoles.FindAsync(userRoleId);
        if (userRole == null)
        {
            throw new ArgumentException($"No se encontró el registro de asignación de rol con ID {userRoleId}");
        }

        userRole.IsActive = true;
        userRole.AssignedBy = assignedBy ?? userRole.AssignedBy;
        userRole.AssignedAt = DateTimeService.GetCostaRicaNow();
        userRole.ExpiresAt = expiresAt;
        
        await _context.SaveChangesAsync();
        return userRole.UserRoleId;
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

    #region Helper Methods

    /// <summary>
    /// Obtiene el ApplicationId desde ApplicationKey
    /// </summary>
    public async Task<int?> GetApplicationIdByKeyAsync(string applicationKey)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(a => a.ApplicationKey == applicationKey && a.IsActive);
        
        return application?.ApplicationId;
    }

    #endregion
}

