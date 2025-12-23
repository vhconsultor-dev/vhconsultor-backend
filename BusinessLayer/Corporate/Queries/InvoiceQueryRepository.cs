using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Queries;

/// <summary>
/// Repository para consultas de Invoice usando Dapper
/// </summary>
public class InvoiceQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public InvoiceQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    /// <summary>
    /// Obtiene facturas con filtros opcionales - Query unificado
    /// </summary>
    public async Task<InvoiceQueryResult> GetInvoicesAsync(InvoiceQueryFilter filter)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                i.InvoiceId, i.ContractId, i.InvoiceNumber, i.InvoiceDate, i.DueDate,
                i.SubTotal, i.Tax, i.Total, i.CurrencyCode, i.Status, i.PaymentStatus,
                i.PaidDate, i.PaidBy, i.PaymentMethodId, i.PaymentReference,
                i.DepositNumber, i.TransferNumber, i.Notes, i.CreatedAt, i.UpdatedAt,
                i.LastModifiedBy
            FROM [Corporate].[Invoices] i
            WHERE 1=1";

        var parameters = new DynamicParameters();

        // Filtro por ID de factura
        if (filter.InvoiceId.HasValue)
        {
            sql += " AND i.InvoiceId = @InvoiceId";
            parameters.Add("InvoiceId", filter.InvoiceId.Value);
        }

        // Filtro por ID de contrato
        if (filter.ContractId.HasValue)
        {
            sql += " AND i.ContractId = @ContractId";
            parameters.Add("ContractId", filter.ContractId.Value);
        }

        // Filtro por número de factura
        if (!string.IsNullOrEmpty(filter.InvoiceNumber))
        {
            sql += " AND i.InvoiceNumber LIKE @InvoiceNumber";
            parameters.Add("InvoiceNumber", $"%{filter.InvoiceNumber}%");
        }

        // Filtro por estado
        if (!string.IsNullOrEmpty(filter.Status))
        {
            sql += " AND i.Status = @Status";
            parameters.Add("Status", filter.Status);
        }

        // Filtro por estado de pago
        if (!string.IsNullOrEmpty(filter.PaymentStatus))
        {
            sql += " AND i.PaymentStatus = @PaymentStatus";
            parameters.Add("PaymentStatus", filter.PaymentStatus);
        }

        // Filtro por moneda
        if (!string.IsNullOrEmpty(filter.CurrencyCode))
        {
            sql += " AND i.CurrencyCode = @CurrencyCode";
            parameters.Add("CurrencyCode", filter.CurrencyCode);
        }

        // Filtro por rango de fechas de emisión
        if (filter.InvoiceDateFrom.HasValue)
        {
            sql += " AND i.InvoiceDate >= @InvoiceDateFrom";
            parameters.Add("InvoiceDateFrom", filter.InvoiceDateFrom.Value);
        }

        if (filter.InvoiceDateTo.HasValue)
        {
            sql += " AND i.InvoiceDate <= @InvoiceDateTo";
            parameters.Add("InvoiceDateTo", filter.InvoiceDateTo.Value);
        }

        // Filtro por rango de fechas de vencimiento
        if (filter.DueDateFrom.HasValue)
        {
            sql += " AND i.DueDate >= @DueDateFrom";
            parameters.Add("DueDateFrom", filter.DueDateFrom.Value);
        }

        if (filter.DueDateTo.HasValue)
        {
            sql += " AND i.DueDate <= @DueDateTo";
            parameters.Add("DueDateTo", filter.DueDateTo.Value);
        }

        // Filtro por facturas vencidas
        if (filter.IsOverdue.HasValue && filter.IsOverdue.Value)
        {
            sql += " AND i.DueDate < GETDATE() AND i.PaymentStatus = 'Unpaid'";
        }

        // Ordenamiento
        sql += filter.OrderBy switch
        {
            "InvoiceDate" => " ORDER BY i.InvoiceDate DESC",
            "DueDate" => " ORDER BY i.DueDate ASC",
            "Total" => " ORDER BY i.Total DESC",
            "Status" => " ORDER BY i.Status, i.InvoiceDate DESC",
            _ => " ORDER BY i.CreatedAt DESC"
        };

        // Paginación
        if (filter.PageSize.HasValue && filter.PageNumber.HasValue)
        {
            int offset = (filter.PageNumber.Value - 1) * filter.PageSize.Value;
            sql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            parameters.Add("Offset", offset);
            parameters.Add("PageSize", filter.PageSize.Value);
        }

        var invoices = await connection.QueryAsync<Invoice>(sql, parameters);

        // Contar total de registros (sin paginación)
        var countSql = @"
            SELECT COUNT(*)
            FROM [Corporate].[Invoices] i
            WHERE 1=1";

        // Aplicar los mismos filtros para el conteo
        if (filter.InvoiceId.HasValue)
            countSql += " AND i.InvoiceId = @InvoiceId";
        if (filter.ContractId.HasValue)
            countSql += " AND i.ContractId = @ContractId";
        if (!string.IsNullOrEmpty(filter.InvoiceNumber))
            countSql += " AND i.InvoiceNumber LIKE @InvoiceNumber";
        if (!string.IsNullOrEmpty(filter.Status))
            countSql += " AND i.Status = @Status";
        if (!string.IsNullOrEmpty(filter.PaymentStatus))
            countSql += " AND i.PaymentStatus = @PaymentStatus";
        if (!string.IsNullOrEmpty(filter.CurrencyCode))
            countSql += " AND i.CurrencyCode = @CurrencyCode";
        if (filter.InvoiceDateFrom.HasValue)
            countSql += " AND i.InvoiceDate >= @InvoiceDateFrom";
        if (filter.InvoiceDateTo.HasValue)
            countSql += " AND i.InvoiceDate <= @InvoiceDateTo";
        if (filter.DueDateFrom.HasValue)
            countSql += " AND i.DueDate >= @DueDateFrom";
        if (filter.DueDateTo.HasValue)
            countSql += " AND i.DueDate <= @DueDateTo";
        if (filter.IsOverdue.HasValue && filter.IsOverdue.Value)
            countSql += " AND i.DueDate < GETDATE() AND i.PaymentStatus = 'Unpaid'";

        var totalRecords = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        return new InvoiceQueryResult
        {
            Invoices = invoices.ToList(),
            TotalRecords = totalRecords,
            PageNumber = filter.PageNumber ?? 1,
            PageSize = filter.PageSize ?? totalRecords
        };
    }

    /// <summary>
    /// Obtiene una factura por ID con sus items
    /// </summary>
    public async Task<InvoiceDetailDto?> GetInvoiceByIdAsync(int invoiceId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var invoiceSql = @"
            SELECT 
                i.InvoiceId, i.ContractId, i.InvoiceNumber, i.InvoiceDate, i.DueDate,
                i.SubTotal, i.Tax, i.Total, i.CurrencyCode, i.Status, i.PaymentStatus,
                i.PaidDate, i.PaidBy, i.PaymentMethodId, i.PaymentReference,
                i.DepositNumber, i.TransferNumber, i.Notes, i.CreatedAt, i.UpdatedAt,
                i.LastModifiedBy
            FROM [Corporate].[Invoices] i
            WHERE i.InvoiceId = @InvoiceId";

        var invoice = await connection.QueryFirstOrDefaultAsync<Invoice>(invoiceSql, new { InvoiceId = invoiceId });

        if (invoice == null)
            return null;

        var itemsSql = @"
            SELECT 
                InvoiceItemId, InvoiceId, ContractServiceId, Description,
                Quantity, UnitPrice, Discount, LineTotal, ServiceOrder, CreatedAt
            FROM [Corporate].[InvoiceItems]
            WHERE InvoiceId = @InvoiceId
            ORDER BY ServiceOrder, InvoiceItemId";

        var items = await connection.QueryAsync<InvoiceItem>(itemsSql, new { InvoiceId = invoiceId });

        var attachmentsSql = @"
            SELECT 
                InvoiceAttachmentId, InvoiceId, FileUrl, UploadedBy, UploadedAt
            FROM [Corporate].[InvoiceAttachments]
            WHERE InvoiceId = @InvoiceId
            ORDER BY UploadedAt DESC";

        var attachments = await connection.QueryAsync<InvoiceAttachment>(attachmentsSql, new { InvoiceId = invoiceId });

        return new InvoiceDetailDto
        {
            Invoice = invoice,
            Items = items.ToList(),
            Attachments = attachments.ToList()
        };
    }

    /// <summary>
    /// Obtiene resumen de facturas por contrato
    /// </summary>
    public async Task<InvoiceSummaryDto> GetInvoiceSummaryByContractAsync(int contractId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                COUNT(*) AS TotalInvoices,
                SUM(CASE WHEN PaymentStatus = 'Paid' THEN 1 ELSE 0 END) AS PaidInvoices,
                SUM(CASE WHEN PaymentStatus = 'Unpaid' THEN 1 ELSE 0 END) AS UnpaidInvoices,
                SUM(CASE WHEN Status = 'Overdue' THEN 1 ELSE 0 END) AS OverdueInvoices,
                SUM(Total) AS TotalAmount,
                SUM(CASE WHEN PaymentStatus = 'Paid' THEN Total ELSE 0 END) AS PaidAmount,
                SUM(CASE WHEN PaymentStatus = 'Unpaid' THEN Total ELSE 0 END) AS UnpaidAmount
            FROM [Corporate].[Invoices]
            WHERE ContractId = @ContractId";

        var summary = await connection.QueryFirstOrDefaultAsync<InvoiceSummaryDto>(sql, new { ContractId = contractId });

        return summary ?? new InvoiceSummaryDto();
    }
}

/// <summary>
/// Filtros para consulta de facturas
/// </summary>
public class InvoiceQueryFilter
{
    public int? InvoiceId { get; set; }
    public int? ContractId { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? Status { get; set; }
    public string? PaymentStatus { get; set; }
    public string? CurrencyCode { get; set; }
    public DateTime? InvoiceDateFrom { get; set; }
    public DateTime? InvoiceDateTo { get; set; }
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }
    public bool? IsOverdue { get; set; }
    public string? OrderBy { get; set; } // InvoiceDate, DueDate, Total, Status
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
}

/// <summary>
/// Resultado de consulta de facturas con paginación
/// </summary>
public class InvoiceQueryResult
{
    public List<Invoice> Invoices { get; set; } = new();
    public int TotalRecords { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalRecords / PageSize) : 0;
}

/// <summary>
/// DTO para detalle de factura con items y adjuntos
/// </summary>
public class InvoiceDetailDto
{
    public Invoice Invoice { get; set; } = new();
    public List<InvoiceItem> Items { get; set; } = new();
    public List<InvoiceAttachment> Attachments { get; set; } = new();
}

/// <summary>
/// DTO para resumen de facturas de un contrato
/// </summary>
public class InvoiceSummaryDto
{
    public int TotalInvoices { get; set; }
    public int PaidInvoices { get; set; }
    public int UnpaidInvoices { get; set; }
    public int OverdueInvoices { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal UnpaidAmount { get; set; }
}

