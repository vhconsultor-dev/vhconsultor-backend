using ModelLayer;
using ModelLayer.Corporate.Entities;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Creates the first mandatory follow-up when converting a lead to opportunity.
/// Due today at end of business (11:59 PM UTC).
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
        var today = DateTime.UtcNow.Date;
        var endOfDay = today.AddDays(1).AddSeconds(-1); // 23:59:59 UTC today

        var followUp = new OpportunityFollowUp
        {
            OpportunityId = opportunityId,
            StageKey = stageKey,
            Status = "Pending",
            DueAt = endOfDay,
            IsRequired = true,
            Notes = null,
            CreatedAt = DateTime.UtcNow
        };

        _context.OpportunityFollowUps.Add(followUp);
        await _context.SaveChangesAsync();
    }
}
