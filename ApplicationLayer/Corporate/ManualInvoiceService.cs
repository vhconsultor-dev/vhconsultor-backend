using BusinessLayer.Corporate.Commands;
using BusinessLayer.Corporate.Queries;

namespace ApplicationLayer.Corporate;

public class ManualInvoiceService
{
    private readonly CreateManualInvoiceHeaderCommand _createCommand;
    private readonly UpdateManualInvoiceHeaderCommand _updateCommand;
    private readonly DeleteManualInvoiceHeaderCommand _deleteCommand;
    private readonly MarkManualInvoiceAsPaidCommand _markAsPaidCommand;
    private readonly ManualInvoiceQueryRepository _queryRepository;

    public ManualInvoiceService(
        CreateManualInvoiceHeaderCommand createCommand,
        UpdateManualInvoiceHeaderCommand updateCommand,
        DeleteManualInvoiceHeaderCommand deleteCommand,
        MarkManualInvoiceAsPaidCommand markAsPaidCommand,
        ManualInvoiceQueryRepository queryRepository)
    {
        _createCommand = createCommand;
        _updateCommand = updateCommand;
        _deleteCommand = deleteCommand;
        _markAsPaidCommand = markAsPaidCommand;
        _queryRepository = queryRepository;
    }

    public Task<CreateManualInvoiceHeaderResponse> CreateAsync(CreateManualInvoiceHeaderRequest request, int createdByUserId)
        => _createCommand.ExecuteAsync(request, createdByUserId);

    public Task<UpdateManualInvoiceHeaderResponse> UpdateAsync(int id, UpdateManualInvoiceHeaderRequest request, string modifiedBy)
        => _updateCommand.ExecuteAsync(id, request, modifiedBy);

    public Task<DeleteManualInvoiceHeaderResponse> DeleteAsync(int id)
        => _deleteCommand.ExecuteAsync(id);

    public Task<MarkManualInvoiceAsPaidResponse> MarkAsPaidAsync(int id, MarkManualInvoiceAsPaidRequest request, int userId)
        => _markAsPaidCommand.ExecuteAsync(id, request, userId);

    public Task<IEnumerable<ManualInvoiceDto>> GetAsync(ManualInvoiceFilters filters)
        => _queryRepository.GetAsync(filters);
}
