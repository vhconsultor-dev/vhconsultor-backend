using BusinessLayer.Shared.Commands;
using BusinessLayer.Shared.Queries;

namespace ApplicationLayer.Shared;

public class PermissionService
{
    private readonly CreatePermissionCommand _createPermissionCommand;
    private readonly UpdatePermissionCommand _updatePermissionCommand;
    private readonly DeletePermissionCommand _deletePermissionCommand;
    private readonly PermissionQueryRepository _permissionQueryRepository;

    public PermissionService(
        CreatePermissionCommand createPermissionCommand,
        UpdatePermissionCommand updatePermissionCommand,
        DeletePermissionCommand deletePermissionCommand,
        PermissionQueryRepository permissionQueryRepository)
    {
        _createPermissionCommand = createPermissionCommand;
        _updatePermissionCommand = updatePermissionCommand;
        _deletePermissionCommand = deletePermissionCommand;
        _permissionQueryRepository = permissionQueryRepository;
    }

    public async Task<CreatePermissionResponse> CreatePermissionAsync(CreatePermissionRequest request)
    {
        return await _createPermissionCommand.ExecuteAsync(request);
    }

    public async Task<UpdatePermissionResponse> UpdatePermissionAsync(int permissionId, UpdatePermissionRequest request)
    {
        return await _updatePermissionCommand.ExecuteAsync(permissionId, request);
    }

    public async Task<DeletePermissionResponse> DeletePermissionAsync(int permissionId)
    {
        return await _deletePermissionCommand.ExecuteAsync(permissionId);
    }

    public async Task<List<PermissionDto>> GetAllPermissionsAsync(
        int? resourceId = null,
        int? actionId = null,
        bool? isActive = null,
        string? searchTerm = null)
    {
        return await _permissionQueryRepository.GetAllPermissionsAsync(resourceId, actionId, isActive, searchTerm);
    }

    public async Task<PermissionDto?> GetPermissionByIdAsync(int permissionId)
    {
        return await _permissionQueryRepository.GetPermissionByIdAsync(permissionId);
    }
}

