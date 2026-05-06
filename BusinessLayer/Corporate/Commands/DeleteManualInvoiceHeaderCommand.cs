using Microsoft.EntityFrameworkCore;
using ModelLayer;

namespace BusinessLayer.Corporate.Commands;

public class DeleteManualInvoiceHeaderCommand
{
    private readonly DBcontext _context;

    public DeleteManualInvoiceHeaderCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task<DeleteManualInvoiceHeaderResponse> ExecuteAsync(int manualInvoiceHeaderId)
    {
        var header = await _context.ManualInvoiceHeaders
            .FirstOrDefaultAsync(h => h.ManualInvoiceHeaderId == manualInvoiceHeaderId);

        if (header == null)
            throw new KeyNotFoundException($"Manual invoice with ID {manualInvoiceHeaderId} not found.");

        if (header.PaymentStatus == "Paid")
            throw new InvalidOperationException($"Invoice '{header.InvoiceNumber}' is already paid and cannot be deleted.");

        var invoiceNumber = header.InvoiceNumber;
        _context.ManualInvoiceHeaders.Remove(header);
        await _context.SaveChangesAsync();

        return new DeleteManualInvoiceHeaderResponse
        {
            ManualInvoiceHeaderId = manualInvoiceHeaderId,
            InvoiceNumber = invoiceNumber
        };
    }
}

public class DeleteManualInvoiceHeaderResponse
{
    public int ManualInvoiceHeaderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
}
