using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

public class ManualInvoiceQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public ManualInvoiceQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<IEnumerable<ManualInvoiceDto>> GetAsync(ManualInvoiceFilters filters)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT
                h.ManualInvoiceHeaderId,
                h.CustomerId,
                c.CompanyName AS CustomerName,
                h.InvoiceNumber,
                h.InvoiceDate,
                h.DueDate,
                h.CurrencyCode,
                h.ExchangeRate,
                h.SubTotal,
                h.DiscountAmount,
                h.TaxRate,
                h.TaxAmount,
                h.Total,
                h.Status,
                h.PaymentStatus,
                h.PaidDate,
                h.PaymentMethodId,
                h.PaymentReference,
                h.DepositNumber,
                h.TransferNumber,
                h.BillingPeriodFrom,
                h.BillingPeriodTo,
                h.BillingName,
                h.BillingEmail,
                h.Notes,
                h.ClientNotes,
                h.Lang,
                h.SentAt,
                h.CreatedBy,
                h.CreatedAt,
                h.UpdatedAt,
                -- Detail columns
                d.ManualInvoiceDetailId,
                d.ServiceDescription,
                d.UnitLabel,
                d.Quantity,
                d.UnitPrice,
                d.DiscountPercent,
                d.DiscountAmount  AS DetailDiscountAmount,
                d.LineTotal,
                d.TaxApplicable,
                d.Notes           AS DetailNotes,
                d.DisplayOrder
            FROM [Corporate].[ManualInvoiceHeaders] h
            INNER JOIN [Corporate].[Customers] c ON c.CustomerId = h.CustomerId
            LEFT JOIN  [Corporate].[ManualInvoiceDetails] d ON d.ManualInvoiceHeaderId = h.ManualInvoiceHeaderId
            WHERE h.CustomerId = @CustomerId";

        var parameters = new DynamicParameters();
        parameters.Add("CustomerId", filters.CustomerId);

        if (filters.ManualInvoiceHeaderId.HasValue)
        {
            sql += " AND h.ManualInvoiceHeaderId = @ManualInvoiceHeaderId";
            parameters.Add("ManualInvoiceHeaderId", filters.ManualInvoiceHeaderId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.InvoiceNumber))
        {
            sql += " AND h.InvoiceNumber LIKE @InvoiceNumber";
            parameters.Add("InvoiceNumber", $"%{filters.InvoiceNumber}%");
        }

        if (!string.IsNullOrWhiteSpace(filters.Status))
        {
            sql += " AND h.Status = @Status";
            parameters.Add("Status", filters.Status);
        }

        if (!string.IsNullOrWhiteSpace(filters.PaymentStatus))
        {
            sql += " AND h.PaymentStatus = @PaymentStatus";
            parameters.Add("PaymentStatus", filters.PaymentStatus);
        }

        if (!string.IsNullOrWhiteSpace(filters.CurrencyCode))
        {
            sql += " AND h.CurrencyCode = @CurrencyCode";
            parameters.Add("CurrencyCode", filters.CurrencyCode.ToUpper());
        }

        if (filters.InvoiceDateFrom.HasValue)
        {
            sql += " AND h.InvoiceDate >= @InvoiceDateFrom";
            parameters.Add("InvoiceDateFrom", filters.InvoiceDateFrom.Value);
        }

        if (filters.InvoiceDateTo.HasValue)
        {
            sql += " AND h.InvoiceDate <= @InvoiceDateTo";
            parameters.Add("InvoiceDateTo", filters.InvoiceDateTo.Value);
        }

        if (filters.DueDateFrom.HasValue)
        {
            sql += " AND h.DueDate >= @DueDateFrom";
            parameters.Add("DueDateFrom", filters.DueDateFrom.Value);
        }

        if (filters.DueDateTo.HasValue)
        {
            sql += " AND h.DueDate <= @DueDateTo";
            parameters.Add("DueDateTo", filters.DueDateTo.Value);
        }

        if (filters.IsOverdue == true)
        {
            sql += " AND h.PaymentStatus = 'Unpaid' AND h.DueDate < CAST(GETDATE() AS DATE)";
        }

        sql += " ORDER BY h.InvoiceDate DESC, h.ManualInvoiceHeaderId DESC, d.DisplayOrder ASC";

        // Use multi-mapping to build headers with their details
        var headerDict = new Dictionary<int, ManualInvoiceDto>();

        await connection.QueryAsync<ManualInvoiceDto, ManualInvoiceDetailDto?, ManualInvoiceDto>(
            sql,
            (header, detail) =>
            {
                if (!headerDict.TryGetValue(header.ManualInvoiceHeaderId, out var existing))
                {
                    existing = header;
                    existing.Details = new List<ManualInvoiceDetailDto>();
                    headerDict[header.ManualInvoiceHeaderId] = existing;
                }

                if (detail != null && detail.ManualInvoiceDetailId > 0)
                    existing.Details.Add(detail);

                return existing;
            },
            parameters,
            splitOn: "ManualInvoiceDetailId"
        );

        var allHeaders = headerDict.Values.ToList();

        // Pagination applied in memory (after distinct headers are assembled)
        int pageNumber = filters.PageNumber <= 0 ? 1 : filters.PageNumber;
        int pageSize = filters.PageSize <= 0 ? 50 : filters.PageSize;

        return allHeaders
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
    }
}

public class ManualInvoiceFilters
{
    public int CustomerId { get; set; }          // Required
    public int? ManualInvoiceHeaderId { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? Status { get; set; }
    public string? PaymentStatus { get; set; }
    public string? CurrencyCode { get; set; }
    public DateTime? InvoiceDateFrom { get; set; }
    public DateTime? InvoiceDateTo { get; set; }
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }
    public bool? IsOverdue { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class ManualInvoiceDto
{
    public int ManualInvoiceHeaderId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? PaidDate { get; set; }
    public int? PaymentMethodId { get; set; }
    public string? PaymentReference { get; set; }
    public string? DepositNumber { get; set; }
    public string? TransferNumber { get; set; }
    public DateTime? BillingPeriodFrom { get; set; }
    public DateTime? BillingPeriodTo { get; set; }
    public string? BillingName { get; set; }
    public string? BillingEmail { get; set; }
    public string? Notes { get; set; }
    public string? ClientNotes { get; set; }
    public string Lang { get; set; } = "es";
    public DateTime? SentAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ManualInvoiceDetailDto> Details { get; set; } = new();
}

public class ManualInvoiceDetailDto
{
    public int ManualInvoiceDetailId { get; set; }
    public string ServiceDescription { get; set; } = string.Empty;
    public string? UnitLabel { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DetailDiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
    public bool TaxApplicable { get; set; }
    public string? DetailNotes { get; set; }
    public int DisplayOrder { get; set; }
}
