using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

public class OpportunityQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public OpportunityQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<IEnumerable<Opportunity>> GetAllAsync(OpportunityFilter? filter = null)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var where = new List<string>();
        var parameters = new DynamicParameters();

        if (filter != null)
        {
            if (filter.Status != null)
            {
                where.Add("Status = @Status");
                parameters.Add("Status", filter.Status);
            }

            if (filter.CurrentStageKey != null)
            {
                where.Add("CurrentStageKey = @CurrentStageKey");
                parameters.Add("CurrentStageKey", filter.CurrentStageKey);
            }

            if (filter.AssignedToUserId.HasValue)
            {
                where.Add("AssignedToUserId = @AssignedToUserId");
                parameters.Add("AssignedToUserId", filter.AssignedToUserId.Value);
            }

            if (filter.ViewerUserId.HasValue)
            {
                where.Add("ViewerUserId = @ViewerUserId");
                parameters.Add("ViewerUserId", filter.ViewerUserId.Value);
            }

            if (filter.MyOpportunities.HasValue && filter.MyOpportunities.Value && filter.UserId.HasValue)
            {
                where.Add("(AssignedToUserId = @UserId OR ViewerUserId = @UserId)");
                parameters.Add("UserId", filter.UserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                where.Add("(FirstName LIKE @Search OR LastName LIKE @Search OR Email LIKE @Search OR BrandName LIKE @Search OR Title LIKE @Search)");
                parameters.Add("Search", $"%{filter.Search}%");
            }

            if (filter.FromDate.HasValue)
            {
                where.Add("ConvertedAt >= @FromDate");
                parameters.Add("FromDate", filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                where.Add("ConvertedAt <= @ToDate");
                parameters.Add("ToDate", filter.ToDate.Value);
            }
        }

        var whereClause = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "";
        var sql = $@"
            SELECT 
                OpportunityId, SubmissionId, Status, CurrentStageKey, Title,
                FirstName, LastName, Email, PhoneNumber, Country, BrandName,
                NumberOfListings, ProductPageLink, StoreLink, SelectedPlatform,
                AccountType, ServiceType, AnnualSalesRange, AdvertisingBudgetRange,
                PromotionalBudgetRange, AdditionalDetails,
                AssignedToUserId, ViewerUserId,
                CustomerId, ContractId, WonAt, WonByUserId,
                LostAt, LostByUserId, LostReasonKey, LostReasonNotes,
                ConvertedAt, ConvertedByUserId, CreatedAt, UpdatedAt
            FROM [Corporate].[Opportunities]
            {whereClause}
            ORDER BY ConvertedAt DESC";

        return await connection.QueryAsync<Opportunity>(sql, parameters);
    }

    public async Task<Opportunity?> GetByIdAsync(int opportunityId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                OpportunityId, SubmissionId, Status, CurrentStageKey, Title,
                FirstName, LastName, Email, PhoneNumber, Country, BrandName,
                NumberOfListings, ProductPageLink, StoreLink, SelectedPlatform,
                AccountType, ServiceType, AnnualSalesRange, AdvertisingBudgetRange,
                PromotionalBudgetRange, AdditionalDetails,
                AssignedToUserId, ViewerUserId,
                CustomerId, ContractId, WonAt, WonByUserId,
                LostAt, LostByUserId, LostReasonKey, LostReasonNotes,
                ConvertedAt, ConvertedByUserId, CreatedAt, UpdatedAt
            FROM [Corporate].[Opportunities]
            WHERE OpportunityId = @OpportunityId";

        return await connection.QuerySingleOrDefaultAsync<Opportunity>(sql, new { OpportunityId = opportunityId });
    }

    public async Task<int> GetCountByStatusAsync(string status)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = "SELECT COUNT(1) FROM [Corporate].[Opportunities] WHERE Status = @Status";
        return await connection.ExecuteScalarAsync<int>(sql, new { Status = status });
    }
}

public class OpportunityFilter
{
    public string? Status { get; set; }
    public string? CurrentStageKey { get; set; }
    public int? AssignedToUserId { get; set; }
    public int? ViewerUserId { get; set; }
    public bool? MyOpportunities { get; set; }
    public int? UserId { get; set; }
    public string? Search { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
