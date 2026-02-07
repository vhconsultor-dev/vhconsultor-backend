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
    /// <exception cref="InvalidOperationException">Si la factura está pagada</exception>
    public async Task<DeleteInvoicesResponse> DeleteSingleInvoiceAsync(int invoiceId)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
        
        if (invoice == null)
            throw new KeyNotFoundException($"Invoice with ID {invoiceId} not found");

        // Validar que no esté pagada
        if (invoice.PaymentStatus == "Paid")
        {
            throw new InvalidOperationException(
                $"Cannot delete invoice '{invoice.InvoiceNumber}' because it is already paid. " +
                "Paid invoices cannot be deleted to maintain financial record integrity.");
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
    /// <exception cref="InvalidOperationException">Si alguna factura está pagada</exception>
    public async Task<DeleteInvoicesResponse> DeleteAllContractInvoicesAsync(int contractId)
    {
        var invoices = await _context.Invoices
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
            var paidInvoiceNumbers = string.Join(", ", paidInvoices.Select(i => $"#{i.InvoiceNumber}"));
            var paidWord = paidInvoices.Count == 1 ? "invoice is" : "invoices are";
            
            throw new InvalidOperationException(
                $"Cannot delete all invoices because {paidInvoices.Count} {paidWord} already paid: {paidInvoiceNumbers}. " +
                "You can manually delete the unpaid invoices individually.");
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





