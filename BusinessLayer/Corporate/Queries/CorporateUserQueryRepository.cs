using Dapper;
using Microsoft.Data.SqlClient;
using BusinessLayer.Corporate.Models;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

public class CorporateUserQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public CorporateUserQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    /// <summary>
    /// Lista usuarios corporate para autocomplete de @mentions (estilo Instagram).
    /// </summary>
    public async Task<IEnumerable<CorporateUserSummaryDto>> GetMentionableUsersAsync(
        string? search = null,
        bool? isActive = true,
        int limit = 20)
    {
        limit = Math.Clamp(limit, 1, 50);

        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT TOP (@Limit)
                UserId,
                FirstName,
                LastName,
                LTRIM(RTRIM(FirstName + ' ' + LastName)) AS FullName,
                Email,
                ProfilePictureUrl,
                IsActive
            FROM [Global].[Users]
            WHERE IsCorporate = 1";

        var parameters = new DynamicParameters();
        parameters.Add("Limit", limit);

        if (isActive.HasValue)
        {
            sql += " AND IsActive = @IsActive";
            parameters.Add("IsActive", isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            sql += @" AND (
                FirstName LIKE '%' + @Search + '%'
                OR LastName LIKE '%' + @Search + '%'
                OR Email LIKE '%' + @Search + '%'
                OR Username LIKE '%' + @Search + '%'
                OR CAST(UserId AS NVARCHAR(20)) = @SearchExact
            )";
            parameters.Add("Search", search.Trim());
            parameters.Add("SearchExact", search.Trim());
        }

        sql += " ORDER BY FirstName, LastName";

        return await connection.QueryAsync<CorporateUserSummaryDto>(sql, parameters);
    }

    public async Task<CorporateUserSummaryDto?> GetCorporateUserByIdAsync(int userId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT
                UserId,
                FirstName,
                LastName,
                LTRIM(RTRIM(FirstName + ' ' + LastName)) AS FullName,
                Email,
                ProfilePictureUrl,
                IsActive
            FROM [Global].[Users]
            WHERE UserId = @UserId AND IsCorporate = 1";

        return await connection.QuerySingleOrDefaultAsync<CorporateUserSummaryDto>(sql, new { UserId = userId });
    }
}
