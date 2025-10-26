using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

/// <summary>
/// Repository para consultas de FeeType usando Dapper
/// </summary>
public class FeeTypeQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public FeeTypeQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    /// <summary>
    /// Obtiene fee types con filtros opcionales
    /// </summary>
    /// <param name="id">ID del fee type (opcional)</param>
    /// <param name="typeName">Nombre del tipo para búsqueda parcial (opcional)</param>
    /// <param name="isActive">Filtrar por estado activo (opcional)</param>
    /// <returns>Lista de fee types</returns>
    public async Task<IEnumerable<FeeType>> GetFeeTypesAsync(
        int? id = null,
        string? typeName = null,
        bool? isActive = true)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                FeeTypeId, TypeName, TypeDescription, IsActive, CreatedAt
            FROM [Corporate].[FeeTypes]
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (id.HasValue)
        {
            sql += " AND FeeTypeId = @Id";
            parameters.Add("Id", id.Value);
        }

        if (!string.IsNullOrEmpty(typeName))
        {
            sql += " AND TypeName LIKE '%' + @TypeName + '%'";
            parameters.Add("TypeName", typeName);
        }

        if (isActive.HasValue)
        {
            sql += " AND IsActive = @IsActive";
            parameters.Add("IsActive", isActive.Value);
        }

        sql += " ORDER BY TypeName";

        return await connection.QueryAsync<FeeType>(sql, parameters);
    }

    /// <summary>
    /// Obtiene un fee type por su ID
    /// </summary>
    /// <param name="feeTypeId">ID del fee type</param>
    /// <returns>FeeType o null si no existe</returns>
    public async Task<FeeType?> GetByIdAsync(int feeTypeId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                FeeTypeId, TypeName, TypeDescription, IsActive, CreatedAt
            FROM [Corporate].[FeeTypes]
            WHERE FeeTypeId = @FeeTypeId";

        return await connection.QueryFirstOrDefaultAsync<FeeType>(sql, new { FeeTypeId = feeTypeId });
    }

    /// <summary>
    /// Obtiene todos los fee types activos
    /// </summary>
    /// <returns>Lista de fee types activos</returns>
    public async Task<IEnumerable<FeeType>> GetAllActiveAsync()
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                FeeTypeId, TypeName, TypeDescription, IsActive, CreatedAt
            FROM [Corporate].[FeeTypes]
            WHERE IsActive = 1
            ORDER BY TypeName";

        return await connection.QueryAsync<FeeType>(sql);
    }
}

