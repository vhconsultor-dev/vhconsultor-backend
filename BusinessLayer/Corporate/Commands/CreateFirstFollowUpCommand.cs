using ModelLayer;
using ModelLayer.Corporate.Entities;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Creates the first mandatory follow-up when converting a lead to opportunity.
/// </summary>
public class CreateFirstFollowUpCommand
{
    private readonly DBcontext _context;

    public CreateFirstFollowUpCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task ExecuteAsync(int opportunityId, string stageKey)
    {
        var followUp = new OpportunityFollowUp
        {
            OpportunityId = opportunityId,
            StageKey = stageKey,
            Status = "Pending",
            DueAt = OpportunityFollowUpDueDateCalculator.GetDueAt(stageKey),
            IsRequired = true,
            Notes = null,
            CreatedAt = DateTime.UtcNow
        };

        _context.OpportunityFollowUps.Add(followUp);
        await _context.SaveChangesAsync();
    }
}
