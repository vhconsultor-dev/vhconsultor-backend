using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

/// <summary>
/// Repository para consultas de Service usando Dapper
/// </summary>
public class ServiceQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public ServiceQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    /// <summary>
    /// Obtiene services con filtros opcionales
    /// </summary>
    /// <param name="id">ID del service (opcional)</param>
    /// <param name="serviceCode">Código del servicio (opcional)</param>
    /// <param name="serviceName">Nombre del servicio para búsqueda parcial (opcional)</param>
    /// <param name="billingUnit">Unidad de facturación (opcional)</param>
    /// <param name="isActive">Filtrar por estado activo (opcional)</param>
    /// <returns>Lista de services</returns>
    public async Task<IEnumerable<Service>> GetServicesAsync(
        int? id = null,
        string? serviceCode = null,
        string? serviceName = null,
        string? billingUnit = null,
        bool? isActive = true)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                ServiceId, ServiceCode, ServiceName, ServiceDescription, 
                DefaultUnitPrice, BillingUnit, IsActive, CreatedAt, UpdatedAt
            FROM [Corporate].[Services]
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (id.HasValue)
        {
            sql += " AND ServiceId = @Id";
            parameters.Add("Id", id.Value);
        }

        if (!string.IsNullOrEmpty(serviceCode))
        {
            sql += " AND ServiceCode = @ServiceCode";
            parameters.Add("ServiceCode", serviceCode);
        }

        if (!string.IsNullOrEmpty(serviceName))
        {
            sql += " AND ServiceName LIKE '%' + @ServiceName + '%'";
            parameters.Add("ServiceName", serviceName);
        }

        if (!string.IsNullOrEmpty(billingUnit))
        {
            sql += " AND BillingUnit = @BillingUnit";
            parameters.Add("BillingUnit", billingUnit);
        }

        if (isActive.HasValue)
        {
            sql += " AND IsActive = @IsActive";
            parameters.Add("IsActive", isActive.Value);
        }

        sql += " ORDER BY ServiceName";

        return await connection.QueryAsync<Service>(sql, parameters);
    }

    /// <summary>
    /// Obtiene un service por su ID
    /// </summary>
    /// <param name="serviceId">ID del service</param>
    /// <returns>Service o null si no existe</returns>
    public async Task<Service?> GetByIdAsync(int serviceId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                ServiceId, ServiceCode, ServiceName, ServiceDescription, 
                DefaultUnitPrice, BillingUnit, IsActive, CreatedAt, UpdatedAt
            FROM [Corporate].[Services]
            WHERE ServiceId = @ServiceId";

        return await connection.QueryFirstOrDefaultAsync<Service>(sql, new { ServiceId = serviceId });
    }

    /// <summary>
    /// Obtiene un service por su código
    /// </summary>
    /// <param name="serviceCode">Código del service</param>
    /// <returns>Service o null si no existe</returns>
    public async Task<Service?> GetByCodeAsync(string serviceCode)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                ServiceId, ServiceCode, ServiceName, ServiceDescription, 
                DefaultUnitPrice, BillingUnit, IsActive, CreatedAt, UpdatedAt
            FROM [Corporate].[Services]
            WHERE ServiceCode = @ServiceCode";

        return await connection.QueryFirstOrDefaultAsync<Service>(sql, new { ServiceCode = serviceCode });
    }

    /// <summary>
    /// Obtiene todos los services activos
    /// </summary>
    /// <returns>Lista de services activos</returns>
    public async Task<IEnumerable<Service>> GetAllActiveAsync()
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                ServiceId, ServiceCode, ServiceName, ServiceDescription, 
                DefaultUnitPrice, BillingUnit, IsActive, CreatedAt, UpdatedAt
            FROM [Corporate].[Services]
            WHERE IsActive = 1
            ORDER BY ServiceName";

        return await connection.QueryAsync<Service>(sql);
    }
}

