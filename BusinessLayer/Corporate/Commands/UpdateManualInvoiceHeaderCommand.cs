using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Commands;

public class UpdateManualInvoiceHeaderCommand
{
    private readonly DBcontext _context;

    public UpdateManualInvoiceHeaderCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task<UpdateManualInvoiceHeaderResponse> ExecuteAsync(int manualInvoiceHeaderId, UpdateManualInvoiceHeaderRequest request, string modifiedBy)
    {
        var header = await _context.ManualInvoiceHeaders
            .Include(h => h.Details)
            .FirstOrDefaultAsync(h => h.ManualInvoiceHeaderId == manualInvoiceHeaderId);

        if (header == null)
            throw new KeyNotFoundException($"Manual invoice with ID {manualInvoiceHeaderId} not found.");

        if (header.PaymentStatus == "Paid")
            throw new InvalidOperationException($"Invoice '{header.InvoiceNumber}' is already paid and cannot be modified.");

        if (header.Status == "Cancelled")
            throw new InvalidOperationException($"Invoice '{header.InvoiceNumber}' is cancelled and cannot be modified.");

        // Replace details
        _context.ManualInvoiceDetails.RemoveRange(header.Details);

        var details = BuildDetails(request.Details);

        decimal subTotal = details.Sum(d => d.LineTotal);
        decimal discountAmount = request.DiscountAmount ?? 0;
        decimal netBeforeTax = subTotal - discountAmount;
        decimal taxRate = request.TaxRate ?? header.TaxRate;
        decimal taxAmount = Math.Round(netBeforeTax * taxRate / 100, 2);

        header.InvoiceDate = request.InvoiceDate;
        header.DueDate = request.DueDate;
        header.CurrencyCode = request.CurrencyCode.Trim().ToUpper();
        header.ExchangeRate = request.ExchangeRate ?? header.ExchangeRate;
        header.SubTotal = subTotal;
        header.DiscountAmount = discountAmount;
        header.TaxRate = taxRate;
        header.TaxAmount = taxAmount;
        header.Total = netBeforeTax + taxAmount;
        header.BillingPeriodFrom = request.BillingPeriodFrom;
        header.BillingPeriodTo = request.BillingPeriodTo;
        header.BillingName = request.BillingName?.Trim();
        header.BillingEmail = request.BillingEmail?.Trim();
        header.Notes = request.Notes?.Trim();
        header.ClientNotes = request.ClientNotes?.Trim();
        header.Lang = string.IsNullOrWhiteSpace(request.Lang) ? header.Lang : request.Lang.Trim();
        header.Status = request.Status ?? header.Status;
        header.UpdatedAt = DateTimeService.GetCostaRicaNow();
        header.LastModifiedBy = modifiedBy;

        foreach (var detail in details)
            header.Details.Add(detail);

        await _context.SaveChangesAsync();

        return new UpdateManualInvoiceHeaderResponse
        {
            ManualInvoiceHeaderId = header.ManualInvoiceHeaderId,
            InvoiceNumber = header.InvoiceNumber,
            Total = header.Total
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
}

public class UpdateManualInvoiceHeaderRequest
{
    // Required
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
    public string? Status { get; set; }
}

public class UpdateManualInvoiceHeaderResponse
{
    public int ManualInvoiceHeaderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Total { get; set; }
}
