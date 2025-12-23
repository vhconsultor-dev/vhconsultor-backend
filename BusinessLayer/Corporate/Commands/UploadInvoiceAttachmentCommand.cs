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

        try
        {
            // Generate unique file name to avoid conflicts
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

            // Upload file to Azure Blob Storage
            string fileUrl;
            using (var stream = file.OpenReadStream())
            {
                fileUrl = await _blobStorageService.UploadFileAsync(
                    stream,
                    uniqueFileName,
                    invoice.InvoiceNumber,
                    file.ContentType);
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
            await _context.SaveChangesAsync();

            return new UploadInvoiceAttachmentResponse
            {
                Success = true,
                Message = $"File '{file.FileName}' has been successfully uploaded to invoice '{invoice.InvoiceNumber}'",
                AttachmentId = attachment.InvoiceAttachmentId,
                FileName = file.FileName, // Original file name from upload
                FileUrl = attachment.FileUrl,
                FileSize = file.Length, // File size from upload
                UploadedAt = attachment.UploadedAt
            };
        }
        catch (InvalidOperationException ex)
        {
            // Re-throw blob storage exceptions
            throw new InvalidOperationException(
                $"Failed to upload file '{file.FileName}': {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"An unexpected error occurred while uploading the file '{file.FileName}': {ex.Message}", ex);
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

