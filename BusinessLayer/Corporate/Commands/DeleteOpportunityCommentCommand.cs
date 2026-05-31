using Microsoft.EntityFrameworkCore;
using ModelLayer;

namespace BusinessLayer.Corporate.Commands;

public class DeleteOpportunityCommentCommand
{
    private readonly DBcontext _context;

    public DeleteOpportunityCommentCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task ExecuteAsync(int commentId, int requestingUserId)
    {
        // 1. Validate comment exists
        var comment = await _context.OpportunityComments
            .FirstOrDefaultAsync(c => c.CommentId == commentId);

        if (comment == null)
            throw new InvalidOperationException(
                $"Comment with ID {commentId} not found.");

        // 2. Validate requester is author (only author can delete)
        if (comment.AuthorUserId != requestingUserId)
            throw new InvalidOperationException(
                "Only the comment author can delete this comment.");

        // 3. Delete mentions first (cascade)
        var mentions = await _context.OpportunityCommentMentions
            .Where(m => m.CommentId == commentId)
            .ToListAsync();

        if (mentions.Any())
            _context.OpportunityCommentMentions.RemoveRange(mentions);

        // 4. Note: Attachments are not deleted from Azure Blob Storage, only DB records
        var attachments = await _context.OpportunityCommentAttachments
            .Where(a => a.CommentId == commentId)
            .ToListAsync();

        if (attachments.Any())
            _context.OpportunityCommentAttachments.RemoveRange(attachments);

        // 5. Delete comment
        _context.OpportunityComments.Remove(comment);

        await _context.SaveChangesAsync();
    }
}
