using ModelLayer;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command para actualizar una factura (solo si no está pagada)
/// </summary>
public class UpdateInvoiceCommand
{
    private readonly DBcontext _context;

    public UpdateInvoiceCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ejecuta la actualización de una factura
    /// </summary>
    public async Task<UpdateInvoiceResponse> ExecuteAsync(int invoiceId, UpdateInvoiceRequest request)
    {
        // 1. Obtener factura con sus items
        var invoice = await _context.Invoices
            .Include(i => i.InvoiceItems)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

        if (invoice == null)
            throw new KeyNotFoundException($"Invoice with ID {invoiceId} not found");

        // 2. Validar que no esté pagada
        if (invoice.PaymentStatus == "Paid")
        {
            throw new InvalidOperationException(
                $"Cannot update invoice '{invoice.InvoiceNumber}' because it is already paid. " +
                "Paid invoices cannot be modified to maintain financial record integrity.");
        }

        // 3. Actualizar campos
        invoice.InvoiceDate = request.InvoiceDate;
        invoice.DueDate = request.DueDate;
        invoice.SubTotal = request.Amount;
        invoice.Tax = 0; // Sin impuestos
        invoice.Total = request.Amount;
        invoice.Status = request.Status ?? invoice.Status;
        invoice.Notes = request.Notes;
        if (!string.IsNullOrWhiteSpace(request.Lang))
            invoice.Lang = request.Lang.Trim();
        invoice.UpdatedAt = DateTimeService.GetCostaRicaNow();

        // 4. Actualizar o crear item
        if (invoice.InvoiceItems.Any())
        {
            // Actualizar el primer item
            var item = invoice.InvoiceItems.First();
            item.Description = request.Description ?? item.Description;
            item.UnitPrice = request.Amount;
            item.LineTotal = request.Amount;
        }
        else
        {
            // Crear un nuevo item si no existe
            var item = new InvoiceItem
            {
                Description = request.Description ?? "Services for the period",
                Quantity = 1,
                UnitPrice = request.Amount,
                Discount = 0,
                LineTotal = request.Amount,
                CreatedAt = DateTimeService.GetCostaRicaNow()
            };
            invoice.InvoiceItems.Add(item);
        }

        // 5. Guardar cambios
        await _context.SaveChangesAsync();

        return new UpdateInvoiceResponse
        {
            Success = true,
            Message = $"Invoice '{invoice.InvoiceNumber}' has been successfully updated",
            InvoiceId = invoice.InvoiceId,
            InvoiceNumber = invoice.InvoiceNumber
        };
    }
}

/// <summary>
/// Request para actualizar una factura
/// </summary>
public class UpdateInvoiceRequest
{
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    /// <summary>Language for PDF: "en" (English) or "es" (Spanish).</summary>
    public string? Lang { get; set; }
}

/// <summary>
/// Response de actualización de factura
/// </summary>
public class UpdateInvoiceResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
}
