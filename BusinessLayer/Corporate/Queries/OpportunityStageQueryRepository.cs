using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

public class OpportunityStageQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public OpportunityStageQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<IEnumerable<OpportunityStage>> GetAllActiveAsync()
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                OpportunityStageId, StageKey, DisplayName, SortOrder, 
                DefaultChecklistText, IsActive, CreatedAt
            FROM [Corporate].[OpportunityStages]
            WHERE IsActive = 1
            ORDER BY SortOrder ASC";

        return await connection.QueryAsync<OpportunityStage>(sql);
    }

    public async Task<OpportunityStage?> GetByKeyAsync(string stageKey)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                OpportunityStageId, StageKey, DisplayName, SortOrder, 
                DefaultChecklistText, IsActive, CreatedAt
            FROM [Corporate].[OpportunityStages]
            WHERE StageKey = @StageKey";

        return await connection.QuerySingleOrDefaultAsync<OpportunityStage>(sql, new { StageKey = stageKey });
    }
}
