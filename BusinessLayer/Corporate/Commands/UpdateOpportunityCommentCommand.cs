using Microsoft.EntityFrameworkCore;
using ModelLayer;

namespace BusinessLayer.Corporate.Commands;

public class UpdateOpportunityCommentCommand
{
    private readonly DBcontext _context;

    public UpdateOpportunityCommentCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task ExecuteAsync(UpdateOpportunityCommentRequest request)
    {
        // 1. Validate comment exists
        var comment = await _context.OpportunityComments
            .FirstOrDefaultAsync(c => c.CommentId == request.CommentId);

        if (comment == null)
            throw new InvalidOperationException(
                $"Comment with ID {request.CommentId} not found.");

        // 2. Validate author matches (only author can edit)
        if (comment.AuthorUserId != request.AuthorUserId)
            throw new InvalidOperationException(
                "Only the comment author can edit this comment.");

        // 3. Update comment
        comment.Body = request.Body;
        comment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}

public class UpdateOpportunityCommentRequest
{
    public int CommentId { get; set; }
    public int AuthorUserId { get; set; }
    public string Body { get; set; } = string.Empty;
}
