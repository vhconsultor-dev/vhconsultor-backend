using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.EntityFrameworkCore;
using ModelLayer;

namespace ApplicationLayer.Corporate;

/// <summary>
/// Service for sending email notifications when users are @mentioned in opportunity comments
/// </summary>
public class OpportunityCommentNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly DBcontext _context;
    private readonly ILogger<OpportunityCommentNotificationService> _logger;

    public OpportunityCommentNotificationService(
        IConfiguration configuration,
        DBcontext context,
        ILogger<OpportunityCommentNotificationService> logger)
    {
        _configuration = configuration;
        _context = context;
        _logger = logger;
    }

    public async Task SendMentionNotificationsAsync(
        int commentId,
        int opportunityId,
        int authorUserId,
        List<int> mentionedUserIds)
    {
        if (!mentionedUserIds.Any())
            return;

        // Get opportunity details
        var opportunity = await _context.Opportunities
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OpportunityId == opportunityId);

        if (opportunity == null)
            return;

        // Get author details
        var author = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == authorUserId);

        if (author == null)
            return;

        // Get comment details
        var comment = await _context.OpportunityComments
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CommentId == commentId);

        if (comment == null)
            return;

        // Get mentioned users
        var mentionedUsers = await _context.Users
            .AsNoTracking()
            .Where(u => mentionedUserIds.Contains(u.UserId) && u.IsActive)
            .ToListAsync();

        var apiKey = _configuration["SendGrid:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("SendGrid API key is not configured.");

        var fromEmail = _configuration["SendGrid:FromEmail"] ?? "no-reply@vhconsultor.com";
        var fromName = _configuration["SendGrid:FromName"] ?? "VH Consultor";

        var client = new SendGridClient(apiKey);

        foreach (var mentionedUser in mentionedUsers)
        {
            if (string.IsNullOrWhiteSpace(mentionedUser.Email))
                continue;

            var subject = $"You were mentioned in Opportunity: {opportunity.Title}";
            var plainTextContent = $@"
Hello {mentionedUser.FirstName} {mentionedUser.LastName},

{author.FirstName} {author.LastName} mentioned you in a comment on the opportunity ""{opportunity.Title}"".

Comment:
{comment.Body}

Opportunity Details:
- Title: {opportunity.Title}
- Brand: {opportunity.BrandName}
- Status: {opportunity.Status}
- Stage: {opportunity.CurrentStageKey ?? "N/A"}

Please log in to VH Consultor Corporate to view the full opportunity and comment.

---
This is an automated notification from VH Consultor.
";

            var htmlContent = $@"
<html>
<body>
<p>Hello <strong>{mentionedUser.FirstName} {mentionedUser.LastName}</strong>,</p>

<p><strong>{author.FirstName} {author.LastName}</strong> mentioned you in a comment on the opportunity <strong>&quot;{opportunity.Title}&quot;</strong>.</p>

<h3>Comment:</h3>
<div style=""background-color: #f5f5f5; padding: 15px; border-left: 4px solid #007bff; margin: 10px 0;"">
{comment.Body.Replace("\n", "<br/>")}
</div>

<h3>Opportunity Details:</h3>
<ul>
  <li><strong>Title:</strong> {opportunity.Title}</li>
  <li><strong>Brand:</strong> {opportunity.BrandName}</li>
  <li><strong>Status:</strong> {opportunity.Status}</li>
  <li><strong>Stage:</strong> {opportunity.CurrentStageKey ?? "N/A"}</li>
</ul>

<p>Please log in to <strong>VH Consultor Corporate</strong> to view the full opportunity and comment.</p>

<hr />
<p style=""color: #888; font-size: 12px;"">This is an automated notification from VH Consultor.</p>
</body>
</html>
";

            var msg = MailHelper.CreateSingleEmail(
                new EmailAddress(fromEmail, fromName),
                new EmailAddress(mentionedUser.Email, $"{mentionedUser.FirstName} {mentionedUser.LastName}"),
                subject,
                plainTextContent,
                htmlContent
            );

            try
            {
                var sendResponse = await client.SendEmailAsync(msg);
                if (!sendResponse.IsSuccessStatusCode)
                {
                    var body = await sendResponse.Body.ReadAsStringAsync();
                    _logger.LogWarning(
                        "SendGrid mention email failed for user {UserId} ({Email}). Status: {Status}. Body: {Body}",
                        mentionedUser.UserId, mentionedUser.Email, sendResponse.StatusCode, body);
                }
                else
                {
                    _logger.LogInformation(
                        "Mention notification sent to user {UserId} ({Email}) for comment {CommentId}",
                        mentionedUser.UserId, mentionedUser.Email, commentId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send mention notification to user {UserId} ({Email})",
                    mentionedUser.UserId, mentionedUser.Email);
            }
        }
    }
}
