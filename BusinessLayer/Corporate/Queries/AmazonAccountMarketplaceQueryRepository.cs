using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

/// <summary>
/// Repository for querying Corporate.AmazonAccountMarketplaces with optional filters.
/// </summary>
public class AmazonAccountMarketplaceQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public AmazonAccountMarketplaceQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    /// <summary>
    /// Gets Amazon account–marketplace associations with optional filters. All filters use exact match.
    /// </summary>
    public async Task<IEnumerable<AmazonAccountMarketplace>> GetAmazonAccountMarketplacesAsync(
        int? id = null,
        int? amazonAccountId = null,
        int? amazonMarketplaceId = null,
        bool? isPrimary = null,
        bool? isActive = null)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                AmazonAccountMarketplaceId,
                AmazonAccountId,
                AmazonMarketplaceId,
                IsPrimary,
                IsActive,
                CreatedAt
            FROM [Corporate].[AmazonAccountMarketplaces] WITH (NOLOCK)
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (id.HasValue)
        {
            sql += " AND AmazonAccountMarketplaceId = @Id";
            parameters.Add("Id", id.Value);
        }

        if (amazonAccountId.HasValue)
        {
            sql += " AND AmazonAccountId = @AmazonAccountId";
            parameters.Add("AmazonAccountId", amazonAccountId.Value);
        }

        if (amazonMarketplaceId.HasValue)
        {
            sql += " AND AmazonMarketplaceId = @AmazonMarketplaceId";
            parameters.Add("AmazonMarketplaceId", amazonMarketplaceId.Value);
        }

        if (isPrimary.HasValue)
        {
            sql += " AND IsPrimary = @IsPrimary";
            parameters.Add("IsPrimary", isPrimary.Value);
        }

        if (isActive.HasValue)
        {
            sql += " AND IsActive = @IsActive";
            parameters.Add("IsActive", isActive.Value);
        }

        sql += " ORDER BY CreatedAt DESC";

        var items = await connection.QueryAsync<AmazonAccountMarketplace>(sql, parameters);
        return items.ToList();
    }

    /// <summary>
    /// Gets a single association by ID.
    /// </summary>
    public async Task<AmazonAccountMarketplace?> GetByIdAsync(int amazonAccountMarketplaceId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                AmazonAccountMarketplaceId, AmazonAccountId, AmazonMarketplaceId,
                IsPrimary, IsActive, CreatedAt
            FROM [Corporate].[AmazonAccountMarketplaces] WITH (NOLOCK)
            WHERE AmazonAccountMarketplaceId = @AmazonAccountMarketplaceId";

        return await connection.QueryFirstOrDefaultAsync<AmazonAccountMarketplace>(
            sql, new { AmazonAccountMarketplaceId = amazonAccountMarketplaceId });
    }
}
