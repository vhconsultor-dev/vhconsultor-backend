using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

/// <summary>
/// Repository para consultas de IndustrySector usando Dapper
/// </summary>
public class IndustrySectorQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public IndustrySectorQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    /// <summary>
    /// Obtiene industry sectors con filtros opcionales
    /// </summary>
    /// <param name="id">ID del sector (opcional)</param>
    /// <param name="sectorName">Nombre del sector para búsqueda parcial (opcional)</param>
    /// <param name="isActive">Filtrar por estado activo (opcional, por defecto true)</param>
    /// <returns>Lista de industry sectors que coinciden con los filtros</returns>
    public async Task<IEnumerable<IndustrySector>> GetIndustrySectorsAsync(
        int? id = null,
        string? sectorName = null,
        bool? isActive = true)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                SectorId, SectorName, Description, 
                IsActive, CreatedAt
            FROM [Corporate].[IndustrySectors]
            WHERE 1=1";

        var parameters = new DynamicParameters();

        // Si se especifica ID, devolver solo ese sector
        if (id.HasValue)
        {
            sql += " AND SectorId = @Id";
            parameters.Add("Id", id.Value);
        }

        // Si se especifica SectorName (búsqueda parcial)
        if (!string.IsNullOrEmpty(sectorName))
        {
            sql += " AND SectorName LIKE '%' + @SectorName + '%'";
            parameters.Add("SectorName", sectorName);
        }

        // Filtrar por IsActive si se especifica
        if (isActive.HasValue)
        {
            sql += " AND IsActive = @IsActive";
            parameters.Add("IsActive", isActive.Value);
        }

        sql += " ORDER BY SectorName";

        return await connection.QueryAsync<IndustrySector>(sql, parameters);
    }

    /// <summary>
    /// Obtiene un industry sector por su ID
    /// </summary>
    /// <param name="sectorId">ID del sector</param>
    /// <returns>IndustrySector o null si no existe</returns>
    public async Task<IndustrySector?> GetByIdAsync(int sectorId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                SectorId, SectorName, Description, 
                IsActive, CreatedAt
            FROM [Corporate].[IndustrySectors]
            WHERE SectorId = @SectorId";

        return await connection.QueryFirstOrDefaultAsync<IndustrySector>(sql, new { SectorId = sectorId });
    }

    /// <summary>
    /// Obtiene todos los industry sectors activos
    /// </summary>
    /// <returns>Lista de industry sectors activos</returns>
    public async Task<IEnumerable<IndustrySector>> GetAllActiveAsync()
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                SectorId, SectorName, Description, 
                IsActive, CreatedAt
            FROM [Corporate].[IndustrySectors]
            WHERE IsActive = 1
            ORDER BY SectorName";

        return await connection.QueryAsync<IndustrySector>(sql);
    }
}

