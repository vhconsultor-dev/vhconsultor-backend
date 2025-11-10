using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Shared;
using ModelLayer.Shared.Entities;

namespace BusinessLayer.Shared.Queries;

/// <summary>
/// Repository para consultas de UserLoginHistory usando Dapper
/// </summary>
public class UserLoginHistoryQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public UserLoginHistoryQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    /// <summary>
    /// Obtiene el historial de login con filtros opcionales
    /// </summary>
    public async Task<IEnumerable<UserLoginHistory>> GetLoginHistoryAsync(
        int? userId = null,
        DateTime? loginDateFrom = null,
        DateTime? loginDateTo = null,
        string? ipAddress = null,
        string? country = null,
        string? city = null,
        bool? loginSuccessful = null,
        int? limit = 100)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT TOP (@Limit)
                LoginHistoryId, UserId, LoginDate, IPAddress, Location,
                Country, City, Region, UserAgent, DeviceType, Browser,
                OperatingSystem, LoginSuccessful, FailureReason, SessionId
            FROM [Global].[UserLoginHistory]
            WHERE 1=1";

        var parameters = new DynamicParameters();
        parameters.Add("Limit", limit ?? 100);

        if (userId.HasValue)
        {
            sql += " AND UserId = @UserId";
            parameters.Add("UserId", userId.Value);
        }

        if (loginDateFrom.HasValue)
        {
            sql += " AND LoginDate >= @LoginDateFrom";
            parameters.Add("LoginDateFrom", loginDateFrom.Value);
        }

        if (loginDateTo.HasValue)
        {
            sql += " AND LoginDate <= @LoginDateTo";
            parameters.Add("LoginDateTo", loginDateTo.Value);
        }

        if (!string.IsNullOrEmpty(ipAddress))
        {
            sql += " AND IPAddress = @IPAddress";
            parameters.Add("IPAddress", ipAddress);
        }

        if (!string.IsNullOrEmpty(country))
        {
            sql += " AND Country LIKE '%' + @Country + '%'";
            parameters.Add("Country", country);
        }

        if (!string.IsNullOrEmpty(city))
        {
            sql += " AND City LIKE '%' + @City + '%'";
            parameters.Add("City", city);
        }

        if (loginSuccessful.HasValue)
        {
            sql += " AND LoginSuccessful = @LoginSuccessful";
            parameters.Add("LoginSuccessful", loginSuccessful.Value);
        }

        sql += " ORDER BY LoginDate DESC";

        return await connection.QueryAsync<UserLoginHistory>(sql, parameters);
    }

    /// <summary>
    /// Obtiene el último login exitoso de un usuario
    /// </summary>
    public async Task<UserLoginHistory?> GetLastSuccessfulLoginAsync(int userId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT TOP 1
                LoginHistoryId, UserId, LoginDate, IPAddress, Location,
                Country, City, Region, UserAgent, DeviceType, Browser,
                OperatingSystem, LoginSuccessful, FailureReason, SessionId
            FROM [Global].[UserLoginHistory]
            WHERE UserId = @UserId AND LoginSuccessful = 1
            ORDER BY LoginDate DESC";

        return await connection.QueryFirstOrDefaultAsync<UserLoginHistory>(sql, new { UserId = userId });
    }

    /// <summary>
    /// Obtiene estadísticas de intentos fallidos recientes
    /// </summary>
    public async Task<int> GetRecentFailedAttemptsAsync(int userId, int minutesWindow = 30)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT COUNT(*)
            FROM [Global].[UserLoginHistory]
            WHERE UserId = @UserId 
                AND LoginSuccessful = 0
                AND LoginDate >= DATEADD(MINUTE, -@MinutesWindow, GETDATE())";

        return await connection.QueryFirstOrDefaultAsync<int>(sql, new { UserId = userId, MinutesWindow = minutesWindow });
    }
}

