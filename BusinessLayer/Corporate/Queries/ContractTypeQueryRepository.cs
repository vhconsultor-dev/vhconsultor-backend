using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

/// <summary>
/// Repository para consultas de ContractType usando Dapper
/// </summary>
public class ContractTypeQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public ContractTypeQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    /// <summary>
    /// Obtiene contract types con filtros opcionales
    /// </summary>
    /// <param name="id">ID del contract type (opcional)</param>
    /// <param name="typeName">Nombre del tipo para búsqueda parcial (opcional)</param>
    /// <param name="isActive">Filtrar por estado activo (opcional)</param>
    /// <returns>Lista de contract types</returns>
    public async Task<IEnumerable<ContractType>> GetContractTypesAsync(
        int? id = null,
        string? typeName = null,
        bool? isActive = true)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                ContractTypeId, TypeName, TypeDescription, IsActive, CreatedAt
            FROM [Corporate].[ContractTypes]
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (id.HasValue)
        {
            sql += " AND ContractTypeId = @Id";
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

        return await connection.QueryAsync<ContractType>(sql, parameters);
    }

    /// <summary>
    /// Obtiene un contract type por su ID
    /// </summary>
    /// <param name="contractTypeId">ID del contract type</param>
    /// <returns>ContractType o null si no existe</returns>
    public async Task<ContractType?> GetByIdAsync(int contractTypeId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                ContractTypeId, TypeName, TypeDescription, IsActive, CreatedAt
            FROM [Corporate].[ContractTypes]
            WHERE ContractTypeId = @ContractTypeId";

        return await connection.QueryFirstOrDefaultAsync<ContractType>(sql, new { ContractTypeId = contractTypeId });
    }

    /// <summary>
    /// Obtiene todos los contract types activos
    /// </summary>
    /// <returns>Lista de contract types activos</returns>
    public async Task<IEnumerable<ContractType>> GetAllActiveAsync()
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                ContractTypeId, TypeName, TypeDescription, IsActive, CreatedAt
            FROM [Corporate].[ContractTypes]
            WHERE IsActive = 1
            ORDER BY TypeName";

        return await connection.QueryAsync<ContractType>(sql);
    }
}

