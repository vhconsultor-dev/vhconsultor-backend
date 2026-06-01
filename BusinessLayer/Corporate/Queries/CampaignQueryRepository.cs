using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

public class CampaignQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public CampaignQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<IEnumerable<Campaign>> GetAllAsync(string? status = null)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT CampaignId, Name, Subject, BodyContent, Status, ScheduledAt, SentAt,
                   CreatedByUserId, CreatedAt, UpdatedAt
            FROM [Corporate].[Campaigns]
            WHERE 1=1";

        var parameters = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(status))
        {
            sql += " AND Status = @Status";
            parameters.Add("Status", status);
        }

        sql += " ORDER BY CreatedAt DESC";
        return await connection.QueryAsync<Campaign>(sql, parameters);
    }

    public async Task<Campaign?> GetByIdAsync(int campaignId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT CampaignId, Name, Subject, BodyContent, Status, ScheduledAt, SentAt,
                   CreatedByUserId, CreatedAt, UpdatedAt
            FROM [Corporate].[Campaigns]
            WHERE CampaignId = @CampaignId";

        return await connection.QuerySingleOrDefaultAsync<Campaign>(sql, new { CampaignId = campaignId });
    }

    public async Task<IEnumerable<CampaignAttachment>> GetAttachmentsAsync(int campaignId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT CampaignAttachmentId, CampaignId, FileUrl, FileName, ContentType,
                   UploadedByUserId, UploadedAt
            FROM [Corporate].[CampaignAttachments]
            WHERE CampaignId = @CampaignId
            ORDER BY UploadedAt DESC";

        return await connection.QueryAsync<CampaignAttachment>(sql, new { CampaignId = campaignId });
    }

    public async Task<IEnumerable<CampaignProspect>> GetProspectsAsync(int campaignId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT CampaignProspectId, CampaignId, Email, LeadId, FirstName, LastName, SentAt, CreatedAt
            FROM [Corporate].[CampaignProspects]
            WHERE CampaignId = @CampaignId
            ORDER BY CreatedAt ASC";

        return await connection.QueryAsync<CampaignProspect>(sql, new { CampaignId = campaignId });
    }
}
