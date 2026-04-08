using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ApplicationLayer.Shared;
using ApiLayer.Tools;
using ModelLayer.Shared.Entities;

namespace ApiLayer.Controllers.Shared;

/// <summary>
/// Controlador para gestión de RBAC (Roles, Permisos, Recursos, Acciones)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RBACController : ControllerBase
{
    private readonly RBACService _rbacService;

    public RBACController(RBACService rbacService)
    {
        _rbacService = rbacService;
    }

    #region GET - Resources

    /// <summary>
    /// Obtiene recursos con filtros opcionales
    /// </summary>
    [HttpGet("resources")]
    public async Task<IActionResult> GetResources(
        [FromQuery] int? resourceId = null,
        [FromQuery] string? resourceName = null,
        [FromQuery] string? resourceKey = null,
        [FromQuery] string? module = null,
        [FromQuery] bool? isActive = true)
    {
        try
        {
            var resources = await _rbacService.GetResourcesAsync(resourceId, resourceName, resourceKey, module, isActive);
            var resourcesList = resources.ToList();

            if (!resourcesList.Any())
            {
                var notFoundResponse = ResponseStructure<object>.NotFound("No se encontraron recursos");
                return NotFound(notFoundResponse);
            }

            if (resourceId.HasValue && resourcesList.Count == 1)
            {
                var response = ResponseStructure<object>.Success(resourcesList.First(), "Recurso obtenido exitosamente");
                return Ok(response);
            }

            var listResponse = ResponseStructure<object>.Success(resourcesList, $"{resourcesList.Count} recurso(s) encontrado(s)");
            return Ok(listResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al obtener recursos: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Obtiene acciones con filtros opcionales
    /// </summary>
    [HttpGet("actions")]
    public async Task<IActionResult> GetActions(
        [FromQuery] int? actionId = null,
        [FromQuery] string? actionName = null,
        [FromQuery] string? actionKey = null,
        [FromQuery] bool? isActive = true)
    {
        try
        {
            var actions = await _rbacService.GetActionsAsync(actionId, actionName, actionKey, isActive);
            var actionsList = actions.ToList();

            if (!actionsList.Any())
            {
                var notFoundResponse = ResponseStructure<object>.NotFound("No se encontraron acciones");
                return NotFound(notFoundResponse);
            }

            if (actionId.HasValue && actionsList.Count == 1)
            {
                var response = ResponseStructure<object>.Success(actionsList.First(), "Acción obtenida exitosamente");
                return Ok(response);
            }

            var listResponse = ResponseStructure<object>.Success(actionsList, $"{actionsList.Count} acción(es) encontrada(s)");
            return Ok(listResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al obtener acciones: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Obtiene permisos con filtros opcionales
    /// </summary>
    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions(
        [FromQuery] int? permissionId = null,
        [FromQuery] int? resourceId = null,
        [FromQuery] int? actionId = null,
        [FromQuery] string? permissionKey = null,
        [FromQuery] bool? isActive = true)
    {
        try
        {
            var permissions = await _rbacService.GetPermissionsAsync(permissionId, resourceId, actionId, permissionKey, isActive);
            var permissionsList = permissions.ToList();

            if (!permissionsList.Any())
            {
                var notFoundResponse = ResponseStructure<object>.NotFound("No se encontraron permisos");
                return NotFound(notFoundResponse);
            }

            if (permissionId.HasValue && permissionsList.Count == 1)
            {
                var response = ResponseStructure<object>.Success(permissionsList.First(), "Permiso obtenido exitosamente");
                return Ok(response);
            }

            var listResponse = ResponseStructure<object>.Success(permissionsList, $"{permissionsList.Count} permiso(s) encontrado(s)");
            return Ok(listResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al obtener permisos: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Obtiene roles con filtros opcionales
    /// </summary>
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles(
        [FromQuery] int? roleId = null,
        [FromQuery] string? roleName = null,
        [FromQuery] string? roleKey = null,
        [FromQuery] bool? isSystemRole = null,
        [FromQuery] bool? isActive = true)
    {
        try
        {
            var roles = await _rbacService.GetRolesAsync(roleId, roleName, roleKey, isSystemRole, isActive);
            var rolesList = roles.ToList();

            if (!rolesList.Any())
            {
                var notFoundResponse = ResponseStructure<object>.NotFound("No se encontraron roles");
                return NotFound(notFoundResponse);
            }

            if (roleId.HasValue && rolesList.Count == 1)
            {
                var response = ResponseStructure<object>.Success(rolesList.First(), "Rol obtenido exitosamente");
                return Ok(response);
            }

            var listResponse = ResponseStructure<object>.Success(rolesList, $"{rolesList.Count} rol(es) encontrado(s)");
            return Ok(listResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al obtener roles: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Obtiene permisos de un rol
    /// </summary>
    [HttpGet("role-permissions")]
    public async Task<IActionResult> GetRolePermissions(
        [FromQuery] int? roleId = null,
        [FromQuery] int? permissionId = null)
    {
        try
        {
            var rolePermissions = await _rbacService.GetRolePermissionsAsync(roleId, permissionId);
            var rolePermissionsList = rolePermissions.ToList();

            if (!rolePermissionsList.Any())
            {
                var notFoundResponse = ResponseStructure<object>.NotFound("No se encontraron permisos asignados");
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(rolePermissionsList, $"{rolePermissionsList.Count} permiso(s) asignado(s) encontrado(s)");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al obtener permisos del rol: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Obtiene roles de un usuario
    /// </summary>
    [HttpGet("user-roles")]
    public async Task<IActionResult> GetUserRoles(
        [FromQuery] int? userId = null,
        [FromQuery] int? roleId = null,
        [FromQuery] bool? isActive = true)
    {
        try
        {
            var userRoles = await _rbacService.GetUserRolesAsync(userId, roleId, isActive);
            var userRolesList = userRoles.ToList();

            if (!userRolesList.Any())
            {
                var notFoundResponse = ResponseStructure<object>.NotFound("No se encontraron roles asignados");
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(userRolesList, $"{userRolesList.Count} rol(es) asignado(s) encontrado(s)");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al obtener roles del usuario: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Obtiene permisos directos de un usuario
    /// </summary>
    [HttpGet("user-permissions")]
    public async Task<IActionResult> GetUserPermissions(
        [FromQuery] int? userId = null,
        [FromQuery] int? permissionId = null,
        [FromQuery] bool? isActive = true)
    {
        try
        {
            var userPermissions = await _rbacService.GetUserPermissionsAsync(userId, permissionId, isActive);
            var userPermissionsList = userPermissions.ToList();

            if (!userPermissionsList.Any())
            {
                var notFoundResponse = ResponseStructure<object>.NotFound("No se encontraron permisos directos");
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(userPermissionsList, $"{userPermissionsList.Count} permiso(s) directo(s) encontrado(s)");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al obtener permisos del usuario: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Obtiene denegaciones de permisos de un usuario
    /// </summary>
    [HttpGet("user-permission-denials")]
    public async Task<IActionResult> GetUserPermissionDenials(
        [FromQuery] int? userId = null,
        [FromQuery] int? permissionId = null,
        [FromQuery] bool? isActive = true)
    {
        try
        {
            var denials = await _rbacService.GetUserPermissionDenialsAsync(userId, permissionId, isActive);
            var denialsList = denials.ToList();

            if (!denialsList.Any())
            {
                var notFoundResponse = ResponseStructure<object>.NotFound("No se encontraron denegaciones");
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(denialsList, $"{denialsList.Count} denegación(es) encontrada(s)");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al obtener denegaciones: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Obtiene todos los permisos efectivos de un usuario (roles + permisos directos - denegaciones)
    /// </summary>
    [HttpGet("user-effective-permissions/{userId}")]
    public async Task<IActionResult> GetEffectiveUserPermissions(int userId)
    {
        try
        {
            var permissions = await _rbacService.GetEffectiveUserPermissionsAsync(userId);
            var permissionsList = permissions.ToList();

            var response = ResponseStructure<object>.Success(permissionsList, $"{permissionsList.Count} permiso(s) efectivo(s) encontrado(s)");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al obtener permisos efectivos: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region POST - Create

    /// <summary>
    /// Crea un nuevo recurso
    /// </summary>
    [HttpPost("resources")]
    public async Task<IActionResult> CreateResource([FromBody] Resource resource)
    {
        try
        {
            var resourceId = await _rbacService.CreateResourceAsync(resource);
            var response = ResponseStructure<object>.Success(new { resourceId }, "Recurso creado exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al crear recurso: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Crea una nueva acción
    /// </summary>
    [HttpPost("actions")]
    public async Task<IActionResult> CreateAction([FromBody] ModelLayer.Shared.Entities.Action action)
    {
        try
        {
            var actionId = await _rbacService.CreateActionAsync(action);
            var response = ResponseStructure<object>.Success(new { actionId }, "Acción creada exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al crear acción: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Crea un nuevo permiso
    /// </summary>
    [HttpPost("permissions")]
    public async Task<IActionResult> CreatePermission([FromBody] Permission permission)
    {
        try
        {
            var permissionId = await _rbacService.CreatePermissionAsync(permission);
            var response = ResponseStructure<object>.Success(new { permissionId }, "Permiso creado exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al crear permiso: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Crea un nuevo rol
    /// </summary>
    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] Role role)
    {
        try
        {
            var roleId = await _rbacService.CreateRoleAsync(role);
            var response = ResponseStructure<object>.Success(new { roleId }, "Rol creado exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al crear rol: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Asigna un permiso a un rol
    /// </summary>
    [HttpPost("role-permissions")]
    public async Task<IActionResult> AssignPermissionToRole([FromBody] AssignPermissionToRoleRequest request)
    {
        try
        {
            var rolePermissionId = await _rbacService.AssignPermissionToRoleAsync(request.RoleId, request.PermissionId, request.GrantedBy);
            var response = ResponseStructure<object>.Success(new { rolePermissionId }, "Permiso asignado al rol exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al asignar permiso al rol: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Asigna un rol a un usuario
    /// </summary>
    [HttpPost("user-roles")]
    public async Task<IActionResult> AssignRoleToUser([FromBody] AssignRoleToUserRequest request)
    {
        try
        {
            var userRoleId = await _rbacService.AssignRoleToUserAsync(request.UserId, request.RoleId, request.ApplicationId, request.AssignedBy, request.ExpiresAt);
            var response = ResponseStructure<object>.Success(new { userRoleId }, "Rol asignado al usuario exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al asignar rol al usuario: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Otorga un permiso directo a un usuario
    /// </summary>
    [HttpPost("user-permissions")]
    public async Task<IActionResult> GrantPermissionToUser([FromBody] GrantPermissionToUserRequest request)
    {
        try
        {
            var userPermissionId = await _rbacService.GrantPermissionToUserAsync(request.UserId, request.PermissionId, request.GrantedBy, request.ExpiresAt);
            var response = ResponseStructure<object>.Success(new { userPermissionId }, "Permiso otorgado al usuario exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al otorgar permiso al usuario: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Deniega un permiso a un usuario
    /// </summary>
    [HttpPost("user-permission-denials")]
    public async Task<IActionResult> DenyPermissionToUser([FromBody] DenyPermissionToUserRequest request)
    {
        try
        {
            var denialId = await _rbacService.DenyPermissionToUserAsync(request.UserId, request.PermissionId, request.DeniedBy, request.Reason);
            var response = ResponseStructure<object>.Success(new { denialId }, "Permiso denegado al usuario exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al denegar permiso al usuario: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region PUT - Update

    /// <summary>
    /// Actualiza un recurso
    /// </summary>
    [HttpPut("resources/{resourceId}")]
    public async Task<IActionResult> UpdateResource(int resourceId, [FromBody] Resource resource)
    {
        try
        {
            resource.ResourceId = resourceId;
            var success = await _rbacService.UpdateResourceAsync(resource);
            
            if (!success)
            {
                var notFoundResponse = ResponseStructure<object>.NotFound("Recurso no encontrado");
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(null, "Recurso actualizado exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al actualizar recurso: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Actualiza un permiso
    /// </summary>
    [HttpPut("permissions/{permissionId}")]
    public async Task<IActionResult> UpdatePermission(int permissionId, [FromBody] Permission permission)
    {
        try
        {
            permission.PermissionId = permissionId;
            var success = await _rbacService.UpdatePermissionAsync(permission);
            
            if (!success)
            {
                var notFoundResponse = ResponseStructure<object>.NotFound("Permiso no encontrado");
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(null, "Permiso actualizado exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al actualizar permiso: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Actualiza un rol
    /// </summary>
    [HttpPut("roles/{roleId}")]
    public async Task<IActionResult> UpdateRole(int roleId, [FromBody] Role role)
    {
        try
        {
            role.RoleId = roleId;
            var success = await _rbacService.UpdateRoleAsync(role);
            
            if (!success)
            {
                var notFoundResponse = ResponseStructure<object>.NotFound("Rol no encontrado");
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(null, "Rol actualizado exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al actualizar rol: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region DELETE

    /// <summary>
    /// Elimina (desactiva) un recurso
    /// </summary>
    [HttpDelete("resources/{resourceId}")]
    public async Task<IActionResult> DeleteResource(int resourceId)
    {
        try
        {
            var success = await _rbacService.DeleteResourceAsync(resourceId);
            
            if (!success)
            {
                var notFoundResponse = ResponseStructure<object>.NotFound("Recurso no encontrado");
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(null, "Recurso eliminado exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al eliminar recurso: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Elimina (desactiva) un permiso
    /// </summary>
    [HttpDelete("permissions/{permissionId}")]
    public async Task<IActionResult> DeletePermission(int permissionId)
    {
        try
        {
            var success = await _rbacService.DeletePermissionAsync(permissionId);
            
            if (!success)
            {
                var notFoundResponse = ResponseStructure<object>.NotFound("Permiso no encontrado");
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(null, "Permiso eliminado exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al eliminar permiso: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Elimina (desactiva) un rol
    /// </summary>
    [HttpDelete("roles/{roleId}")]
    public async Task<IActionResult> DeleteRole(int roleId)
    {
        try
        {
            var success = await _rbacService.DeleteRoleAsync(roleId);
            
            if (!success)
            {
                var notFoundResponse = ResponseStructure<object>.NotFound("Rol no encontrado");
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(null, "Rol eliminado exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al eliminar rol: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Remueve un permiso de un rol
    /// </summary>
    [HttpDelete("role-permissions")]
    public async Task<IActionResult> RemovePermissionFromRole([FromQuery] int roleId, [FromQuery] int permissionId)
    {
        try
        {
            var success = await _rbacService.RemovePermissionFromRoleAsync(roleId, permissionId);
            
            if (!success)
            {
                var notFoundResponse = ResponseStructure<object>.NotFound("Asignación no encontrada");
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(null, "Permiso removido del rol exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al remover permiso del rol: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Remueve un rol de un usuario
    /// </summary>
    [HttpDelete("user-roles")]
    public async Task<IActionResult> RemoveRoleFromUser([FromQuery] int userId, [FromQuery] int roleId)
    {
        try
        {
            var success = await _rbacService.RemoveRoleFromUserAsync(userId, roleId);
            
            if (!success)
            {
                var notFoundResponse = ResponseStructure<object>.NotFound("Asignación no encontrada");
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(null, "Rol removido del usuario exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al remover rol del usuario: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Revoca un permiso directo de un usuario
    /// </summary>
    [HttpDelete("user-permissions")]
    public async Task<IActionResult> RevokePermissionFromUser([FromQuery] int userId, [FromQuery] int permissionId)
    {
        try
        {
            var success = await _rbacService.RevokePermissionFromUserAsync(userId, permissionId);
            
            if (!success)
            {
                var notFoundResponse = ResponseStructure<object>.NotFound("Permiso no encontrado");
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(null, "Permiso revocado del usuario exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al revocar permiso del usuario: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Remueve una denegación de permiso de un usuario
    /// </summary>
    [HttpDelete("user-permission-denials")]
    public async Task<IActionResult> RemoveDenialFromUser([FromQuery] int userId, [FromQuery] int permissionId)
    {
        try
        {
            var success = await _rbacService.RemoveDenialFromUserAsync(userId, permissionId);
            
            if (!success)
            {
                var notFoundResponse = ResponseStructure<object>.NotFound("Denegación no encontrada");
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(null, "Denegación removida del usuario exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al remover denegación del usuario: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    #endregion
}

#region Request Models

public class AssignPermissionToRoleRequest
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    public int? GrantedBy { get; set; }
}

public class AssignRoleToUserRequest
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public int ApplicationId { get; set; }
    public int? AssignedBy { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class GrantPermissionToUserRequest
{
    public int UserId { get; set; }
    public int PermissionId { get; set; }
    public int? GrantedBy { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class DenyPermissionToUserRequest
{
    public int UserId { get; set; }
    public int PermissionId { get; set; }
    public int? DeniedBy { get; set; }
    public string? Reason { get; set; }
}

#endregion

