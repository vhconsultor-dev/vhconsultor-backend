using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Commands;

public class MarkManualInvoiceAsPaidCommand
{
    private readonly DBcontext _context;

    public MarkManualInvoiceAsPaidCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task<MarkManualInvoiceAsPaidResponse> ExecuteAsync(int manualInvoiceHeaderId, MarkManualInvoiceAsPaidRequest request, int userId)
    {
        var header = await _context.ManualInvoiceHeaders
            .FirstOrDefaultAsync(h => h.ManualInvoiceHeaderId == manualInvoiceHeaderId);

        if (header == null)
            throw new KeyNotFoundException($"Manual invoice with ID {manualInvoiceHeaderId} not found.");

        if (header.PaymentStatus == "Paid")
            throw new InvalidOperationException($"Invoice '{header.InvoiceNumber}' is already paid.");

        if (header.Status == "Cancelled")
            throw new InvalidOperationException($"Invoice '{header.InvoiceNumber}' is cancelled and cannot be marked as paid.");

        if (request.PaidDate.HasValue && request.PaidDate.Value > DateTime.Now)
            throw new InvalidOperationException("Payment date cannot be in the future.");

        var userExists = await _context.Users.AnyAsync(u => u.UserId == userId);
        if (!userExists)
            throw new InvalidOperationException($"User with ID {userId} does not exist.");

        header.PaymentStatus = "Paid";
        header.Status = "Paid";
        header.PaidDate = request.PaidDate ?? DateTimeService.GetCostaRicaNow();
        header.PaidBy = userId;
        header.PaymentMethodId = request.PaymentMethodId;
        header.PaymentReference = request.PaymentReference;
        header.DepositNumber = request.DepositNumber;
        header.TransferNumber = request.TransferNumber;
        header.UpdatedAt = DateTimeService.GetCostaRicaNow();
        header.LastModifiedBy = userId.ToString();

        await _context.SaveChangesAsync();

        return new MarkManualInvoiceAsPaidResponse
        {
            ManualInvoiceHeaderId = header.ManualInvoiceHeaderId,
            InvoiceNumber = header.InvoiceNumber,
            PaidDate = header.PaidDate!.Value
        };
    }
}

public class MarkManualInvoiceAsPaidRequest
{
    public DateTime? PaidDate { get; set; }
    public int? PaymentMethodId { get; set; }
    public string? PaymentReference { get; set; }
    public string? DepositNumber { get; set; }
    public string? TransferNumber { get; set; }
    public int? UserId { get; set; }
}

public class MarkManualInvoiceAsPaidResponse
{
    public int ManualInvoiceHeaderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime PaidDate { get; set; }
}
