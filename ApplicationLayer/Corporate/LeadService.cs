using BusinessLayer.Corporate.Commands;
using BusinessLayer.Corporate.Queries;
using ModelLayer.Ecommerce.Entities;

namespace ApplicationLayer.Corporate;

/// <summary>
/// Servicio de aplicación para gestionar Leads (CustomerSubmissions) desde Corporate.
/// </summary>
public class LeadService
{
    private readonly CreateLeadCommand _createLeadCommand;
    private readonly UpdateLeadCommand _updateLeadCommand;
    private readonly MarkLeadAsReadCommand _markLeadAsReadCommand;
    private readonly UpdateLeadNotesCommand _updateLeadNotesCommand;
    private readonly LeadQueryRepository _leadQueryRepository;

    public LeadService(
        CreateLeadCommand createLeadCommand,
        UpdateLeadCommand updateLeadCommand,
        MarkLeadAsReadCommand markLeadAsReadCommand,
        UpdateLeadNotesCommand updateLeadNotesCommand,
        LeadQueryRepository leadQueryRepository)
    {
        _createLeadCommand = createLeadCommand;
        _updateLeadCommand = updateLeadCommand;
        _markLeadAsReadCommand = markLeadAsReadCommand;
        _updateLeadNotesCommand = updateLeadNotesCommand;
        _leadQueryRepository = leadQueryRepository;
    }

    #region Commands

    public async Task<int> CreateLeadAsync(CreateLeadRequest request)
        => await _createLeadCommand.ExecuteAsync(request);

    public async Task<bool> UpdateLeadAsync(int submissionId, UpdateLeadRequest request)
        => await _updateLeadCommand.ExecuteAsync(submissionId, request);

    public async Task<bool> MarkAsReadAsync(int submissionId, int userId)
        => await _markLeadAsReadCommand.ExecuteAsync(submissionId, userId);

    public async Task<bool> UpdateNotesAsync(int submissionId, string? notes)
        => await _updateLeadNotesCommand.ExecuteAsync(submissionId, notes);

    #endregion

    #region Queries

    public async Task<IEnumerable<CustomerSubmission>> GetAllLeadsAsync(LeadFilter? filter = null)
        => await _leadQueryRepository.GetAllAsync(filter);

    public async Task<CustomerSubmission?> GetLeadByIdAsync(int submissionId)
        => await _leadQueryRepository.GetByIdAsync(submissionId);

    public async Task<int> GetUnreadCountAsync()
        => await _leadQueryRepository.GetUnreadCountAsync();

    #endregion
}
