using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

/// <summary>
/// Repository para consultas de PaymentMethod usando Dapper
/// </summary>
public class PaymentMethodQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public PaymentMethodQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    /// <summary>
    /// Obtiene payment methods con filtros opcionales
    /// </summary>
    /// <param name="id">ID del payment method (opcional)</param>
    /// <param name="methodName">Nombre del método para búsqueda parcial (opcional)</param>
    /// <param name="isActive">Filtrar por estado activo (opcional)</param>
    /// <returns>Lista de payment methods</returns>
    public async Task<IEnumerable<PaymentMethod>> GetPaymentMethodsAsync(
        int? id = null,
        string? methodName = null,
        bool? isActive = true)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                PaymentMethodId, MethodName, MethodDescription, IsActive, CreatedAt
            FROM [Corporate].[PaymentMethods]
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (id.HasValue)
        {
            sql += " AND PaymentMethodId = @Id";
            parameters.Add("Id", id.Value);
        }

        if (!string.IsNullOrEmpty(methodName))
        {
            sql += " AND MethodName LIKE '%' + @MethodName + '%'";
            parameters.Add("MethodName", methodName);
        }

        if (isActive.HasValue)
        {
            sql += " AND IsActive = @IsActive";
            parameters.Add("IsActive", isActive.Value);
        }

        sql += " ORDER BY MethodName";

        return await connection.QueryAsync<PaymentMethod>(sql, parameters);
    }

    /// <summary>
    /// Obtiene un payment method por su ID
    /// </summary>
    /// <param name="paymentMethodId">ID del payment method</param>
    /// <returns>PaymentMethod o null si no existe</returns>
    public async Task<PaymentMethod?> GetByIdAsync(int paymentMethodId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                PaymentMethodId, MethodName, MethodDescription, IsActive, CreatedAt
            FROM [Corporate].[PaymentMethods]
            WHERE PaymentMethodId = @PaymentMethodId";

        return await connection.QueryFirstOrDefaultAsync<PaymentMethod>(sql, new { PaymentMethodId = paymentMethodId });
    }

    /// <summary>
    /// Obtiene todos los payment methods activos
    /// </summary>
    /// <returns>Lista de payment methods activos</returns>
    public async Task<IEnumerable<PaymentMethod>> GetAllActiveAsync()
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                PaymentMethodId, MethodName, MethodDescription, IsActive, CreatedAt
            FROM [Corporate].[PaymentMethods]
            WHERE IsActive = 1
            ORDER BY MethodName";

        return await connection.QueryAsync<PaymentMethod>(sql);
    }
}
