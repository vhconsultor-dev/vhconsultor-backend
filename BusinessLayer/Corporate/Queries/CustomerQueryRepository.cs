using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

/// <summary>
/// Repository para consultas de Customer usando Dapper
/// </summary>
public class CustomerQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public CustomerQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    /// <summary>
    /// Obtiene todos los customers activos
    /// </summary>
    /// <returns>Lista de customers</returns>
    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                CustomerId, CompanyName, NIT, CompanyType, 
                PrimaryEmail, SecondaryEmail, BillingEmail,
                PrimaryPhone, SecondaryPhone, EmergencyPhone,
                Contact1Name, Contact1Phone, Contact2Name, Contact2Phone,
                Contact3Name, Contact3Phone, CountryId, State, City,
                Address, PostalCode, SectorId, CompanySize, AnnualRevenue,
                Website, ClientStatus, Priority, Source, Notes,
                IsActive, CreatedAt, UpdatedAt, LastContactDate
            FROM [Corporate].[Customers]
            WHERE IsActive = 1
            ORDER BY CompanyName";

        return await connection.QueryAsync<Customer>(sql);
    }

    /// <summary>
    /// Obtiene un customer por su ID
    /// </summary>
    /// <param name="customerId">ID del customer</param>
    /// <returns>Customer o null si no existe</returns>
    public async Task<Customer?> GetByIdAsync(int customerId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                CustomerId, CompanyName, NIT, CompanyType, 
                PrimaryEmail, SecondaryEmail, BillingEmail,
                PrimaryPhone, SecondaryPhone, EmergencyPhone,
                Contact1Name, Contact1Phone, Contact2Name, Contact2Phone,
                Contact3Name, Contact3Phone, CountryId, State, City,
                Address, PostalCode, SectorId, CompanySize, AnnualRevenue,
                Website, ClientStatus, Priority, Source, Notes,
                IsActive, CreatedAt, UpdatedAt, LastContactDate
            FROM [Corporate].[Customers]
            WHERE CustomerId = @CustomerId";

        return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { CustomerId = customerId });
    }

    /// <summary>
    /// Obtiene un customer por su NIT
    /// </summary>
    /// <param name="nit">NIT del customer</param>
    /// <returns>Customer o null si no existe</returns>
    public async Task<Customer?> GetByNITAsync(string nit)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                CustomerId, CompanyName, NIT, CompanyType, 
                PrimaryEmail, SecondaryEmail, BillingEmail,
                PrimaryPhone, SecondaryPhone, EmergencyPhone,
                Contact1Name, Contact1Phone, Contact2Name, Contact2Phone,
                Contact3Name, Contact3Phone, CountryId, State, City,
                Address, PostalCode, SectorId, CompanySize, AnnualRevenue,
                Website, ClientStatus, Priority, Source, Notes,
                IsActive, CreatedAt, UpdatedAt, LastContactDate
            FROM [Corporate].[Customers]
            WHERE NIT = @NIT AND IsActive = 1";

        return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { NIT = nit });
    }

    /// <summary>
    /// Busca customers por nombre de compañía (búsqueda parcial)
    /// </summary>
    /// <param name="companyName">Nombre o parte del nombre de la compañía</param>
    /// <returns>Lista de customers que coinciden</returns>
    public async Task<IEnumerable<Customer>> SearchByCompanyNameAsync(string companyName)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                CustomerId, CompanyName, NIT, CompanyType, 
                PrimaryEmail, SecondaryEmail, BillingEmail,
                PrimaryPhone, SecondaryPhone, EmergencyPhone,
                Contact1Name, Contact1Phone, Contact2Name, Contact2Phone,
                Contact3Name, Contact3Phone, CountryId, State, City,
                Address, PostalCode, SectorId, CompanySize, AnnualRevenue,
                Website, ClientStatus, Priority, Source, Notes,
                IsActive, CreatedAt, UpdatedAt, LastContactDate
            FROM [Corporate].[Customers]
            WHERE CompanyName LIKE '%' + @CompanyName + '%' AND IsActive = 1
            ORDER BY CompanyName";

        return await connection.QueryAsync<Customer>(sql, new { CompanyName = companyName });
    }

    /// <summary>
    /// Obtiene customers filtrados por múltiples criterios
    /// </summary>
    /// <param name="filter">Filtros de búsqueda</param>
    /// <returns>Lista de customers que coinciden con los filtros</returns>
    public async Task<IEnumerable<Customer>> GetFilteredAsync(CustomerFilter filter)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                CustomerId, CompanyName, NIT, CompanyType, 
                PrimaryEmail, SecondaryEmail, BillingEmail,
                PrimaryPhone, SecondaryPhone, EmergencyPhone,
                Contact1Name, Contact1Phone, Contact2Name, Contact2Phone,
                Contact3Name, Contact3Phone, CountryId, State, City,
                Address, PostalCode, SectorId, CompanySize, AnnualRevenue,
                Website, ClientStatus, Priority, Source, Notes,
                IsActive, CreatedAt, UpdatedAt, LastContactDate
            FROM [Corporate].[Customers]
            WHERE IsActive = 1";

        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(filter.CompanyName))
        {
            sql += " AND CompanyName LIKE '%' + @CompanyName + '%'";
            parameters.Add("CompanyName", filter.CompanyName);
        }

        if (!string.IsNullOrEmpty(filter.NIT))
        {
            sql += " AND NIT = @NIT";
            parameters.Add("NIT", filter.NIT);
        }

        if (!string.IsNullOrEmpty(filter.ClientStatus))
        {
            sql += " AND ClientStatus = @ClientStatus";
            parameters.Add("ClientStatus", filter.ClientStatus);
        }

        if (!string.IsNullOrEmpty(filter.Priority))
        {
            sql += " AND Priority = @Priority";
            parameters.Add("Priority", filter.Priority);
        }

        if (filter.CountryId.HasValue)
        {
            sql += " AND CountryId = @CountryId";
            parameters.Add("CountryId", filter.CountryId.Value);
        }

        if (filter.SectorId.HasValue)
        {
            sql += " AND SectorId = @SectorId";
            parameters.Add("SectorId", filter.SectorId.Value);
        }

        if (!string.IsNullOrEmpty(filter.City))
        {
            sql += " AND City = @City";
            parameters.Add("City", filter.City);
        }

        sql += " ORDER BY CompanyName";

        return await connection.QueryAsync<Customer>(sql, parameters);
    }

    /// <summary>
    /// Obtiene customers por estado de cliente
    /// </summary>
    /// <param name="clientStatus">Estado del cliente</param>
    /// <returns>Lista de customers con ese estado</returns>
    public async Task<IEnumerable<Customer>> GetByClientStatusAsync(string clientStatus)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                CustomerId, CompanyName, NIT, CompanyType, 
                PrimaryEmail, SecondaryEmail, BillingEmail,
                PrimaryPhone, SecondaryPhone, EmergencyPhone,
                Contact1Name, Contact1Phone, Contact2Name, Contact2Phone,
                Contact3Name, Contact3Phone, CountryId, State, City,
                Address, PostalCode, SectorId, CompanySize, AnnualRevenue,
                Website, ClientStatus, Priority, Source, Notes,
                IsActive, CreatedAt, UpdatedAt, LastContactDate
            FROM [Corporate].[Customers]
            WHERE ClientStatus = @ClientStatus AND IsActive = 1
            ORDER BY CompanyName";

        return await connection.QueryAsync<Customer>(sql, new { ClientStatus = clientStatus });
    }

    /// <summary>
    /// Obtiene customers por prioridad
    /// </summary>
    /// <param name="priority">Prioridad</param>
    /// <returns>Lista de customers con esa prioridad</returns>
    public async Task<IEnumerable<Customer>> GetByPriorityAsync(string priority)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                CustomerId, CompanyName, NIT, CompanyType, 
                PrimaryEmail, SecondaryEmail, BillingEmail,
                PrimaryPhone, SecondaryPhone, EmergencyPhone,
                Contact1Name, Contact1Phone, Contact2Name, Contact2Phone,
                Contact3Name, Contact3Phone, CountryId, State, City,
                Address, PostalCode, SectorId, CompanySize, AnnualRevenue,
                Website, ClientStatus, Priority, Source, Notes,
                IsActive, CreatedAt, UpdatedAt, LastContactDate
            FROM [Corporate].[Customers]
            WHERE Priority = @Priority AND IsActive = 1
            ORDER BY CompanyName";

        return await connection.QueryAsync<Customer>(sql, new { Priority = priority });
    }
}

/// <summary>
/// Clase para filtros de búsqueda de customers
/// </summary>
public class CustomerFilter
{
    public string? CompanyName { get; set; }
    public string? NIT { get; set; }
    public string? ClientStatus { get; set; }
    public string? Priority { get; set; }
    public int? CountryId { get; set; }
    public int? SectorId { get; set; }
    public string? City { get; set; }
}

