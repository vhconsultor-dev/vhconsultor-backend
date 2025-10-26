using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

/// <summary>
/// Repository para consultas de Country usando Dapper
/// </summary>
public class CountryQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public CountryQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    /// <summary>
    /// Obtiene countries con filtros opcionales
    /// </summary>
    /// <param name="id">ID del country (opcional)</param>
    /// <param name="countryCode">Código del país (opcional)</param>
    /// <param name="countryName">Nombre del país para búsqueda parcial (opcional)</param>
    /// <param name="phoneCode">Código telefónico (opcional)</param>
    /// <param name="isActive">Filtrar por estado activo (opcional, por defecto true)</param>
    /// <returns>Lista de countries que coinciden con los filtros</returns>
    public async Task<IEnumerable<Country>> GetCountriesAsync(
        int? id = null,
        string? countryCode = null,
        string? countryName = null,
        string? phoneCode = null,
        bool? isActive = true)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                CountryId, CountryCode, CountryName, 
                PhoneCode, IsActive, CreatedAt
            FROM [Corporate].[Countries]
            WHERE 1=1";

        var parameters = new DynamicParameters();

        // Si se especifica ID, devolver solo ese country
        if (id.HasValue)
        {
            sql += " AND CountryId = @Id";
            parameters.Add("Id", id.Value);
        }

        // Si se especifica CountryCode
        if (!string.IsNullOrEmpty(countryCode))
        {
            sql += " AND CountryCode = @CountryCode";
            parameters.Add("CountryCode", countryCode);
        }

        // Si se especifica CountryName (búsqueda parcial)
        if (!string.IsNullOrEmpty(countryName))
        {
            sql += " AND CountryName LIKE '%' + @CountryName + '%'";
            parameters.Add("CountryName", countryName);
        }

        // Si se especifica PhoneCode
        if (!string.IsNullOrEmpty(phoneCode))
        {
            sql += " AND PhoneCode = @PhoneCode";
            parameters.Add("PhoneCode", phoneCode);
        }

        // Filtrar por IsActive si se especifica
        if (isActive.HasValue)
        {
            sql += " AND IsActive = @IsActive";
            parameters.Add("IsActive", isActive.Value);
        }

        sql += " ORDER BY CountryName";

        return await connection.QueryAsync<Country>(sql, parameters);
    }

    /// <summary>
    /// Obtiene un country por su ID
    /// </summary>
    /// <param name="countryId">ID del country</param>
    /// <returns>Country o null si no existe</returns>
    public async Task<Country?> GetByIdAsync(int countryId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                CountryId, CountryCode, CountryName, 
                PhoneCode, IsActive, CreatedAt
            FROM [Corporate].[Countries]
            WHERE CountryId = @CountryId";

        return await connection.QueryFirstOrDefaultAsync<Country>(sql, new { CountryId = countryId });
    }

    /// <summary>
    /// Obtiene un country por su código
    /// </summary>
    /// <param name="countryCode">Código del país (ej: CRI, USA)</param>
    /// <returns>Country o null si no existe</returns>
    public async Task<Country?> GetByCodeAsync(string countryCode)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                CountryId, CountryCode, CountryName, 
                PhoneCode, IsActive, CreatedAt
            FROM [Corporate].[Countries]
            WHERE CountryCode = @CountryCode AND IsActive = 1";

        return await connection.QueryFirstOrDefaultAsync<Country>(sql, new { CountryCode = countryCode });
    }

    /// <summary>
    /// Obtiene todos los countries activos
    /// </summary>
    /// <returns>Lista de countries activos</returns>
    public async Task<IEnumerable<Country>> GetAllActiveAsync()
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                CountryId, CountryCode, CountryName, 
                PhoneCode, IsActive, CreatedAt
            FROM [Corporate].[Countries]
            WHERE IsActive = 1
            ORDER BY CountryName";

        return await connection.QueryAsync<Country>(sql);
    }
}

