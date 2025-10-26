using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

/// <summary>
/// Repository para consultas de Currency usando Dapper
/// </summary>
public class CurrencyQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public CurrencyQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    /// <summary>
    /// Obtiene currencies con filtros opcionales
    /// </summary>
    /// <param name="currencyCode">Código de la moneda (opcional)</param>
    /// <param name="currencyName">Nombre de la moneda para búsqueda parcial (opcional)</param>
    /// <param name="currencySymbol">Símbolo de la moneda (opcional)</param>
    /// <param name="isActive">Filtrar por estado activo (opcional)</param>
    /// <returns>Lista de currencies</returns>
    public async Task<IEnumerable<Currency>> GetCurrenciesAsync(
        string? currencyCode = null,
        string? currencyName = null,
        string? currencySymbol = null,
        bool? isActive = true)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                CurrencyCode, CurrencyName, CurrencySymbol, IsActive, CreatedAt
            FROM [Corporate].[Currencies]
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(currencyCode))
        {
            sql += " AND CurrencyCode = @CurrencyCode";
            parameters.Add("CurrencyCode", currencyCode);
        }

        if (!string.IsNullOrEmpty(currencyName))
        {
            sql += " AND CurrencyName LIKE '%' + @CurrencyName + '%'";
            parameters.Add("CurrencyName", currencyName);
        }

        if (!string.IsNullOrEmpty(currencySymbol))
        {
            sql += " AND CurrencySymbol = @CurrencySymbol";
            parameters.Add("CurrencySymbol", currencySymbol);
        }

        if (isActive.HasValue)
        {
            sql += " AND IsActive = @IsActive";
            parameters.Add("IsActive", isActive.Value);
        }

        sql += " ORDER BY CurrencyName";

        return await connection.QueryAsync<Currency>(sql, parameters);
    }

    /// <summary>
    /// Obtiene una currency por su código
    /// </summary>
    /// <param name="currencyCode">Código de la moneda</param>
    /// <returns>Currency o null si no existe</returns>
    public async Task<Currency?> GetByCodeAsync(string currencyCode)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                CurrencyCode, CurrencyName, CurrencySymbol, IsActive, CreatedAt
            FROM [Corporate].[Currencies]
            WHERE CurrencyCode = @CurrencyCode";

        return await connection.QueryFirstOrDefaultAsync<Currency>(sql, new { CurrencyCode = currencyCode });
    }

    /// <summary>
    /// Obtiene todas las currencies activas
    /// </summary>
    /// <returns>Lista de currencies activas</returns>
    public async Task<IEnumerable<Currency>> GetAllActiveAsync()
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                CurrencyCode, CurrencyName, CurrencySymbol, IsActive, CreatedAt
            FROM [Corporate].[Currencies]
            WHERE IsActive = 1
            ORDER BY CurrencyName";

        return await connection.QueryAsync<Currency>(sql);
    }
}

