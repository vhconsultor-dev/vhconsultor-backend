using Dapper;
using Microsoft.Data.SqlClient;
using BusinessLayer.Corporate.Models;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

public class OpportunityCommentQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public OpportunityCommentQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<IEnumerable<OpportunityComment>> GetByOpportunityIdAsync(int opportunityId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                CommentId, OpportunityId, AuthorUserId, Body, 
                CreatedAt, UpdatedAt
            FROM [Corporate].[OpportunityComments]
            WHERE OpportunityId = @OpportunityId
            ORDER BY CreatedAt ASC";

        return await connection.QueryAsync<OpportunityComment>(sql, new { OpportunityId = opportunityId });
    }

    public async Task<OpportunityComment?> GetByIdAsync(int commentId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                CommentId, OpportunityId, AuthorUserId, Body, 
                CreatedAt, UpdatedAt
            FROM [Corporate].[OpportunityComments]
            WHERE CommentId = @CommentId";

        return await connection.QuerySingleOrDefaultAsync<OpportunityComment>(sql, new { CommentId = commentId });
    }

    public async Task<IEnumerable<OpportunityCommentAttachment>> GetAttachmentsByCommentIdAsync(int commentId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                CommentAttachmentId, CommentId, FileUrl, FileName, 
                ContentType, UploadedBy, UploadedAt
            FROM [Corporate].[OpportunityCommentAttachments]
            WHERE CommentId = @CommentId
            ORDER BY UploadedAt DESC";

        return await connection.QueryAsync<OpportunityCommentAttachment>(sql, new { CommentId = commentId });
    }

    public async Task<IEnumerable<OpportunityCommentMention>> GetMentionsByCommentIdAsync(int commentId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                CommentMentionId, CommentId, MentionedUserId, CreatedAt
            FROM [Corporate].[OpportunityCommentMentions]
            WHERE CommentId = @CommentId
            ORDER BY CreatedAt ASC";

        return await connection.QueryAsync<OpportunityCommentMention>(sql, new { CommentId = commentId });
    }

    public async Task<IEnumerable<OpportunityCommentDetailDto>> GetDetailsByOpportunityIdAsync(int opportunityId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string commentsSql = @"
            SELECT
                c.CommentId,
                c.OpportunityId,
                c.AuthorUserId,
                c.Body,
                c.CreatedAt,
                c.UpdatedAt,
                u.UserId,
                u.FirstName,
                u.LastName,
                LTRIM(RTRIM(u.FirstName + ' ' + u.LastName)) AS FullName,
                u.Email,
                u.ProfilePictureUrl,
                u.IsActive,
                (SELECT COUNT(*) FROM [Corporate].[OpportunityCommentAttachments] a WHERE a.CommentId = c.CommentId) AS AttachmentCount
            FROM [Corporate].[OpportunityComments] c
            INNER JOIN [Global].[Users] u ON u.UserId = c.AuthorUserId
            WHERE c.OpportunityId = @OpportunityId
            ORDER BY c.CreatedAt ASC";

        var rows = (await connection.QueryAsync<CommentDetailRow>(commentsSql, new { OpportunityId = opportunityId })).ToList();
        if (rows.Count == 0)
            return Array.Empty<OpportunityCommentDetailDto>();

        var commentIds = rows.Select(r => r.CommentId).ToList();

        const string mentionsSql = @"
            SELECT
                m.CommentMentionId,
                m.CommentId,
                m.MentionedUserId,
                m.CreatedAt,
                u.UserId,
                u.FirstName,
                u.LastName,
                LTRIM(RTRIM(u.FirstName + ' ' + u.LastName)) AS FullName,
                u.Email,
                u.ProfilePictureUrl,
                u.IsActive
            FROM [Corporate].[OpportunityCommentMentions] m
            INNER JOIN [Global].[Users] u ON u.UserId = m.MentionedUserId
            WHERE m.CommentId IN @CommentIds
            ORDER BY m.CreatedAt ASC";

        var mentionRows = (await connection.QueryAsync<MentionDetailRow>(mentionsSql, new { CommentIds = commentIds })).ToList();
        var mentionsByComment = mentionRows.GroupBy(m => m.CommentId).ToDictionary(g => g.Key, g => g.ToList());

        return rows.Select(row =>
        {
            var dto = MapCommentRow(row);
            if (mentionsByComment.TryGetValue(row.CommentId, out var mentions))
            {
                dto.Mentions = mentions.Select(MapMentionRow).ToList();
            }
            return dto;
        });
    }

    public async Task<OpportunityCommentDetailDto?> GetDetailByIdAsync(int commentId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string commentSql = @"
            SELECT
                c.CommentId,
                c.OpportunityId,
                c.AuthorUserId,
                c.Body,
                c.CreatedAt,
                c.UpdatedAt,
                u.UserId,
                u.FirstName,
                u.LastName,
                LTRIM(RTRIM(u.FirstName + ' ' + u.LastName)) AS FullName,
                u.Email,
                u.ProfilePictureUrl,
                u.IsActive,
                (SELECT COUNT(*) FROM [Corporate].[OpportunityCommentAttachments] a WHERE a.CommentId = c.CommentId) AS AttachmentCount
            FROM [Corporate].[OpportunityComments] c
            INNER JOIN [Global].[Users] u ON u.UserId = c.AuthorUserId
            WHERE c.CommentId = @CommentId";

        var row = await connection.QuerySingleOrDefaultAsync<CommentDetailRow>(commentSql, new { CommentId = commentId });
        if (row == null)
            return null;

        const string mentionsSql = @"
            SELECT
                m.CommentMentionId,
                m.CommentId,
                m.MentionedUserId,
                m.CreatedAt,
                u.UserId,
                u.FirstName,
                u.LastName,
                LTRIM(RTRIM(u.FirstName + ' ' + u.LastName)) AS FullName,
                u.Email,
                u.ProfilePictureUrl,
                u.IsActive
            FROM [Corporate].[OpportunityCommentMentions] m
            INNER JOIN [Global].[Users] u ON u.UserId = m.MentionedUserId
            WHERE m.CommentId = @CommentId
            ORDER BY m.CreatedAt ASC";

        var mentionRows = (await connection.QueryAsync<MentionDetailRow>(mentionsSql, new { CommentId = commentId })).ToList();

        var dto = MapCommentRow(row);
        dto.Mentions = mentionRows.Select(MapMentionRow).ToList();
        return dto;
    }

    private static OpportunityCommentDetailDto MapCommentRow(CommentDetailRow row) => new()
    {
        CommentId = row.CommentId,
        OpportunityId = row.OpportunityId,
        AuthorUserId = row.AuthorUserId,
        Body = row.Body,
        CreatedAt = row.CreatedAt,
        UpdatedAt = row.UpdatedAt,
        AttachmentCount = row.AttachmentCount,
        Author = new CorporateUserSummaryDto
        {
            UserId = row.UserId,
            FirstName = row.FirstName,
            LastName = row.LastName,
            FullName = row.FullName,
            Email = row.Email,
            ProfilePictureUrl = row.ProfilePictureUrl,
            IsActive = row.IsActive
        }
    };

    private static OpportunityCommentMentionDetailDto MapMentionRow(MentionDetailRow row) => new()
    {
        CommentMentionId = row.CommentMentionId,
        CommentId = row.CommentId,
        MentionedUserId = row.MentionedUserId,
        CreatedAt = row.CreatedAt,
        User = new CorporateUserSummaryDto
        {
            UserId = row.UserId,
            FirstName = row.FirstName,
            LastName = row.LastName,
            FullName = row.FullName,
            Email = row.Email,
            ProfilePictureUrl = row.ProfilePictureUrl,
            IsActive = row.IsActive
        }
    };

    private class CommentDetailRow
    {
        public int CommentId { get; set; }
        public int OpportunityId { get; set; }
        public int AuthorUserId { get; set; }
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int AttachmentCount { get; set; }
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public bool IsActive { get; set; }
    }

    private class MentionDetailRow
    {
        public int CommentMentionId { get; set; }
        public int CommentId { get; set; }
        public int MentionedUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public bool IsActive { get; set; }
    }
}
