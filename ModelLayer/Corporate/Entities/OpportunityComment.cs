namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Comments on opportunities (supervisor and assigned user can comment)
/// </summary>
public class OpportunityComment
{
    public int CommentId { get; set; }
    public int OpportunityId { get; set; }
    public int AuthorUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
