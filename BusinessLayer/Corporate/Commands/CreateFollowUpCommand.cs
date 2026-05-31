using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Corporate.Entities;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Creates a manual follow-up for an opportunity (optional, not required)
/// </summary>
public class CreateFollowUpCommand
{
    private readonly DBcontext _context;

    public CreateFollowUpCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task<int> ExecuteAsync(CreateFollowUpRequest request)
    {
        // 1. Validate opportunity exists
        var opportunity = await _context.Opportunities
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OpportunityId == request.OpportunityId);

        if (opportunity == null)
            throw new InvalidOperationException(
                $"Opportunity with ID {request.OpportunityId} not found.");

        // 2. Validate opportunity is Open
        if (opportunity.Status != "Open")
            throw new InvalidOperationException(
                $"Cannot create follow-up for opportunity with ID {request.OpportunityId}. Current status is {opportunity.Status}, must be Open.");

        // 3. Validate stage exists if provided
        if (!string.IsNullOrWhiteSpace(request.StageKey))
        {
            var stage = await _context.OpportunityStages
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StageKey == request.StageKey);

            if (stage == null)
                throw new InvalidOperationException(
                    $"Stage with key '{request.StageKey}' not found.");

            if (!stage.IsActive)
                throw new InvalidOperationException(
                    $"Stage with key '{request.StageKey}' is not active.");
        }

        // 4. Create follow-up
        var followUp = new OpportunityFollowUp
        {
            OpportunityId = request.OpportunityId,
            StageKey = request.StageKey ?? opportunity.CurrentStageKey ?? "follow_up",
            Status = "Pending",
            DueAt = request.DueAt,
            IsRequired = false,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.OpportunityFollowUps.Add(followUp);
        await _context.SaveChangesAsync();

        return followUp.FollowUpId;
    }
}

public class CreateFollowUpRequest
{
    public int OpportunityId { get; set; }
    public string? StageKey { get; set; }
    public DateTime? DueAt { get; set; }
    public string? Notes { get; set; }
}
