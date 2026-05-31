using Microsoft.AspNetCore.SignalR;
using ApiLayer.Hubs;

namespace ApiLayer.Services;

/// <summary>
/// Service for sending SignalR notifications related to opportunities
/// </summary>
public class OpportunitySignalRService
{
    private readonly IHubContext<OpportunityHub> _hubContext;

    public OpportunitySignalRService(IHubContext<OpportunityHub> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <summary>
    /// Sends a notification when a new comment is created
    /// </summary>
    public async Task NotifyCommentCreatedAsync(
        int opportunityId,
        int commentId,
        int authorUserId,
        string authorName,
        string commentBody,
        List<int> mentionedUserIds)
    {
        var notification = new
        {
            Type = "CommentCreated",
            OpportunityId = opportunityId,
            CommentId = commentId,
            AuthorUserId = authorUserId,
            AuthorName = authorName,
            CommentBody = commentBody,
            MentionedUserIds = mentionedUserIds,
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients
            .Group($"Opportunity_{opportunityId}")
            .SendAsync("ReceiveCommentNotification", notification);

        foreach (var userId in mentionedUserIds)
        {
            await _hubContext.Clients
                .Group($"UserOpportunities_{userId}")
                .SendAsync("ReceiveMentionNotification", notification);
        }
    }

    /// <summary>
    /// Sends a notification when an opportunity status changes (Won/Lost)
    /// </summary>
    public async Task NotifyStatusChangedAsync(
        int opportunityId,
        string newStatus,
        int? changedByUserId,
        string? changedByName)
    {
        var notification = new
        {
            Type = "StatusChanged",
            OpportunityId = opportunityId,
            NewStatus = newStatus,
            ChangedByUserId = changedByUserId,
            ChangedByName = changedByName,
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients
            .Group($"Opportunity_{opportunityId}")
            .SendAsync("ReceiveStatusChangeNotification", notification);
    }

    /// <summary>
    /// Sends a notification when an opportunity stage changes
    /// </summary>
    public async Task NotifyStageChangedAsync(
        int opportunityId,
        string newStageKey,
        string newStageDisplayName)
    {
        var notification = new
        {
            Type = "StageChanged",
            OpportunityId = opportunityId,
            NewStageKey = newStageKey,
            NewStageDisplayName = newStageDisplayName,
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients
            .Group($"Opportunity_{opportunityId}")
            .SendAsync("ReceiveStageChangeNotification", notification);
    }

    /// <summary>
    /// Sends a notification when a follow-up is completed
    /// </summary>
    public async Task NotifyFollowUpCompletedAsync(
        int opportunityId,
        int followUpId,
        int completedByUserId,
        string completedByName)
    {
        var notification = new
        {
            Type = "FollowUpCompleted",
            OpportunityId = opportunityId,
            FollowUpId = followUpId,
            CompletedByUserId = completedByUserId,
            CompletedByName = completedByName,
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients
            .Group($"Opportunity_{opportunityId}")
            .SendAsync("ReceiveFollowUpNotification", notification);
    }
}
