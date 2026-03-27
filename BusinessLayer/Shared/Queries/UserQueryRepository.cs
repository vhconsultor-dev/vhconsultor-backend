using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Shared;
using ModelLayer.Shared.Entities;

namespace BusinessLayer.Shared.Queries;

/// <summary>
/// Repository para consultas de User usando Dapper
/// </summary>
public class UserQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public UserQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    /// <summary>
    /// Obtiene un usuario por su ID
    /// </summary>
    public async Task<User?> GetByIdAsync(int userId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                UserId, FirstName, LastName, Email, Username, PasswordHash,
                PhoneNumber, ProfilePictureUrl, IsCorporate, IsBrandPartner, IsSuperAdmin,
                IsActive, EmailVerified, FailedLoginAttempts, LockedUntil,
                LastLogin, LastLoginIP, LastLoginLocation, LastLoginCountry,
                LastLoginCity, LastLoginUserAgent, CreatedAt, UpdatedAt,
                CreatedBy, LastModifiedBy
            FROM [Global].[Users]
            WHERE UserId = @UserId";

        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { UserId = userId });
    }

    /// <summary>
    /// Obtiene un usuario por username o email
    /// </summary>
    public async Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                UserId, FirstName, LastName, Email, Username, PasswordHash,
                PhoneNumber, ProfilePictureUrl, IsCorporate, IsBrandPartner, IsSuperAdmin,
                IsActive, EmailVerified, FailedLoginAttempts, LockedUntil,
                LastLogin, LastLoginIP, LastLoginLocation, LastLoginCountry,
                LastLoginCity, LastLoginUserAgent, CreatedAt, UpdatedAt,
                CreatedBy, LastModifiedBy
            FROM [Global].[Users]
            WHERE (Username = @UsernameOrEmail OR Email = @UsernameOrEmail)";

        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { UsernameOrEmail = usernameOrEmail });
    }

    /// <summary>
    /// Obtiene usuarios con filtros opcionales
    /// </summary>
    public async Task<IEnumerable<User>> GetUsersAsync(
        int? userId = null,
        string? email = null,
        string? username = null,
        bool? isActive = null,
        bool? isCorporate = null,
        bool? isBrandPartner = null,
        bool? emailVerified = null)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                UserId, FirstName, LastName, Email, Username, PasswordHash,
                PhoneNumber, ProfilePictureUrl, IsCorporate, IsBrandPartner, IsSuperAdmin,
                IsActive, EmailVerified, FailedLoginAttempts, LockedUntil,
                LastLogin, LastLoginIP, LastLoginLocation, LastLoginCountry,
                LastLoginCity, LastLoginUserAgent, CreatedAt, UpdatedAt,
                CreatedBy, LastModifiedBy
            FROM [Global].[Users]
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (userId.HasValue)
        {
            sql += " AND UserId = @UserId";
            parameters.Add("UserId", userId.Value);
        }

        if (!string.IsNullOrEmpty(email))
        {
            sql += " AND Email LIKE '%' + @Email + '%'";
            parameters.Add("Email", email);
        }

        if (!string.IsNullOrEmpty(username))
        {
            sql += " AND Username LIKE '%' + @Username + '%'";
            parameters.Add("Username", username);
        }

        if (isActive.HasValue)
        {
            sql += " AND IsActive = @IsActive";
            parameters.Add("IsActive", isActive.Value);
        }

        if (isCorporate.HasValue)
        {
            sql += " AND IsCorporate = @IsCorporate";
            parameters.Add("IsCorporate", isCorporate.Value);
        }

        if (isBrandPartner.HasValue)
        {
            sql += " AND IsBrandPartner = @IsBrandPartner";
            parameters.Add("IsBrandPartner", isBrandPartner.Value);
        }

        if (emailVerified.HasValue)
        {
            sql += " AND EmailVerified = @EmailVerified";
            parameters.Add("EmailVerified", emailVerified.Value);
        }

        sql += " ORDER BY CreatedAt DESC";

        return await connection.QueryAsync<User>(sql, parameters);
    }

    /// <summary>
    /// Verifica si una cuenta está bloqueada
    /// </summary>
    public async Task<bool> IsAccountLockedAsync(int userId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                CASE 
                    WHEN LockedUntil IS NOT NULL AND LockedUntil > GETDATE() THEN 1
                    ELSE 0
                END AS IsLocked
            FROM [Global].[Users]
            WHERE UserId = @UserId";

        var result = await connection.QueryFirstOrDefaultAsync<int>(sql, new { UserId = userId });
        return result == 1;
    }
}

