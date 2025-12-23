using ModelLayer;
using Microsoft.EntityFrameworkCore;
using BusinessLayer.Shared.Services;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command to delete an invoice attachment
/// </summary>
public class DeleteInvoiceAttachmentCommand
{
    private readonly DBcontext _context;
    private readonly AzureBlobStorageService _blobStorageService;

    public DeleteInvoiceAttachmentCommand(DBcontext context, AzureBlobStorageService blobStorageService)
    {
        _context = context;
        _blobStorageService = blobStorageService;
    }

    /// <summary>
    /// Deletes an invoice attachment from both database and Azure Blob Storage
    /// </summary>
    /// <param name="attachmentId">ID of the attachment to delete</param>
    /// <returns>Response with deletion result</returns>
    /// <exception cref="KeyNotFoundException">If attachment not found</exception>
    /// <exception cref="InvalidOperationException">If deletion fails</exception>
    public async Task<DeleteInvoiceAttachmentResponse> ExecuteAsync(int attachmentId)
    {
        // Get attachment with invoice details
        var attachment = await _context.InvoiceAttachments
            .Include(a => a.Invoice)
            .FirstOrDefaultAsync(a => a.InvoiceAttachmentId == attachmentId);

        if (attachment == null)
            throw new KeyNotFoundException($"Attachment with ID {attachmentId} not found");

        var fileName = attachment.FileName ?? "Unknown file";
        var invoiceNumber = attachment.Invoice?.InvoiceNumber ?? "Unknown invoice";

        try
        {
            // Delete from Azure Blob Storage first
            var blobDeleted = await _blobStorageService.DeleteFileAsync(attachment.FileUrl);

            if (!blobDeleted)
            {
                throw new InvalidOperationException(
                    $"Failed to delete file '{fileName}' from Azure Blob Storage. The file may not exist in storage.");
            }

            // Delete from database
            _context.InvoiceAttachments.Remove(attachment);
            await _context.SaveChangesAsync();

            return new DeleteInvoiceAttachmentResponse
            {
                Success = true,
                Message = $"Attachment '{fileName}' has been successfully deleted from invoice '{invoiceNumber}'",
                DeletedFileName = fileName,
                InvoiceNumber = invoiceNumber
            };
        }
        catch (InvalidOperationException)
        {
            // Re-throw our custom exceptions
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"An unexpected error occurred while deleting attachment '{fileName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deletes all attachments for a specific invoice
    /// </summary>
    /// <param name="invoiceId">ID of the invoice</param>
    /// <returns>Response with deletion result</returns>
    /// <exception cref="KeyNotFoundException">If invoice not found</exception>
    /// <exception cref="InvalidOperationException">If deletion fails</exception>
    public async Task<DeleteInvoiceAttachmentResponse> DeleteAllAttachmentsForInvoiceAsync(int invoiceId)
    {
        // Verify invoice exists
        var invoice = await _context.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

        if (invoice == null)
            throw new KeyNotFoundException($"Invoice with ID {invoiceId} not found");

        // Get all attachments for this invoice
        var attachments = await _context.InvoiceAttachments
            .Where(a => a.InvoiceId == invoiceId)
            .ToListAsync();

        if (!attachments.Any())
        {
            return new DeleteInvoiceAttachmentResponse
            {
                Success = true,
                Message = $"No attachments found for invoice '{invoice.InvoiceNumber}'",
                DeletedCount = 0,
                InvoiceNumber = invoice.InvoiceNumber
            };
        }

        try
        {
            int successfulDeletions = 0;
            var failedDeletions = new List<string>();

            // Delete each file from Azure Blob Storage
            foreach (var attachment in attachments)
            {
                try
                {
                    await _blobStorageService.DeleteFileAsync(attachment.FileUrl);
                    successfulDeletions++;
                }
                catch (Exception ex)
                {
                    failedDeletions.Add($"{attachment.FileName ?? "Unknown"}: {ex.Message}");
                }
            }

            // If any blob deletion failed, throw exception
            if (failedDeletions.Any())
            {
                throw new InvalidOperationException(
                    $"Failed to delete {failedDeletions.Count} file(s) from Azure Blob Storage: " +
                    string.Join("; ", failedDeletions));
            }

            // Delete all attachment records from database
            _context.InvoiceAttachments.RemoveRange(attachments);
            await _context.SaveChangesAsync();

            return new DeleteInvoiceAttachmentResponse
            {
                Success = true,
                Message = $"Successfully deleted {attachments.Count} attachment(s) from invoice '{invoice.InvoiceNumber}'",
                DeletedCount = attachments.Count,
                InvoiceNumber = invoice.InvoiceNumber
            };
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"An unexpected error occurred while deleting attachments for invoice '{invoice.InvoiceNumber}': {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Response for delete invoice attachment operation
/// </summary>
public class DeleteInvoiceAttachmentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? DeletedFileName { get; set; }
    public string? InvoiceNumber { get; set; }
    public int DeletedCount { get; set; }
}

