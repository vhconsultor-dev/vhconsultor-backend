using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

/// <summary>
/// Repository for querying Corporate.AmazonMarketplaces with optional filters.
/// </summary>
public class AmazonMarketplaceQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public AmazonMarketplaceQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    /// <summary>
    /// Gets Amazon marketplaces with optional filters. All string filters use partial match (LIKE).
    /// </summary>
    /// <param name="id">Optional. Return only this marketplace by ID.</param>
    /// <param name="amazonMarketplaceCode">Optional. Partial match on AmazonMarketplaceCode.</param>
    /// <param name="countryCode">Optional. Partial match on CountryCode.</param>
    /// <param name="countryName">Optional. Partial match on CountryName.</param>
    /// <param name="amazonRegion">Optional. Partial match on AmazonRegion.</param>
    /// <param name="currencyCode">Optional. Partial match on CurrencyCode.</param>
    /// <param name="isActive">Optional. Filter by IsActive. When null, all are returned.</param>
    /// <returns>List of marketplaces matching the criteria, ordered by CountryName.</returns>
    public async Task<IEnumerable<AmazonMarketplace>> GetAmazonMarketplacesAsync(
        int? id = null,
        string? amazonMarketplaceCode = null,
        string? countryCode = null,
        string? countryName = null,
        string? amazonRegion = null,
        string? currencyCode = null,
        bool? isActive = null)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                AmazonMarketplaceId,
                AmazonMarketplaceCode,
                CountryCode,
                CountryName,
                AmazonRegion,
                CurrencyCode,
                IsActive
            FROM [Corporate].[AmazonMarketplaces]
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (id.HasValue)
        {
            sql += " AND AmazonMarketplaceId = @Id";
            parameters.Add("Id", id.Value);
        }

        if (!string.IsNullOrWhiteSpace(amazonMarketplaceCode))
        {
            sql += " AND AmazonMarketplaceCode LIKE @AmazonMarketplaceCode";
            parameters.Add("AmazonMarketplaceCode", "%" + amazonMarketplaceCode.Trim() + "%");
        }

        if (!string.IsNullOrWhiteSpace(countryCode))
        {
            sql += " AND CountryCode LIKE @CountryCode";
            parameters.Add("CountryCode", "%" + countryCode.Trim() + "%");
        }

        if (!string.IsNullOrWhiteSpace(countryName))
        {
            sql += " AND CountryName LIKE @CountryName";
            parameters.Add("CountryName", "%" + countryName.Trim() + "%");
        }

        if (!string.IsNullOrWhiteSpace(amazonRegion))
        {
            sql += " AND AmazonRegion LIKE @AmazonRegion";
            parameters.Add("AmazonRegion", "%" + amazonRegion.Trim() + "%");
        }

        if (!string.IsNullOrWhiteSpace(currencyCode))
        {
            sql += " AND CurrencyCode LIKE @CurrencyCode";
            parameters.Add("CurrencyCode", "%" + currencyCode.Trim() + "%");
        }

        if (isActive.HasValue)
        {
            sql += " AND IsActive = @IsActive";
            parameters.Add("IsActive", isActive.Value);
        }

        sql += " ORDER BY CountryName";

        var items = await connection.QueryAsync<AmazonMarketplace>(sql, parameters);
        return items.ToList();
    }

    /// <summary>
    /// Gets a single marketplace by ID.
    /// </summary>
    public async Task<AmazonMarketplace?> GetByIdAsync(int amazonMarketplaceId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                AmazonMarketplaceId,
                AmazonMarketplaceCode,
                CountryCode,
                CountryName,
                AmazonRegion,
                CurrencyCode,
                IsActive
            FROM [Corporate].[AmazonMarketplaces]
            WHERE AmazonMarketplaceId = @AmazonMarketplaceId";

        return await connection.QueryFirstOrDefaultAsync<AmazonMarketplace>(sql, new { AmazonMarketplaceId = amazonMarketplaceId });
    }
}
