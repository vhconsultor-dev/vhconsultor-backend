using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Ecommerce.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

public class LeadQueryRepository
{
    private const string BrandKeyExpression = "LOWER(LTRIM(RTRIM(ISNULL(BrandName, ''))))";

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

        var (whereClause, parameters) = BuildWhereClause(filter);
        var orderBy = BuildLeadOrderBy(filter?.SortField, filter?.SortOrder);
        var sql = BaseSelect + whereClause + orderBy;

        return await connection.QueryAsync<CustomerSubmission>(sql, parameters);
    }

    public async Task<LeadBrandGroupsQueryResult> GetBrandGroupsAsync(LeadFilter? filter = null)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var pageNumber = filter?.PageNumber is > 0 ? filter.PageNumber.Value : 1;
        var pageSize = filter?.PageSize is > 0 ? filter.PageSize.Value : 15;
        if (pageSize > 100)
            pageSize = 100;

        var (whereClause, parameters) = BuildWhereClause(filter, "cs");
        var brandKeyExpr = BrandKeyExpression.Replace("BrandName", "cs.BrandName");
        var orderBy = BuildBrandGroupOrderBy(filter?.SortField, filter?.SortOrder);

        var groupedFrom = $@"
            FROM [Ecommerce].[CustomerSubmissions] cs
            {whereClause}
            GROUP BY {brandKeyExpr}";

        var countSql = $@"
            SELECT COUNT(1)
            FROM (
                SELECT 1 AS G
                {groupedFrom}
            ) g";

        var totalLeadsSql = $@"
            SELECT COUNT(1)
            FROM [Ecommerce].[CustomerSubmissions] cs
            {whereClause}";

        var dataSql = $@"
            SELECT
                {brandKeyExpr} AS BrandKey,
                MAX(cs.BrandName) AS BrandName,
                COUNT(1) AS LeadCount,
                SUM(CASE WHEN cs.IsRead = 0 THEN 1 ELSE 0 END) AS UnreadCount,
                MAX(cs.SubmissionDate) AS LatestSubmissionDate
            {groupedFrom}
            {orderBy}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        parameters.Add("Offset", (pageNumber - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        var totalRecords = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var totalLeads = await connection.ExecuteScalarAsync<int>(totalLeadsSql, parameters);
        var items = (await connection.QueryAsync<LeadBrandGroupSummary>(dataSql, parameters)).ToList();

        return new LeadBrandGroupsQueryResult
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalLeads = totalLeads,
        };
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

    private static (string WhereClause, DynamicParameters Parameters) BuildWhereClause(
        LeadFilter? filter,
        string? tableAlias = null)
    {
        var prefix = string.IsNullOrEmpty(tableAlias) ? "" : $"{tableAlias}.";
        var brandKeyExpr = string.IsNullOrEmpty(tableAlias)
            ? BrandKeyExpression
            : BrandKeyExpression.Replace("BrandName", $"{tableAlias}.BrandName");

        var where = new List<string>();
        var parameters = new DynamicParameters();

        if (filter != null)
        {
            if (filter.IsRead.HasValue)
            {
                where.Add($"{prefix}IsRead = @IsRead");
                parameters.Add("IsRead", filter.IsRead.Value ? 1 : 0);
            }
            if (!string.IsNullOrWhiteSpace(filter.SubmissionType))
            {
                where.Add($"{prefix}SubmissionType = @SubmissionType");
                parameters.Add("SubmissionType", filter.SubmissionType);
            }
            if (!string.IsNullOrWhiteSpace(filter.Platform))
            {
                where.Add($"{prefix}SelectedPlatform = @Platform");
                parameters.Add("Platform", filter.Platform);
            }
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                where.Add(
                    $"({prefix}FirstName LIKE @Search OR {prefix}LastName LIKE @Search OR {prefix}Email LIKE @Search OR {prefix}BrandName LIKE @Search)");
                parameters.Add("Search", $"%{filter.Search}%");
            }
            if (filter.FromDate.HasValue)
            {
                where.Add($"{prefix}SubmissionDate >= @FromDate");
                parameters.Add("FromDate", filter.FromDate.Value);
            }
            if (filter.ToDate.HasValue)
            {
                where.Add($"{prefix}SubmissionDate <= @ToDate");
                parameters.Add("ToDate", filter.ToDate.Value);
            }
            if (filter.BrandName != null)
            {
                where.Add($"{brandKeyExpr} = @BrandKey");
                parameters.Add("BrandKey", NormalizeBrandKey(filter.BrandName));
            }
        }

        var whereClause = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "";
        return (whereClause, parameters);
    }

    private static string NormalizeBrandKey(string? brandName) =>
        string.IsNullOrWhiteSpace(brandName) ? "" : brandName.Trim().ToLowerInvariant();

    private static string BuildLeadOrderBy(string? sortField, string? sortOrder)
    {
        var desc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        var column = sortField?.ToLowerInvariant() switch
        {
            "firstname" => "FirstName",
            "email" => "Email",
            "selectedplatform" => "SelectedPlatform",
            "brandname" => "BrandName",
            "submissiondate" => "SubmissionDate",
            _ => "SubmissionDate",
        };
        return $" ORDER BY {column} {(desc ? "DESC" : "ASC")}, SubmissionID ASC";
    }

    private static string BuildBrandGroupOrderBy(string? sortField, string? sortOrder)
    {
        var desc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        var column = sortField?.ToLowerInvariant() switch
        {
            "leadcount" => "LeadCount",
            "latestsubmissiondate" => "LatestSubmissionDate",
            "unreadcount" => "UnreadCount",
            "firstname" or "email" or "submissiondate" or "selectedplatform" => "LatestSubmissionDate",
            _ => "BrandName",
        };
        return $" ORDER BY {column} {(desc ? "DESC" : "ASC")}, BrandKey ASC";
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
    public string? BrandName { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? SortField { get; set; }
    public string? SortOrder { get; set; }
}

public class LeadBrandGroupSummary
{
    public string BrandKey { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public int LeadCount { get; set; }
    public int UnreadCount { get; set; }
    public DateTime? LatestSubmissionDate { get; set; }
}

public class LeadBrandGroupsQueryResult
{
    public List<LeadBrandGroupSummary> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalLeads { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalRecords / PageSize) : 0;
}
