using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Ecommerce.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

public class LeadQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public LeadQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    private static readonly string BaseSelect = @"
        SELECT
            SubmissionID, FirstName, LastName, Email, PhoneNumber, Country,
            BrandName, NumberOfListings, ProductPageLink, StoreLink,
            SelectedPlatform, AccountType, ServiceType,
            AnnualSalesRange, AdvertisingBudgetRange, PromotionalBudgetRange,
            AdditionalDetails, SubmissionDate, SubmissionType,
            IsRead, ReadAt, ReadByUserId, CreatedByUserId, Notes
        FROM [Ecommerce].[CustomerSubmissions]";

    public async Task<IEnumerable<CustomerSubmission>> GetAllAsync(LeadFilter? filter = null)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var where = new List<string>();
        var parameters = new DynamicParameters();

        if (filter != null)
        {
            if (filter.IsRead.HasValue)
            {
                where.Add("IsRead = @IsRead");
                parameters.Add("IsRead", filter.IsRead.Value ? 1 : 0);
            }
            if (!string.IsNullOrWhiteSpace(filter.SubmissionType))
            {
                where.Add("SubmissionType = @SubmissionType");
                parameters.Add("SubmissionType", filter.SubmissionType);
            }
            if (!string.IsNullOrWhiteSpace(filter.Platform))
            {
                where.Add("SelectedPlatform = @Platform");
                parameters.Add("Platform", filter.Platform);
            }
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                where.Add("(FirstName LIKE @Search OR LastName LIKE @Search OR Email LIKE @Search OR BrandName LIKE @Search)");
                parameters.Add("Search", $"%{filter.Search}%");
            }
            if (filter.FromDate.HasValue)
            {
                where.Add("SubmissionDate >= @FromDate");
                parameters.Add("FromDate", filter.FromDate.Value);
            }
            if (filter.ToDate.HasValue)
            {
                where.Add("SubmissionDate <= @ToDate");
                parameters.Add("ToDate", filter.ToDate.Value);
            }
        }

        var whereClause = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "";
        var sql = BaseSelect + whereClause + " ORDER BY SubmissionDate DESC";

        return await connection.QueryAsync<CustomerSubmission>(sql, parameters);
    }

    public async Task<CustomerSubmission?> GetByIdAsync(int submissionId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT
                SubmissionID, FirstName, LastName, Email, PhoneNumber, Country,
                BrandName, NumberOfListings, ProductPageLink, StoreLink,
                SelectedPlatform, AccountType, ServiceType,
                AnnualSalesRange, AdvertisingBudgetRange, PromotionalBudgetRange,
                AdditionalDetails, SubmissionDate, SubmissionType,
                IsRead, ReadAt, ReadByUserId, CreatedByUserId, Notes
            FROM [Ecommerce].[CustomerSubmissions]
            WHERE SubmissionID = @SubmissionId";

        return await connection.QuerySingleOrDefaultAsync<CustomerSubmission>(sql, new { SubmissionId = submissionId });
    }

    public async Task<int> GetUnreadCountAsync()
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = "SELECT COUNT(1) FROM [Ecommerce].[CustomerSubmissions] WHERE IsRead = 0";
        return await connection.ExecuteScalarAsync<int>(sql);
    }
}

public class LeadFilter
{
    public bool? IsRead { get; set; }
    public string? SubmissionType { get; set; }
    public string? Platform { get; set; }
    public string? Search { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
