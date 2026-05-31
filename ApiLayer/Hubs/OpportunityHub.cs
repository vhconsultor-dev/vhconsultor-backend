using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace ApiLayer.Hubs;

/// <summary>
/// SignalR hub for real-time opportunity notifications (comments, mentions, status changes)
/// </summary>
[Authorize]
public class OpportunityHub : Hub
{
    /// <summary>
    /// Joins a specific opportunity room for real-time updates
    /// </summary>
    public async Task JoinOpportunityRoom(int opportunityId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Opportunity_{opportunityId}");
    }

    /// <summary>
    /// Leaves a specific opportunity room
    /// </summary>
    public async Task LeaveOpportunityRoom(int opportunityId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Opportunity_{opportunityId}");
    }

    /// <summary>
    /// Joins all opportunities for a specific user (assigned or viewer)
    /// </summary>
    public async Task JoinUserOpportunitiesRoom(int userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"UserOpportunities_{userId}");
    }

    /// <summary>
    /// Leaves all opportunities for a specific user
    /// </summary>
    public async Task LeaveUserOpportunitiesRoom(int userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"UserOpportunities_{userId}");
    }
}
