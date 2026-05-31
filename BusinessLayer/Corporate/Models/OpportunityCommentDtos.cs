namespace BusinessLayer.Corporate.Models;

/// <summary>Usuario corporate resumido para @mentions y avatares en CRM.</summary>
public class CorporateUserSummaryDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public bool IsActive { get; set; }
}

public class OpportunityCommentMentionDetailDto
{
    public int CommentMentionId { get; set; }
    public int CommentId { get; set; }
    public int MentionedUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public CorporateUserSummaryDto User { get; set; } = new();
}

public class OpportunityCommentDetailDto
{
    public int CommentId { get; set; }
    public int OpportunityId { get; set; }
    public int AuthorUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public CorporateUserSummaryDto Author { get; set; } = new();
    public List<OpportunityCommentMentionDetailDto> Mentions { get; set; } = new();
    public int AttachmentCount { get; set; }
}
