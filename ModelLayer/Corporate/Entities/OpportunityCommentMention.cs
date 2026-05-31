namespace ModelLayer.Corporate.Entities;

/// <summary>
/// @mentions in opportunity comments (triggers email notifications)
/// </summary>
public class OpportunityCommentMention
{
    public int CommentMentionId { get; set; }
    public int CommentId { get; set; }
    public int MentionedUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
