using BusinessLayer.Corporate.Commands;
using BusinessLayer.Corporate.Models;
using BusinessLayer.Corporate.Queries;

namespace ApplicationLayer.Corporate;

public class UserCustomerAssignmentService
{
    private readonly UserCustomerAssignmentQueryRepository _queryRepository;
    private readonly CreateUserCustomerAssignmentCommand _createCommand;
    private readonly UpdateUserCustomerAssignmentCommand _updateCommand;

    public UserCustomerAssignmentService(
        UserCustomerAssignmentQueryRepository queryRepository,
        CreateUserCustomerAssignmentCommand createCommand,
        UpdateUserCustomerAssignmentCommand updateCommand)
    {
        _queryRepository = queryRepository;
        _createCommand = createCommand;
        _updateCommand = updateCommand;
    }

    public async Task<List<UserCustomerAssignmentDetailDto>> GetAssignmentsAsync(
        int? userCustomerAssignmentId = null,
        int? userId = null,
        int? customerId = null,
        bool? isActive = null)
    {
        var rows = await _queryRepository.GetFlatRowsAsync(
            userCustomerAssignmentId, userId, customerId, isActive);
        return UserCustomerAssignmentQueryRepository.MapFlatRowsToDetails(rows);
    }

    public async Task<int> CreateAsync(CreateUserCustomerAssignmentRequest request)
    {
        return await _createCommand.ExecuteAsync(request);
    }

    public async Task<bool> UpdateAsync(int userCustomerAssignmentId, UpdateUserCustomerAssignmentRequest request)
    {
        return await _updateCommand.ExecuteAsync(userCustomerAssignmentId, request);
    }
}
