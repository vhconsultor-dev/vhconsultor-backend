using ModelLayer;
using ModelLayer.Shared;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command para marcar una factura como pagada
/// </summary>
public class MarkInvoiceAsPaidCommand
{
    private readonly DBcontext _context;

    public MarkInvoiceAsPaidCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ejecuta el marcado de factura como pagada
    /// </summary>
    /// <param name="invoiceId">ID de la factura</param>
    /// <param name="request">Datos del pago</param>
    /// <param name="userId">ID del usuario que aplica el pago</param>
    /// <returns>True si se marcó exitosamente</returns>
    public async Task<MarkInvoiceAsPaidResponse> ExecuteAsync(int invoiceId, MarkInvoiceAsPaidRequest request, int userId)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

        if (invoice == null)
            throw new KeyNotFoundException($"Factura con ID {invoiceId} no encontrada");

        // Validaciones
        if (invoice.PaymentStatus == "Paid")
            throw new InvalidOperationException("La factura ya está pagada");

        if (invoice.Status == "Cancelled")
            throw new InvalidOperationException("No se puede pagar una factura cancelada");

        if (request.PaidDate.HasValue && request.PaidDate.Value > DateTime.Now)
            throw new InvalidOperationException("La fecha de pago no puede ser futura");

        // Marcar como pagada
        invoice.PaymentStatus = "Paid";
        invoice.Status = "Paid";
        invoice.PaidDate = request.PaidDate ?? DateTime.Now.Date;
        invoice.PaidBy = userId;
        invoice.PaymentMethodId = request.PaymentMethodId;
        invoice.PaymentReference = request.PaymentReference;
        invoice.DepositNumber = request.DepositNumber;
        invoice.TransferNumber = request.TransferNumber;
        invoice.UpdatedAt = DateTimeService.GetCostaRicaNow();
        invoice.LastModifiedBy = userId.ToString();

        await _context.SaveChangesAsync();

        return new MarkInvoiceAsPaidResponse
        {
            Success = true,
            Message = "Factura marcada como pagada exitosamente",
            InvoiceId = invoiceId,
            PaidDate = invoice.PaidDate.Value
        };
    }
}

/// <summary>
/// Request para marcar factura como pagada
/// </summary>
public class MarkInvoiceAsPaidRequest
{
    public DateTime? PaidDate { get; set; }
    public int? PaymentMethodId { get; set; }
    public string? PaymentReference { get; set; }
    public string? DepositNumber { get; set; }
    public string? TransferNumber { get; set; }
}

/// <summary>
/// Response de marcar factura como pagada
/// </summary>
public class MarkInvoiceAsPaidResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int InvoiceId { get; set; }
    public DateTime PaidDate { get; set; }
}

