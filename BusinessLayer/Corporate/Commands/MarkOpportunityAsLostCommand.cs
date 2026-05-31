using Microsoft.EntityFrameworkCore;
using ModelLayer;

namespace BusinessLayer.Corporate.Commands;

public class MarkOpportunityAsLostCommand
{
    private readonly DBcontext _context;

    public MarkOpportunityAsLostCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task ExecuteAsync(MarkOpportunityAsLostRequest request)
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
                $"Cannot mark opportunity as lost. Current status is {opportunity.Status}, must be Open.");

        // 3. Validate lost reason exists
        var lostReason = await _context.OpportunityLostReasons
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ReasonKey == request.LostReasonKey);

        if (lostReason == null)
            throw new InvalidOperationException(
                $"Lost reason with key '{request.LostReasonKey}' not found.");

        if (!lostReason.IsActive)
            throw new InvalidOperationException(
                $"Lost reason with key '{request.LostReasonKey}' is not active.");

        // 4. Update opportunity
        opportunity.Status = "Lost";
        opportunity.LostAt = DateTime.UtcNow;
        opportunity.LostByUserId = request.LostByUserId;
        opportunity.LostReasonKey = request.LostReasonKey;
        opportunity.LostReasonNotes = request.LostReasonNotes;
        opportunity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}

public class MarkOpportunityAsLostRequest
{
    public int OpportunityId { get; set; }
    public string LostReasonKey { get; set; } = string.Empty;
    public string? LostReasonNotes { get; set; }
    public int LostByUserId { get; set; }
}
