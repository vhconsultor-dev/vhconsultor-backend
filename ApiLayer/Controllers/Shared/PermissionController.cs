using ApplicationLayer.Shared;
using ApiLayer.Tools;
using BusinessLayer.Shared.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ApiLayer.Controllers.Shared;

/// <summary>
/// Controller for managing permissions (RBAC)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionController : ControllerBase
{
    private readonly PermissionService _permissionService;
    private readonly ValidationService _validationService;

    public PermissionController(
        PermissionService permissionService,
        ValidationService validationService)
    {
        _permissionService = permissionService;
        _validationService = validationService;
    }

    #region POST - Create Permission

    /// <summary>
    /// Create a new permission
    /// </summary>
    /// <param name="request">Permission data</param>
    /// <returns>Created permission</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/Permission
    ///     {
    ///         "resourceId": 1,
    ///         "actionId": 2,
    ///         "permissionName": "View Invoices",
    ///         "permissionKey": "invoices.view",
    ///         "description": "Allows viewing invoice records"
    ///     }
    /// 
    /// Permission Key format: resource.action (e.g., invoices.create, customers.edit)
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequest request)
    {
        // Validate request
        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var response = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(response);
        }

        try
        {
            var result = await _permissionService.CreatePermissionAsync(request);

            var successResponse = ResponseStructure<CreatePermissionResponse>.Success(
                result,
                result.Message);

            return Ok(successResponse);
        }
        catch (InvalidOperationException ex)
        {
            var errorResponse = ResponseStructure<object>.ValidationError(ex.Message);
            return BadRequest(errorResponse);
        }
        catch (KeyNotFoundException ex)
        {
            var errorResponse = ResponseStructure<object>.Error(ex.Message, 404);
            return NotFound(errorResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error creating permission: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region PUT - Update Permission

    /// <summary>
    /// Update an existing permission
    /// </summary>
    /// <param name="permissionId">Permission ID</param>
    /// <param name="request">Updated permission data</param>
    /// <returns>Updated permission</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     PUT /api/Permission/5
    ///     {
    ///         "permissionName": "View All Invoices",
    ///         "description": "Allows viewing all invoice records including archived",
    ///         "isActive": true
    ///     }
    /// 
    /// Note: ResourceId, ActionId, and PermissionKey cannot be changed after creation
    /// </remarks>
    [HttpPut("{permissionId}")]
    public async Task<IActionResult> UpdatePermission(int permissionId, [FromBody] UpdatePermissionRequest request)
    {
        // Validate request
        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var response = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(response);
        }

        try
        {
            var result = await _permissionService.UpdatePermissionAsync(permissionId, request);

            var successResponse = ResponseStructure<UpdatePermissionResponse>.Success(
                result,
                result.Message);

            return Ok(successResponse);
        }
        catch (KeyNotFoundException ex)
        {
            var errorResponse = ResponseStructure<object>.Error(ex.Message, 404);
            return NotFound(errorResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error updating permission: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region DELETE - Delete Permission

    /// <summary>
    /// Delete a permission (soft delete)
    /// </summary>
    /// <param name="permissionId">Permission ID</param>
    /// <returns>Deletion result</returns>
    /// <remarks>
    /// This performs a soft delete (sets IsActive = false).
    /// The permission cannot be deleted if it's assigned to any role or user.
    /// Remove it from all roles and users first.
    /// </remarks>
    [HttpDelete("{permissionId}")]
    public async Task<IActionResult> DeletePermission(int permissionId)
    {
        try
        {
            var result = await _permissionService.DeletePermissionAsync(permissionId);

            var successResponse = ResponseStructure<DeletePermissionResponse>.Success(
                result,
                result.Message);

            return Ok(successResponse);
        }
        catch (KeyNotFoundException ex)
        {
            var errorResponse = ResponseStructure<object>.Error(ex.Message, 404);
            return NotFound(errorResponse);
        }
        catch (InvalidOperationException ex)
        {
            var errorResponse = ResponseStructure<object>.ValidationError(ex.Message);
            return BadRequest(errorResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error deleting permission: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region GET - Get All Permissions

    /// <summary>
    /// Get all permissions with optional filters
    /// </summary>
    /// <param name="resourceId">Filter by resource ID</param>
    /// <param name="actionId">Filter by action ID</param>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="searchTerm">Search in permission name, key, or description</param>
    /// <returns>List of permissions</returns>
    /// <remarks>
    /// Sample requests:
    /// 
    ///     GET /api/Permission
    ///     GET /api/Permission?resourceId=1
    ///     GET /api/Permission?actionId=2
    ///     GET /api/Permission?isActive=true
    ///     GET /api/Permission?searchTerm=invoice
    ///     GET /api/Permission?resourceId=1&amp;isActive=true
    /// 
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> GetAllPermissions(
        [FromQuery] int? resourceId = null,
        [FromQuery] int? actionId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? searchTerm = null)
    {
        try
        {
            var permissions = await _permissionService.GetAllPermissionsAsync(
                resourceId, actionId, isActive, searchTerm);

            var successResponse = ResponseStructure<List<BusinessLayer.Shared.Queries.PermissionDto>>.Success(
                permissions,
                $"Retrieved {permissions.Count} permission(s)");

            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error retrieving permissions: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region GET - Get Permission by ID

    /// <summary>
    /// Get a specific permission by ID
    /// </summary>
    /// <param name="permissionId">Permission ID</param>
    /// <returns>Permission details</returns>
    [HttpGet("{permissionId}")]
    public async Task<IActionResult> GetPermissionById(int permissionId)
    {
        try
        {
            var permission = await _permissionService.GetPermissionByIdAsync(permissionId);

            if (permission == null)
            {
                var errorResponse = ResponseStructure<object>.Error(
                    $"Permission with ID {permissionId} not found", 404);
                return NotFound(errorResponse);
            }

            var successResponse = ResponseStructure<BusinessLayer.Shared.Queries.PermissionDto>.Success(
                permission,
                "Permission retrieved successfully");

            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error retrieving permission: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion
}


