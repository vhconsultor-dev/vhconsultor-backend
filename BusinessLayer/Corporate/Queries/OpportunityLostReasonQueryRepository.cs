using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

public class OpportunityLostReasonQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public OpportunityLostReasonQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<IEnumerable<OpportunityLostReason>> GetAllActiveAsync()
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                OpportunityLostReasonId, ReasonKey, DisplayName, SortOrder, 
                IsActive, CreatedAt
            FROM [Corporate].[OpportunityLostReasons]
            WHERE IsActive = 1
            ORDER BY SortOrder ASC";

        return await connection.QueryAsync<OpportunityLostReason>(sql);
    }
}
