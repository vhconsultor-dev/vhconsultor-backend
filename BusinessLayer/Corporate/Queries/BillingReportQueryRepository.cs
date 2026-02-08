using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

/// <summary>
/// Repository for billing reporting queries. Uses NOLOCK for read-only reporting to avoid blocking.
/// </summary>
public class BillingReportQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public BillingReportQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    /// <summary>
    /// Gets all invoices pending payment (Unpaid), with optional filters.
    /// Joins Invoices, Contracts, Customers, FeeTypes. Multiple filters are combined with AND.
    /// </summary>
    /// <param name="filter">Optional: customer tax id (LIKE), customer name (LIKE), fee type (1=Fixed, 2=Percentage)</param>
    /// <returns>List of pending invoice report rows ordered by due date ascending</returns>
    public async Task<IEnumerable<PendingInvoiceReportDto>> GetPendingInvoicesAsync(PendingInvoicesReportFilter filter)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                c.CustomerId,
                c.NIT AS TaxId,
                c.CompanyName AS CustomerName,
                ct.ContractNumber,
                i.InvoiceId,
                i.InvoiceNumber,
                i.DueDate,
                i.Total AS Amount,
                i.CurrencyCode,
                ct.FeeTypeId,
                ft.TypeName AS FeeTypeName
            FROM [Corporate].[Invoices] i WITH (NOLOCK)
            INNER JOIN [Corporate].[Contracts] ct WITH (NOLOCK) ON ct.ContractId = i.ContractId
            INNER JOIN [Corporate].[Customers] c WITH (NOLOCK) ON c.CustomerId = ct.CustomerId
            LEFT JOIN [Corporate].[FeeTypes] ft WITH (NOLOCK) ON ft.FeeTypeId = ct.FeeTypeId
            WHERE i.PaymentStatus = 'Unpaid'
            AND (i.Status IS NULL OR i.Status <> 'Cancelled')";

        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(filter.CustomerTaxId))
        {
            sql += " AND c.NIT LIKE @CustomerTaxId";
            parameters.Add("CustomerTaxId", "%" + filter.CustomerTaxId.Trim() + "%");
        }

        if (!string.IsNullOrWhiteSpace(filter.CustomerName))
        {
            sql += " AND c.CompanyName LIKE @CustomerName";
            parameters.Add("CustomerName", "%" + filter.CustomerName.Trim() + "%");
        }

        if (filter.FeeTypeId.HasValue)
        {
            sql += " AND ct.FeeTypeId = @FeeTypeId";
            parameters.Add("FeeTypeId", filter.FeeTypeId.Value);
        }

        sql += " ORDER BY i.DueDate ASC";

        var items = await connection.QueryAsync<PendingInvoiceReportDto>(sql, parameters);
        return items.ToList();
    }
}

/// <summary>
/// Filter for pending invoices report. All properties optional; when multiple are set, conditions are ANDed.
/// </summary>
public class PendingInvoicesReportFilter
{
    /// <summary>Customer tax id (NIT / cédula). Partial match (LIKE).</summary>
    public string? CustomerTaxId { get; set; }

    /// <summary>Customer name (CompanyName). Partial match (LIKE).</summary>
    public string? CustomerName { get; set; }

    /// <summary>1 = Fixed amount, 2 = Percentage. When null, all fee types are returned.</summary>
    public int? FeeTypeId { get; set; }
}

/// <summary>
/// One row of the pending invoices billing report.
/// </summary>
public class PendingInvoiceReportDto
{
    public int CustomerId { get; set; }
    public string TaxId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ContractNumber { get; set; } = string.Empty;
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int FeeTypeId { get; set; }
    public string FeeTypeName { get; set; } = string.Empty;
}
