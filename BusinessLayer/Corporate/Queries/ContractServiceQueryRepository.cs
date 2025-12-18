using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

/// <summary>
/// Repository para consultas de ContractService usando Dapper
/// </summary>
public class ContractServiceQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public ContractServiceQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    /// <summary>
    /// Obtiene contract services con filtros opcionales
    /// </summary>
    public async Task<IEnumerable<ContractService>> GetContractServicesAsync(
        int? contractServiceId = null,
        int? contractId = null,
        int? serviceId = null,
        bool? isActive = true)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                ContractServiceId, ContractId, ServiceId, ServiceDescription, Regions,
                UnitPrice, Quantity, DiscountPercentage, FinalPrice, IsActive,
                ServiceOrder, BillingFrequency, CreatedAt, UpdatedAt
            FROM [Corporate].[ContractServices]
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (contractServiceId.HasValue)
        {
            sql += " AND ContractServiceId = @ContractServiceId";
            parameters.Add("ContractServiceId", contractServiceId.Value);
        }

        if (contractId.HasValue)
        {
            sql += " AND ContractId = @ContractId";
            parameters.Add("ContractId", contractId.Value);
        }

        if (serviceId.HasValue)
        {
            sql += " AND ServiceId = @ServiceId";
            parameters.Add("ServiceId", serviceId.Value);
        }

        if (isActive.HasValue)
        {
            sql += " AND IsActive = @IsActive";
            parameters.Add("IsActive", isActive.Value);
        }

        sql += " ORDER BY ServiceOrder, ContractServiceId";

        return await connection.QueryAsync<ContractService>(sql, parameters);
    }

    /// <summary>
    /// Obtiene un contract service por su ID
    /// </summary>
    public async Task<ContractService?> GetByIdAsync(int contractServiceId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                ContractServiceId, ContractId, ServiceId, ServiceDescription, Regions,
                UnitPrice, Quantity, DiscountPercentage, FinalPrice, IsActive,
                ServiceOrder, BillingFrequency, CreatedAt, UpdatedAt
            FROM [Corporate].[ContractServices]
            WHERE ContractServiceId = @ContractServiceId";

        return await connection.QueryFirstOrDefaultAsync<ContractService>(sql, new { ContractServiceId = contractServiceId });
    }

    /// <summary>
    /// Obtiene contract services por contract ID
    /// </summary>
    public async Task<IEnumerable<ContractService>> GetByContractIdAsync(int contractId, bool? isActive = true)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                ContractServiceId, ContractId, ServiceId, ServiceDescription, Regions,
                UnitPrice, Quantity, DiscountPercentage, FinalPrice, IsActive,
                ServiceOrder, BillingFrequency, CreatedAt, UpdatedAt
            FROM [Corporate].[ContractServices]
            WHERE ContractId = @ContractId";

        var parameters = new DynamicParameters();
        parameters.Add("ContractId", contractId);

        if (isActive.HasValue)
        {
            sql += " AND IsActive = @IsActive";
            parameters.Add("IsActive", isActive.Value);
        }

        sql += " ORDER BY ServiceOrder, ContractServiceId";

        return await connection.QueryAsync<ContractService>(sql, parameters);
    }

    /// <summary>
    /// Obtiene todos los contract services activos
    /// </summary>
    public async Task<IEnumerable<ContractService>> GetAllActiveAsync()
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                ContractServiceId, ContractId, ServiceId, ServiceDescription, Regions,
                UnitPrice, Quantity, DiscountPercentage, FinalPrice, IsActive,
                ServiceOrder, BillingFrequency, CreatedAt, UpdatedAt
            FROM [Corporate].[ContractServices]
            WHERE IsActive = 1
            ORDER BY ServiceOrder, ContractServiceId";

        return await connection.QueryAsync<ContractService>(sql);
    }

    /// <summary>
    /// Calcula el total de servicios de un contrato
    /// </summary>
    public async Task<decimal> GetContractTotalAsync(int contractId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT ISNULL(SUM(FinalPrice), 0)
            FROM [Corporate].[ContractServices]
            WHERE ContractId = @ContractId AND IsActive = 1";

        return await connection.QuerySingleAsync<decimal>(sql, new { ContractId = contractId });
    }
}

