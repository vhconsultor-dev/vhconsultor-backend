using Dapper;
using ModelLayer.Shared;
using ModelLayer.Shared.Entities;

namespace BusinessLayer.Shared.Commands;

public class ErrorLogCommandRepository : IErrorLogCommandRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public ErrorLogCommandRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<int> CreateAsync(ErrorLog errorLog)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync();
        
        var sql = @"
            INSERT INTO ErrorLogs (
                ErrorNumber, Message, StackTrace, Source, RequestPath, 
                RequestMethod, UserAgent, UserId, RequestBody, QueryString, 
                ExceptionType, CreatedAt, Environment, AdditionalData
            ) VALUES (
                @ErrorNumber, @Message, @StackTrace, @Source, @RequestPath,
                @RequestMethod, @UserAgent, @UserId, @RequestBody, @QueryString,
                @ExceptionType, @CreatedAt, @Environment, @AdditionalData
            );
            SELECT CAST(SCOPE_IDENTITY() as int);";

        return await connection.QuerySingleAsync<int>(sql, errorLog);
    }

    public async Task<bool> UpdateAsync(ErrorLog errorLog)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync();
        
        var sql = @"
            UPDATE ErrorLogs SET 
                ErrorNumber = @ErrorNumber,
                Message = @Message,
                StackTrace = @StackTrace,
                Source = @Source,
                RequestPath = @RequestPath,
                RequestMethod = @RequestMethod,
                UserAgent = @UserAgent,
                UserId = @UserId,
                RequestBody = @RequestBody,
                QueryString = @QueryString,
                ExceptionType = @ExceptionType,
                Environment = @Environment,
                AdditionalData = @AdditionalData
            WHERE Id = @Id";

        var rowsAffected = await connection.ExecuteAsync(sql, errorLog);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync();
        
        var sql = "DELETE FROM ErrorLogs WHERE Id = @Id";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }
} 