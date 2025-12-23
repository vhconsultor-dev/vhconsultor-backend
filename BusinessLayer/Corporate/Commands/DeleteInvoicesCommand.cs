using ModelLayer;
using ModelLayer.Shared;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command para eliminar facturas (físicamente) usando Entity Framework
/// Puede eliminar una factura específica o todas las facturas de un contrato
/// </summary>
public class DeleteInvoicesCommand
{
    private readonly DBcontext _context;

    public DeleteInvoicesCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Elimina una factura específica
    /// </summary>
    /// <param name="invoiceId">ID de la factura a eliminar</param>
    /// <returns>Resultado de la operación</returns>
    /// <exception cref="KeyNotFoundException">Si la factura no existe</exception>
    /// <exception cref="InvalidOperationException">Si la factura está pagada o tiene adjuntos</exception>
    public async Task<DeleteInvoicesResponse> DeleteSingleInvoiceAsync(int invoiceId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.InvoiceAttachments)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
        
        if (invoice == null)
            throw new KeyNotFoundException($"Invoice with ID {invoiceId} not found");

        // Validar que no esté pagada
        if (invoice.PaymentStatus == "Paid")
        {
            throw new InvalidOperationException(
                $"Cannot delete invoice '{invoice.InvoiceNumber}' because it has already been paid. " +
                "Paid invoices cannot be deleted to maintain financial record integrity.");
        }

        // Validar que no tenga adjuntos
        if (invoice.InvoiceAttachments.Any())
        {
            var attachmentCount = invoice.InvoiceAttachments.Count;
            var attachmentWord = attachmentCount == 1 ? "attachment" : "attachments";
            
            throw new InvalidOperationException(
                $"Cannot delete invoice '{invoice.InvoiceNumber}' because it has {attachmentCount} associated {attachmentWord}. " +
                "Please delete all attachments first before deleting the invoice.");
        }

        // Eliminar la factura (cascade eliminará items automáticamente)
        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();

        return new DeleteInvoicesResponse
        {
            Success = true,
            DeletedCount = 1,
            Message = $"Invoice '{invoice.InvoiceNumber}' has been successfully deleted"
        };
    }

    /// <summary>
    /// Elimina todas las facturas de un contrato
    /// </summary>
    /// <param name="contractId">ID del contrato</param>
    /// <returns>Resultado de la operación</returns>
    /// <exception cref="InvalidOperationException">Si alguna factura está pagada o tiene adjuntos</exception>
    public async Task<DeleteInvoicesResponse> DeleteAllContractInvoicesAsync(int contractId)
    {
        var invoices = await _context.Invoices
            .Include(i => i.InvoiceAttachments)
            .Where(i => i.ContractId == contractId)
            .ToListAsync();

        if (!invoices.Any())
        {
            return new DeleteInvoicesResponse
            {
                Success = true,
                DeletedCount = 0,
                Message = "No invoices found for this contract"
            };
        }

        // Validar facturas pagadas
        var paidInvoices = invoices.Where(i => i.PaymentStatus == "Paid").ToList();
        if (paidInvoices.Any())
        {
            var paidInvoiceNumbers = string.Join(", ", paidInvoices.Select(i => i.InvoiceNumber));
            var paidWord = paidInvoices.Count == 1 ? "invoice is" : "invoices are";
            
            throw new InvalidOperationException(
                $"Cannot delete invoices because {paidInvoices.Count} {paidWord} already paid: {paidInvoiceNumbers}. " +
                "Paid invoices cannot be deleted to maintain financial record integrity.");
        }

        // Validar facturas con adjuntos
        var invoicesWithAttachments = invoices.Where(i => i.InvoiceAttachments.Any()).ToList();
        if (invoicesWithAttachments.Any())
        {
            var attachmentDetails = invoicesWithAttachments
                .Select(i => $"{i.InvoiceNumber} ({i.InvoiceAttachments.Count} attachment{(i.InvoiceAttachments.Count > 1 ? "s" : "")})")
                .ToList();
            var detailsList = string.Join(", ", attachmentDetails);
            var invoiceWord = invoicesWithAttachments.Count == 1 ? "invoice has" : "invoices have";
            
            throw new InvalidOperationException(
                $"Cannot delete invoices because {invoicesWithAttachments.Count} {invoiceWord} attachments: {detailsList}. " +
                "Please delete all attachments first before deleting the invoices.");
        }

        // Eliminar todas las facturas
        _context.Invoices.RemoveRange(invoices);
        await _context.SaveChangesAsync();

        var invoiceNumbers = string.Join(", ", invoices.Select(i => i.InvoiceNumber));

        return new DeleteInvoicesResponse
        {
            Success = true,
            DeletedCount = invoices.Count,
            Message = $"Successfully deleted {invoices.Count} invoice(s): {invoiceNumbers}"
        };
    }
}

/// <summary>
/// Respuesta del comando de eliminación de facturas
/// </summary>
public class DeleteInvoicesResponse
{
    public bool Success { get; set; }
    public int DeletedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}




