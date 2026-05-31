using Dapper;
using Microsoft.Data.SqlClient;
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
}
