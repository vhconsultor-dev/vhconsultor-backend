using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Commands;

public class CreateManualInvoiceHeaderCommand
{
    private readonly DBcontext _context;

    public CreateManualInvoiceHeaderCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task<CreateManualInvoiceHeaderResponse> ExecuteAsync(CreateManualInvoiceHeaderRequest request, int createdByUserId)
    {
        var customerExists = await _context.Customers.AnyAsync(c => c.CustomerId == request.CustomerId);
        if (!customerExists)
            throw new KeyNotFoundException($"Customer with ID {request.CustomerId} not found.");

        var invoiceNumber = await GenerateInvoiceNumberAsync();

        // Compute line totals
        var details = BuildDetails(request.Details);

        decimal subTotal = details.Sum(d => d.LineTotal);
        decimal discountAmount = request.DiscountAmount ?? 0;
        decimal netBeforeTax = subTotal - discountAmount;
        decimal taxRate = request.TaxRate ?? 0;
        decimal taxAmount = Math.Round(netBeforeTax * taxRate / 100, 2);
        decimal total = netBeforeTax + taxAmount;

        var header = new ManualInvoiceHeader
        {
            CustomerId = request.CustomerId,
            InvoiceNumber = invoiceNumber,
            InvoiceDate = request.InvoiceDate,
            DueDate = request.DueDate,
            CurrencyCode = request.CurrencyCode.Trim().ToUpper(),
            ExchangeRate = request.ExchangeRate ?? 1,
            SubTotal = subTotal,
            DiscountAmount = discountAmount,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            Total = total,
            Status = "Draft",
            PaymentStatus = "Unpaid",
            BillingPeriodFrom = request.BillingPeriodFrom,
            BillingPeriodTo = request.BillingPeriodTo,
            BillingName = request.BillingName?.Trim(),
            BillingEmail = request.BillingEmail?.Trim(),
            Notes = request.Notes?.Trim(),
            ClientNotes = request.ClientNotes?.Trim(),
            Lang = string.IsNullOrWhiteSpace(request.Lang) ? "es" : request.Lang.Trim(),
            CreatedBy = createdByUserId,
            CreatedAt = DateTimeService.GetCostaRicaNow()
        };

        foreach (var detail in details)
            header.Details.Add(detail);

        _context.ManualInvoiceHeaders.Add(header);
        await _context.SaveChangesAsync();

        return new CreateManualInvoiceHeaderResponse
        {
            ManualInvoiceHeaderId = header.ManualInvoiceHeaderId,
            InvoiceNumber = header.InvoiceNumber,
            Total = header.Total,
            CurrencyCode = header.CurrencyCode
        };
    }

    private List<ManualInvoiceDetail> BuildDetails(List<ManualInvoiceDetailRequest> items)
    {
        var result = new List<ManualInvoiceDetail>();
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            decimal gross = item.Quantity * item.UnitPrice;
            decimal discountPercent = item.DiscountPercent ?? 0;
            decimal discountAmount = Math.Round(gross * discountPercent / 100, 2);
            decimal lineTotal = Math.Round(gross - discountAmount, 2);

            result.Add(new ManualInvoiceDetail
            {
                ServiceDescription = item.ServiceDescription.Trim(),
                UnitLabel = item.UnitLabel?.Trim(),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountPercent = discountPercent,
                DiscountAmount = discountAmount,
                LineTotal = lineTotal,
                TaxApplicable = item.TaxApplicable ?? true,
                Notes = item.Notes?.Trim(),
                DisplayOrder = item.DisplayOrder ?? i,
                CreatedAt = DateTimeService.GetCostaRicaNow()
            });
        }
        return result;
    }

    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var year = DateTime.Now.Year;
        var last = await _context.ManualInvoiceHeaders
            .Where(h => h.InvoiceNumber.StartsWith($"MINV-{year}-"))
            .OrderByDescending(h => h.InvoiceNumber)
            .FirstOrDefaultAsync();

        int next = 1;
        if (last != null)
        {
            var parts = last.InvoiceNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out int lastNum))
                next = lastNum + 1;
        }

        return $"MINV-{year}-{next:D4}";
    }
}

public class CreateManualInvoiceHeaderRequest
{
    // Required
    public int CustomerId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public List<ManualInvoiceDetailRequest> Details { get; set; } = new();

    // Optional
    public decimal? ExchangeRate { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? DiscountAmount { get; set; }
    public DateTime? BillingPeriodFrom { get; set; }
    public DateTime? BillingPeriodTo { get; set; }
    public string? BillingName { get; set; }
    public string? BillingEmail { get; set; }
    public string? Notes { get; set; }
    public string? ClientNotes { get; set; }
    public string? Lang { get; set; }
}

public class ManualInvoiceDetailRequest
{
    // Required
    public string ServiceDescription { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    // Optional
    public string? UnitLabel { get; set; }
    public decimal? DiscountPercent { get; set; }
    public bool? TaxApplicable { get; set; }
    public string? Notes { get; set; }
    public int? DisplayOrder { get; set; }
}

public class CreateManualInvoiceHeaderResponse
{
    public int ManualInvoiceHeaderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
}
