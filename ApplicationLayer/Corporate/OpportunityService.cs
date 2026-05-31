using BusinessLayer.Corporate.Commands;
using BusinessLayer.Corporate.Queries;
using ModelLayer.Corporate.Entities;

namespace ApplicationLayer.Corporate;

/// <summary>
/// Application service for managing Opportunities in Corporate
/// </summary>
public class OpportunityService
{
    private readonly ConvertLeadToOpportunityCommand _convertLeadCommand;
    private readonly CreateFollowUpCommand _createFollowUpCommand;
    private readonly CompleteFollowUpCommand _completeFollowUpCommand;
    private readonly ChangeOpportunityStageCommand _changeStageCommand;
    private readonly MarkOpportunityAsWonCommand _markAsWonCommand;
    private readonly MarkOpportunityAsLostCommand _markAsLostCommand;
    private readonly CreateOpportunityCommentCommand _createCommentCommand;
    private readonly UpdateOpportunityCommentCommand _updateCommentCommand;
    private readonly DeleteOpportunityCommentCommand _deleteCommentCommand;
    private readonly OpportunityQueryRepository _opportunityQueryRepository;
    private readonly OpportunityFollowUpQueryRepository _followUpQueryRepository;
    private readonly OpportunityStageQueryRepository _stageQueryRepository;
    private readonly OpportunityLostReasonQueryRepository _lostReasonQueryRepository;
    private readonly OpportunityCommentQueryRepository _commentQueryRepository;
    private readonly OpportunityCommentNotificationService _commentNotificationService;

    public OpportunityService(
        ConvertLeadToOpportunityCommand convertLeadCommand,
        CreateFollowUpCommand createFollowUpCommand,
        CompleteFollowUpCommand completeFollowUpCommand,
        ChangeOpportunityStageCommand changeStageCommand,
        MarkOpportunityAsWonCommand markAsWonCommand,
        MarkOpportunityAsLostCommand markAsLostCommand,
        CreateOpportunityCommentCommand createCommentCommand,
        UpdateOpportunityCommentCommand updateCommentCommand,
        DeleteOpportunityCommentCommand deleteCommentCommand,
        OpportunityQueryRepository opportunityQueryRepository,
        OpportunityFollowUpQueryRepository followUpQueryRepository,
        OpportunityStageQueryRepository stageQueryRepository,
        OpportunityLostReasonQueryRepository lostReasonQueryRepository,
        OpportunityCommentQueryRepository commentQueryRepository,
        OpportunityCommentNotificationService commentNotificationService)
    {
        _convertLeadCommand = convertLeadCommand;
        _createFollowUpCommand = createFollowUpCommand;
        _completeFollowUpCommand = completeFollowUpCommand;
        _changeStageCommand = changeStageCommand;
        _markAsWonCommand = markAsWonCommand;
        _markAsLostCommand = markAsLostCommand;
        _createCommentCommand = createCommentCommand;
        _updateCommentCommand = updateCommentCommand;
        _deleteCommentCommand = deleteCommentCommand;
        _opportunityQueryRepository = opportunityQueryRepository;
        _followUpQueryRepository = followUpQueryRepository;
        _stageQueryRepository = stageQueryRepository;
        _lostReasonQueryRepository = lostReasonQueryRepository;
        _commentQueryRepository = commentQueryRepository;
        _commentNotificationService = commentNotificationService;
    }

    #region Commands

    public async Task<int> ConvertLeadToOpportunityAsync(ConvertLeadToOpportunityRequest request)
        => await _convertLeadCommand.ExecuteAsync(request);

    public async Task<int> CreateFollowUpAsync(CreateFollowUpRequest request)
        => await _createFollowUpCommand.ExecuteAsync(request);

    public async Task<CompleteFollowUpResponse> CompleteFollowUpAsync(CompleteFollowUpRequest request)
        => await _completeFollowUpCommand.ExecuteAsync(request);

    public async Task ChangeOpportunityStageAsync(ChangeOpportunityStageRequest request)
        => await _changeStageCommand.ExecuteAsync(request);

    public async Task MarkOpportunityAsWonAsync(MarkOpportunityAsWonRequest request)
        => await _markAsWonCommand.ExecuteAsync(request);

    public async Task MarkOpportunityAsLostAsync(MarkOpportunityAsLostRequest request)
        => await _markAsLostCommand.ExecuteAsync(request);

    public async Task<CreateOpportunityCommentResponse> CreateCommentAsync(CreateOpportunityCommentRequest request)
    {
        var response = await _createCommentCommand.ExecuteAsync(request);

        if (response.MentionedUserIds.Any())
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _commentNotificationService.SendMentionNotificationsAsync(
                        response.CommentId,
                        request.OpportunityId,
                        request.AuthorUserId,
                        response.MentionedUserIds);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send mention notifications: {ex.Message}");
                }
            });
        }

        return response;
    }

    public async Task UpdateCommentAsync(UpdateOpportunityCommentRequest request)
        => await _updateCommentCommand.ExecuteAsync(request);

    public async Task DeleteCommentAsync(int commentId, int requestingUserId)
        => await _deleteCommentCommand.ExecuteAsync(commentId, requestingUserId);

    #endregion

    #region Queries

    public async Task<IEnumerable<Opportunity>> GetAllOpportunitiesAsync(OpportunityFilter? filter = null)
        => await _opportunityQueryRepository.GetAllAsync(filter);

    public async Task<Opportunity?> GetOpportunityByIdAsync(int opportunityId)
        => await _opportunityQueryRepository.GetByIdAsync(opportunityId);

    public async Task<int> GetCountByStatusAsync(string status)
        => await _opportunityQueryRepository.GetCountByStatusAsync(status);

    public async Task<IEnumerable<OpportunityFollowUp>> GetFollowUpsByOpportunityIdAsync(int opportunityId)
        => await _followUpQueryRepository.GetByOpportunityIdAsync(opportunityId);

    public async Task<OpportunityFollowUp?> GetFollowUpByIdAsync(int followUpId)
        => await _followUpQueryRepository.GetByIdAsync(followUpId);

    public async Task<IEnumerable<PendingFollowUpDto>> GetPendingFollowUpsAsync(int? userId = null)
        => await _followUpQueryRepository.GetPendingFollowUpsAsync(userId);

    public async Task<IEnumerable<OpportunityFollowUpAttachment>> GetFollowUpAttachmentsAsync(int followUpId)
        => await _followUpQueryRepository.GetAttachmentsByFollowUpIdAsync(followUpId);

    public async Task<IEnumerable<OpportunityStage>> GetAllActiveStagesAsync()
        => await _stageQueryRepository.GetAllActiveAsync();

    public async Task<OpportunityStage?> GetStageByKeyAsync(string stageKey)
        => await _stageQueryRepository.GetByKeyAsync(stageKey);

    public async Task<IEnumerable<OpportunityLostReason>> GetAllActiveLostReasonsAsync()
        => await _lostReasonQueryRepository.GetAllActiveAsync();

    public async Task<IEnumerable<OpportunityComment>> GetCommentsByOpportunityIdAsync(int opportunityId)
        => await _commentQueryRepository.GetByOpportunityIdAsync(opportunityId);

    public async Task<OpportunityComment?> GetCommentByIdAsync(int commentId)
        => await _commentQueryRepository.GetByIdAsync(commentId);

    public async Task<IEnumerable<OpportunityCommentAttachment>> GetCommentAttachmentsAsync(int commentId)
        => await _commentQueryRepository.GetAttachmentsByCommentIdAsync(commentId);

    public async Task<IEnumerable<OpportunityCommentMention>> GetCommentMentionsAsync(int commentId)
        => await _commentQueryRepository.GetMentionsByCommentIdAsync(commentId);

    #endregion
}
