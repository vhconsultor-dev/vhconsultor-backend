using ModelLayer;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Commands;

public class MarkLeadAsReadCommand
{
    private readonly DBcontext _context;

    public MarkLeadAsReadCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task<bool> ExecuteAsync(int submissionId, int userId)
    {
        var lead = await _context.CustomerSubmissions.FindAsync(submissionId);
        if (lead == null) return false;

        lead.IsRead = true;
        lead.ReadAt = DateTimeService.GetCostaRicaNow();
        lead.ReadByUserId = userId;

        await _context.SaveChangesAsync();
        return true;
    }
}

public class UpdateLeadNotesCommand
{
    private readonly DBcontext _context;

    public UpdateLeadNotesCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task<bool> ExecuteAsync(int submissionId, string? notes)
    {
        var lead = await _context.CustomerSubmissions.FindAsync(submissionId);
        if (lead == null) return false;

        lead.Notes = notes;

        await _context.SaveChangesAsync();
        return true;
    }
}
