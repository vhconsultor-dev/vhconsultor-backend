using Microsoft.EntityFrameworkCore;
using ModelLayer;

namespace BusinessLayer.Corporate.Commands;

public class ChangeOpportunityStageCommand
{
    private readonly DBcontext _context;

    public ChangeOpportunityStageCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task ExecuteAsync(ChangeOpportunityStageRequest request)
    {
        // 1. Validate opportunity exists
        var opportunity = await _context.Opportunities
            .FirstOrDefaultAsync(o => o.OpportunityId == request.OpportunityId);

        if (opportunity == null)
            throw new InvalidOperationException(
                $"Opportunity with ID {request.OpportunityId} not found.");

        // 2. Validate opportunity is Open
        if (opportunity.Status != "Open")
            throw new InvalidOperationException(
                $"Cannot change stage for opportunity with ID {request.OpportunityId}. Current status is {opportunity.Status}, must be Open.");

        // 3. Validate stage exists
        var stage = await _context.OpportunityStages
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StageKey == request.StageKey);

        if (stage == null)
            throw new InvalidOperationException(
                $"Stage with key '{request.StageKey}' not found.");

        if (!stage.IsActive)
            throw new InvalidOperationException(
                $"Stage with key '{request.StageKey}' is not active.");

        // 4. Update opportunity stage
        opportunity.CurrentStageKey = request.StageKey;
        opportunity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}

public class ChangeOpportunityStageRequest
{
    public int OpportunityId { get; set; }
    public string StageKey { get; set; } = string.Empty;
}
