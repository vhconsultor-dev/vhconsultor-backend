using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

public class OpportunityFollowUpQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public OpportunityFollowUpQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<IEnumerable<OpportunityFollowUp>> GetByOpportunityIdAsync(int opportunityId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                FollowUpId, OpportunityId, StageKey, Status, DueAt, 
                CompletedAt, CompletedByUserId, Notes, IsRequired, 
                CreatedAt, UpdatedAt
            FROM [Corporate].[OpportunityFollowUps]
            WHERE OpportunityId = @OpportunityId
            ORDER BY DueAt ASC, CreatedAt ASC";

        return await connection.QueryAsync<OpportunityFollowUp>(sql, new { OpportunityId = opportunityId });
    }

    public async Task<OpportunityFollowUp?> GetByIdAsync(int followUpId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                FollowUpId, OpportunityId, StageKey, Status, DueAt, 
                CompletedAt, CompletedByUserId, Notes, IsRequired, 
                CreatedAt, UpdatedAt
            FROM [Corporate].[OpportunityFollowUps]
            WHERE FollowUpId = @FollowUpId";

        return await connection.QuerySingleOrDefaultAsync<OpportunityFollowUp>(sql, new { FollowUpId = followUpId });
    }

    public async Task<IEnumerable<PendingFollowUpDto>> GetPendingFollowUpsAsync(int? userId = null)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var whereClause = userId.HasValue
            ? "AND (o.AssignedToUserId = @UserId OR o.ViewerUserId = @UserId)"
            : "";

        var sql = $@"
            SELECT 
                f.FollowUpId,
                f.OpportunityId,
                o.Title AS OpportunityTitle,
                f.StageKey,
                s.DisplayName AS StageDisplayName,
                f.DueAt,
                f.IsRequired,
                o.AssignedToUserId,
                o.ViewerUserId
            FROM [Corporate].[OpportunityFollowUps] f
            INNER JOIN [Corporate].[Opportunities] o ON f.OpportunityId = o.OpportunityId
            LEFT JOIN [Corporate].[OpportunityStages] s ON f.StageKey = s.StageKey
            WHERE f.Status = 'Pending'
                AND o.Status = 'Open'
                {whereClause}
            ORDER BY f.DueAt ASC, f.CreatedAt ASC";

        return await connection.QueryAsync<PendingFollowUpDto>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<OpportunityFollowUpAttachment>> GetAttachmentsByFollowUpIdAsync(int followUpId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                FollowUpAttachmentId, FollowUpId, FileUrl, FileName, 
                ContentType, UploadedBy, UploadedAt
            FROM [Corporate].[OpportunityFollowUpAttachments]
            WHERE FollowUpId = @FollowUpId
            ORDER BY UploadedAt DESC";

        return await connection.QueryAsync<OpportunityFollowUpAttachment>(sql, new { FollowUpId = followUpId });
    }
}

public class PendingFollowUpDto
{
    public int FollowUpId { get; set; }
    public int OpportunityId { get; set; }
    public string OpportunityTitle { get; set; } = string.Empty;
    public string StageKey { get; set; } = string.Empty;
    public string? StageDisplayName { get; set; }
    public DateTime? DueAt { get; set; }
    public bool IsRequired { get; set; }
    public int AssignedToUserId { get; set; }
    public int? ViewerUserId { get; set; }
}
