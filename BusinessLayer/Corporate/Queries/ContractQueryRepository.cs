using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

/// <summary>
/// Repository para consultas de Contract usando Dapper
/// </summary>
public class ContractQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public ContractQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    /// <summary>
    /// Obtiene contracts con filtros opcionales
    /// </summary>
    public async Task<IEnumerable<Contract>> GetContractsAsync(
        string? contractId = null,
        int? customerId = null,
        string? contractNumber = null,
        string? status = null,
        int? contractTypeId = null,
        string? currencyCode = null,
        DateTime? startDateFrom = null,
        DateTime? startDateTo = null,
        DateTime? endDateFrom = null,
        DateTime? endDateTo = null)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                ContractId, CustomerId, ContractNumber, ClientLegalName, ClientTaxId,
                ClientNationality, ClientAddress, ClientPrimaryContact, ClientEmail, ClientPhone,
                ContractTypeId, ServiceDescription, FeeTypeId, FeeAmount, FeeDescription,
                CurrencyCode, ContractTerm, PaymentFrequency, PaymentDay, PaymentMethodId,
                SignedDate, EffectiveDate, StartDate, EndDate, AutoRenewal, RenewalTerm,
                RenewalNoticeDays, NoticePeriodDays, Status, GoverningLaw, DisputeResolution,
                ContractualDomicile, Jurisdiction, Notes, CreatedAt, UpdatedAt, LastModifiedBy,
                DocumentUrl, SignedDocumentUrl
            FROM [Corporate].[Contracts]
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(contractId))
        {
            sql += " AND ContractId = @ContractId";
            parameters.Add("ContractId", contractId);
        }

        if (customerId.HasValue)
        {
            sql += " AND CustomerId = @CustomerId";
            parameters.Add("CustomerId", customerId.Value);
        }

        if (!string.IsNullOrEmpty(contractNumber))
        {
            sql += " AND ContractNumber LIKE '%' + @ContractNumber + '%'";
            parameters.Add("ContractNumber", contractNumber);
        }

        if (!string.IsNullOrEmpty(status))
        {
            sql += " AND Status = @Status";
            parameters.Add("Status", status);
        }

        if (contractTypeId.HasValue)
        {
            sql += " AND ContractTypeId = @ContractTypeId";
            parameters.Add("ContractTypeId", contractTypeId.Value);
        }

        if (!string.IsNullOrEmpty(currencyCode))
        {
            sql += " AND CurrencyCode = @CurrencyCode";
            parameters.Add("CurrencyCode", currencyCode);
        }

        if (startDateFrom.HasValue)
        {
            sql += " AND StartDate >= @StartDateFrom";
            parameters.Add("StartDateFrom", startDateFrom.Value);
        }

        if (startDateTo.HasValue)
        {
            sql += " AND StartDate <= @StartDateTo";
            parameters.Add("StartDateTo", startDateTo.Value);
        }

        if (endDateFrom.HasValue)
        {
            sql += " AND EndDate >= @EndDateFrom";
            parameters.Add("EndDateFrom", endDateFrom.Value);
        }

        if (endDateTo.HasValue)
        {
            sql += " AND EndDate <= @EndDateTo";
            parameters.Add("EndDateTo", endDateTo.Value);
        }

        sql += " ORDER BY CreatedAt DESC";

        return await connection.QueryAsync<Contract>(sql, parameters);
    }

    /// <summary>
    /// Obtiene un contract por su ID
    /// </summary>
    public async Task<Contract?> GetByIdAsync(string contractId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                ContractId, CustomerId, ContractNumber, ClientLegalName, ClientTaxId,
                ClientNationality, ClientAddress, ClientPrimaryContact, ClientEmail, ClientPhone,
                ContractTypeId, ServiceDescription, FeeTypeId, FeeAmount, FeeDescription,
                CurrencyCode, ContractTerm, PaymentFrequency, PaymentDay, PaymentMethodId,
                SignedDate, EffectiveDate, StartDate, EndDate, AutoRenewal, RenewalTerm,
                RenewalNoticeDays, NoticePeriodDays, Status, GoverningLaw, DisputeResolution,
                ContractualDomicile, Jurisdiction, Notes, CreatedAt, UpdatedAt, LastModifiedBy,
                DocumentUrl, SignedDocumentUrl
            FROM [Corporate].[Contracts]
            WHERE ContractId = @ContractId";

        return await connection.QueryFirstOrDefaultAsync<Contract>(sql, new { ContractId = contractId });
    }

    /// <summary>
    /// Obtiene contracts por customer ID
    /// </summary>
    public async Task<IEnumerable<Contract>> GetByCustomerIdAsync(int customerId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                ContractId, CustomerId, ContractNumber, ClientLegalName, ClientTaxId,
                ClientNationality, ClientAddress, ClientPrimaryContact, ClientEmail, ClientPhone,
                ContractTypeId, ServiceDescription, FeeTypeId, FeeAmount, FeeDescription,
                CurrencyCode, ContractTerm, PaymentFrequency, PaymentDay, PaymentMethodId,
                SignedDate, EffectiveDate, StartDate, EndDate, AutoRenewal, RenewalTerm,
                RenewalNoticeDays, NoticePeriodDays, Status, GoverningLaw, DisputeResolution,
                ContractualDomicile, Jurisdiction, Notes, CreatedAt, UpdatedAt, LastModifiedBy,
                DocumentUrl, SignedDocumentUrl
            FROM [Corporate].[Contracts]
            WHERE CustomerId = @CustomerId
            ORDER BY CreatedAt DESC";

        return await connection.QueryAsync<Contract>(sql, new { CustomerId = customerId });
    }

    /// <summary>
    /// Obtiene contracts activos (con estado "Activo" o "Vigente")
    /// </summary>
    public async Task<IEnumerable<Contract>> GetActiveContractsAsync()
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                ContractId, CustomerId, ContractNumber, ClientLegalName, ClientTaxId,
                ClientNationality, ClientAddress, ClientPrimaryContact, ClientEmail, ClientPhone,
                ContractTypeId, ServiceDescription, FeeTypeId, FeeAmount, FeeDescription,
                CurrencyCode, ContractTerm, PaymentFrequency, PaymentDay, PaymentMethodId,
                SignedDate, EffectiveDate, StartDate, EndDate, AutoRenewal, RenewalTerm,
                RenewalNoticeDays, NoticePeriodDays, Status, GoverningLaw, DisputeResolution,
                ContractualDomicile, Jurisdiction, Notes, CreatedAt, UpdatedAt, LastModifiedBy,
                DocumentUrl, SignedDocumentUrl
            FROM [Corporate].[Contracts]
            WHERE Status IN ('Activo', 'Vigente')
            ORDER BY CreatedAt DESC";

        return await connection.QueryAsync<Contract>(sql);
    }
}

