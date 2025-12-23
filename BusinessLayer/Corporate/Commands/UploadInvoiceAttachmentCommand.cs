using ModelLayer;
using ModelLayer.Corporate.Entities;
using Microsoft.EntityFrameworkCore;
using BusinessLayer.Shared.Services;
using Microsoft.AspNetCore.Http;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command to upload an attachment to an invoice
/// </summary>
public class UploadInvoiceAttachmentCommand
{
    private readonly DBcontext _context;
    private readonly AzureBlobStorageService _blobStorageService;

    public UploadInvoiceAttachmentCommand(DBcontext context, AzureBlobStorageService blobStorageService)
    {
        _context = context;
        _blobStorageService = blobStorageService;
    }

    /// <summary>
    /// Uploads a file attachment to an invoice
    /// </summary>
    /// <param name="invoiceId">ID of the invoice</param>
    /// <param name="file">File to upload</param>
    /// <param name="uploadedBy">User ID who is uploading the file</param>
    /// <returns>Response with attachment details</returns>
    /// <exception cref="KeyNotFoundException">If invoice not found</exception>
    /// <exception cref="InvalidOperationException">If validation fails</exception>
    public async Task<UploadInvoiceAttachmentResponse> ExecuteAsync(int invoiceId, IFormFile file, int uploadedBy)
    {
        try
        {
            // Validate invoice exists
            var invoice = await _context.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null)
                throw new KeyNotFoundException($"Invoice with ID {invoiceId} not found");

            // Validate file
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("No file was provided or the file is empty");

            // Validate file extension
            if (!_blobStorageService.IsFileExtensionAllowed(file.FileName))
            {
                var allowedExtensions = string.Join(", ", _blobStorageService.GetAllowedExtensions());
                throw new InvalidOperationException(
                    $"File type not allowed. The file '{file.FileName}' has an unsupported extension. " +
                    $"Allowed types are: {allowedExtensions}");
            }

            // Validate file size
            if (!_blobStorageService.IsFileSizeAllowed(file.Length))
            {
                var maxSizeMB = _blobStorageService.GetMaxFileSizeMB();
                var fileSizeMB = Math.Round(file.Length / (1024.0 * 1024.0), 2);
                throw new InvalidOperationException(
                    $"File size exceeds the maximum allowed limit. " +
                    $"The file '{file.FileName}' is {fileSizeMB} MB, but the maximum allowed size is {maxSizeMB} MB");
            }

            // Generate unique file name to avoid conflicts
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

            // Upload file to Azure Blob Storage
            string fileUrl;
            try
            {
                using (var stream = file.OpenReadStream())
                {
                    fileUrl = await _blobStorageService.UploadFileAsync(
                        stream,
                        uniqueFileName,
                        invoice.InvoiceNumber,
                        file.ContentType);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to upload file to Azure Blob Storage. File: '{file.FileName}', " +
                    $"Invoice: '{invoice.InvoiceNumber}'. Error: {ex.Message}", ex);
            }

            // Create attachment record in database
            var attachment = new InvoiceAttachment
            {
                InvoiceId = invoiceId,
                FileUrl = fileUrl,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow.AddHours(-6) // Costa Rica time
            };

            _context.InvoiceAttachments.Add(attachment);
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to save attachment record to database. File: '{file.FileName}', " +
                    $"FileUrl: '{fileUrl}'. Error: {ex.Message}", ex);
            }

            return new UploadInvoiceAttachmentResponse
            {
                Success = true,
                Message = $"File '{file.FileName}' has been successfully uploaded to invoice '{invoice.InvoiceNumber}'",
                AttachmentId = attachment.InvoiceAttachmentId,
                FileName = file.FileName,
                FileUrl = attachment.FileUrl,
                FileSize = file.Length,
                UploadedAt = attachment.UploadedAt
            };
        }
        catch (KeyNotFoundException)
        {
            throw; // Re-throw as is
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw as is
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"An unexpected error occurred while processing the file upload. " +
                $"File: '{file?.FileName ?? "unknown"}', InvoiceId: {invoiceId}. " +
                $"Error: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Response for upload invoice attachment operation
/// </summary>
public class UploadInvoiceAttachmentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int AttachmentId { get; set; }
    public string? FileName { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public long? FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
}

