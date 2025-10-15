using BusinessLayer.Ecommerce.Commands;

namespace ApplicationLayer.Ecommerce;

public class CustomerSubmissionService
{
    private readonly CreateCustomerSubmissionCommand _createCustomerSubmissionCommand;

    public CustomerSubmissionService(CreateCustomerSubmissionCommand createCustomerSubmissionCommand)
    {
        _createCustomerSubmissionCommand = createCustomerSubmissionCommand;
    }

    #region Commands

    public async Task<int> CreateCustomerSubmissionAsync(CreateCustomerSubmissionRequest request)
    {
        return await _createCustomerSubmissionCommand.ExecuteAsync(request);
    }

    #endregion
}

