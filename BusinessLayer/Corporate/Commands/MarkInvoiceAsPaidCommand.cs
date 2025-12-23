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
            throw new KeyNotFoundException($"Invoice with ID {invoiceId} not found");

        // Validar userId
        if (userId <= 0)
        {
            throw new InvalidOperationException(
                $"Invalid user ID: {userId}. The user ID must be a valid positive integer.");
        }

        // Verificar que el usuario existe en la base de datos
        var userExists = await _context.Users
            .AnyAsync(u => u.UserId == userId);
        
        if (!userExists)
        {
            throw new InvalidOperationException(
                $"User with ID {userId} does not exist in the database. Cannot mark invoice as paid.");
        }

        // Validations
        if (invoice.PaymentStatus == "Paid")
            throw new InvalidOperationException($"Invoice '{invoice.InvoiceNumber}' is already marked as paid");

        if (invoice.Status == "Cancelled")
            throw new InvalidOperationException($"Cannot mark invoice '{invoice.InvoiceNumber}' as paid because it has been cancelled");

        if (request.PaidDate.HasValue && request.PaidDate.Value > DateTime.Now)
            throw new InvalidOperationException("Payment date cannot be in the future");

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

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            throw new InvalidOperationException(
                $"Failed to save invoice payment status to database. Invoice: '{invoice.InvoiceNumber}' (ID: {invoiceId}), " +
                $"PaymentMethodId: {request.PaymentMethodId}, PaidDate: {request.PaidDate?.ToString("yyyy-MM-dd") ?? "today"}. " +
                $"Database error: {innerMessage}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"An unexpected error occurred while saving invoice payment status. Invoice: '{invoice.InvoiceNumber}' (ID: {invoiceId}). " +
                $"Error: {ex.Message}", ex);
        }

        return new MarkInvoiceAsPaidResponse
        {
            Success = true,
            Message = $"Invoice '{invoice.InvoiceNumber}' has been successfully marked as paid",
            InvoiceId = invoiceId,
            InvoiceNumber = invoice.InvoiceNumber,
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
    public int? UserId { get; set; } // Opcional: si no viene, se obtiene del token JWT
}

/// <summary>
/// Response de marcar factura como pagada
/// </summary>
public class MarkInvoiceAsPaidResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime PaidDate { get; set; }
}




