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
    /// Gets invoices with optional filters for billing reports.
    /// Supports multiple scenarios: all, pending (unpaid), overdue (unpaid past due date), paid, by status, etc.
    /// Joins Invoices, Contracts, Customers, FeeTypes. Multiple filters are combined with AND.
    /// </summary>
    /// <param name="filter">Filters: invoice status type, customer tax id (LIKE), customer name (LIKE), fee type</param>
    /// <returns>List of invoice report rows ordered by due date or paid date depending on status type</returns>
    public async Task<IEnumerable<InvoiceReportDto>> GetInvoicesAsync(InvoiceReportFilter filter)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                c.CustomerId,
                c.NIT AS TaxId,
                c.CompanyName AS CustomerName,
                ct.ContractId,
                ct.ContractNumber,
                i.InvoiceId,
                i.InvoiceNumber,
                i.InvoiceDate,
                i.DueDate,
                i.SubTotal,
                i.Tax,
                i.Total AS Amount,
                i.CurrencyCode,
                i.Status,
                i.PaymentStatus,
                i.PaidDate,
                ct.FeeTypeId,
                ft.TypeName AS FeeTypeName
            FROM [Corporate].[Invoices] i WITH (NOLOCK)
            INNER JOIN [Corporate].[Contracts] ct WITH (NOLOCK) ON ct.ContractId = i.ContractId
            INNER JOIN [Corporate].[Customers] c WITH (NOLOCK) ON c.CustomerId = ct.CustomerId
            LEFT JOIN [Corporate].[FeeTypes] ft WITH (NOLOCK) ON ft.FeeTypeId = ct.FeeTypeId
            WHERE 1=1";

        var parameters = new DynamicParameters();

        // Filter by invoice status type (pending, overdue, paid, cancelled, draft, sent)
        if (!string.IsNullOrWhiteSpace(filter.InvoiceStatusType))
        {
            var statusType = filter.InvoiceStatusType.Trim().ToLowerInvariant();
            
            switch (statusType)
            {
                case "pending":
                    // Unpaid and not cancelled
                    sql += " AND i.PaymentStatus = 'Unpaid' AND (i.Status IS NULL OR i.Status <> 'Cancelled')";
                    break;
                
                case "overdue":
                    // Unpaid, not cancelled, and past due date
                    sql += " AND i.PaymentStatus = 'Unpaid' AND (i.Status IS NULL OR i.Status <> 'Cancelled') AND i.DueDate < GETDATE()";
                    break;
                
                case "paid":
                    // Payment status is Paid
                    sql += " AND i.PaymentStatus = 'Paid'";
                    break;
                
                case "cancelled":
                    // Status is Cancelled
                    sql += " AND i.Status = 'Cancelled'";
                    break;
                
                case "draft":
                    // Status is Draft
                    sql += " AND i.Status = 'Draft'";
                    break;
                
                case "sent":
                    // Status is Sent
                    sql += " AND i.Status = 'Sent'";
                    break;
                
                case "all":
                    // No additional filter - return all invoices
                    break;
                
                default:
                    throw new ArgumentException(
                        $"Invalid invoice status type: '{filter.InvoiceStatusType}'. " +
                        "Valid values: 'all', 'pending' (unpaid), 'overdue' (unpaid and past due date), 'paid', 'cancelled', 'draft', 'sent'");
            }
        }

        // Filter by customer tax id (partial match)
        if (!string.IsNullOrWhiteSpace(filter.CustomerTaxId))
        {
            sql += " AND c.NIT LIKE @CustomerTaxId";
            parameters.Add("CustomerTaxId", "%" + filter.CustomerTaxId.Trim() + "%");
        }

        // Filter by customer name (partial match)
        if (!string.IsNullOrWhiteSpace(filter.CustomerName))
        {
            sql += " AND c.CompanyName LIKE @CustomerName";
            parameters.Add("CustomerName", "%" + filter.CustomerName.Trim() + "%");
        }

        // Filter by fee type (1 = Fixed, 2 = Percentage)
        if (filter.FeeTypeId.HasValue)
        {
            if (filter.FeeTypeId.Value != 1 && filter.FeeTypeId.Value != 2)
            {
                throw new ArgumentException(
                    $"Invalid fee type ID: {filter.FeeTypeId}. Valid values: 1 (Fixed amount), 2 (Percentage)");
            }
            
            sql += " AND ct.FeeTypeId = @FeeTypeId";
            parameters.Add("FeeTypeId", filter.FeeTypeId.Value);
        }

        // Filter by contract number (partial match)
        if (!string.IsNullOrWhiteSpace(filter.ContractNumber))
        {
            sql += " AND ct.ContractNumber LIKE @ContractNumber";
            parameters.Add("ContractNumber", "%" + filter.ContractNumber.Trim() + "%");
        }

        // Filter by invoice number (partial match)
        if (!string.IsNullOrWhiteSpace(filter.InvoiceNumber))
        {
            sql += " AND i.InvoiceNumber LIKE @InvoiceNumber";
            parameters.Add("InvoiceNumber", "%" + filter.InvoiceNumber.Trim() + "%");
        }

        // Ordering based on status type
        var statusTypeLower = filter.InvoiceStatusType?.Trim().ToLowerInvariant();
        sql += statusTypeLower switch
        {
            "overdue" => " ORDER BY i.DueDate ASC", // Most urgent first
            "paid" => " ORDER BY i.PaidDate DESC", // Most recent paid first
            "pending" => " ORDER BY i.DueDate ASC", // Soonest due first
            _ => " ORDER BY i.InvoiceDate DESC" // Default: most recent invoices first
        };

        var items = await connection.QueryAsync<InvoiceReportDto>(sql, parameters);
        return items.ToList();
    }
}

/// <summary>
/// Filter for invoice billing report. All properties optional; when multiple are set, conditions are ANDed.
/// </summary>
public class InvoiceReportFilter
{
    /// <summary>
    /// Invoice status type filter. Valid values: 
    /// 'all' (all invoices),
    /// 'pending' (unpaid and not cancelled), 
    /// 'overdue' (unpaid, not cancelled, and past due date), 
    /// 'paid', 
    /// 'cancelled', 
    /// 'draft', 
    /// 'sent'.
    /// When null or empty, returns all invoices.
    /// </summary>
    public string? InvoiceStatusType { get; set; }

    /// <summary>Customer tax id (NIT / cédula). Partial match (LIKE).</summary>
    public string? CustomerTaxId { get; set; }

    /// <summary>Customer name (CompanyName). Partial match (LIKE).</summary>
    public string? CustomerName { get; set; }

    /// <summary>1 = Fixed amount, 2 = Percentage. When null, all fee types are returned.</summary>
    public int? FeeTypeId { get; set; }

    /// <summary>Contract number. Partial match (LIKE).</summary>
    public string? ContractNumber { get; set; }

    /// <summary>Invoice number. Partial match (LIKE).</summary>
    public string? InvoiceNumber { get; set; }
}

/// <summary>
/// One row of the invoice billing report.
/// </summary>
public class InvoiceReportDto
{
    public int CustomerId { get; set; }
    public string TaxId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? PaidDate { get; set; }
    public int FeeTypeId { get; set; }
    public string FeeTypeName { get; set; } = string.Empty;
}
