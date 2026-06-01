using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace BusinessLayer.Shared.Services;

/// <summary>
/// Service for managing file uploads and deletions in Azure Blob Storage
/// </summary>
public class AzureBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly AzureStorageSettings _settings;

    public AzureBlobStorageService(IOptions<AzureStorageSettings> settings)
    {
        _settings = settings.Value;
        _blobServiceClient = new BlobServiceClient(_settings.ConnectionString);
    }

    /// <summary>
    /// Uploads a contract document to Azure Blob Storage
    /// </summary>
    /// <param name="fileStream">File stream to upload</param>
    /// <param name="fileName">Name of the file</param>
    /// <param name="contractNumber">Contract number for folder organization</param>
    /// <param name="contentType">MIME type of the file</param>
    /// <returns>URL of the uploaded file</returns>
    /// <exception cref="InvalidOperationException">If upload fails</exception>
    public async Task<string> UploadContractDocumentAsync(Stream fileStream, string fileName, string contractNumber, string contentType)
    {
        try
        {
            // Validar que el connection string esté configurado
            if (string.IsNullOrEmpty(_settings.ConnectionString))
            {
                throw new InvalidOperationException(
                    "Azure Storage Connection String is not configured. " +
                    "Please set the 'AzureStorage:ConnectionString' configuration value or environment variable.");
            }

            // Get or create container
            var containerClient = _blobServiceClient.GetBlobContainerClient(_settings.ContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            // Build blob path: ContractSignedDocuments/{ContractNumber}/{fileName}
            var blobPath = $"ContractSignedDocuments/{contractNumber}/{fileName}";
            var blobClient = containerClient.GetBlobClient(blobPath);

            // Set content type
            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            };

            // Upload file (overwrite if exists)
            await blobClient.UploadAsync(fileStream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders,
                Conditions = null // Permite sobrescribir si existe
            });

            // Return the blob URL
            return blobClient.Uri.ToString();
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw configuration errors as is
        }
        catch (Azure.RequestFailedException ex)
        {
            throw new InvalidOperationException(
                $"Azure Storage request failed while uploading '{fileName}'. " +
                $"Status: {ex.Status}, Error Code: {ex.ErrorCode}, Message: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to upload contract document '{fileName}' to Azure Blob Storage. " +
                $"Container: '{_settings.ContainerName}', Path: 'ContractSignedDocuments/{contractNumber}'. " +
                $"Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Uploads a file to Azure Blob Storage
    /// </summary>
    /// <param name="fileStream">File stream to upload</param>
    /// <param name="fileName">Name of the file</param>
    /// <param name="invoiceNumber">Invoice number for folder organization</param>
    /// <param name="contentType">MIME type of the file</param>
    /// <returns>URL of the uploaded file</returns>
    /// <exception cref="InvalidOperationException">If upload fails</exception>
    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string invoiceNumber, string contentType)
    {
        try
        {
            // Validar que el connection string esté configurado
            if (string.IsNullOrEmpty(_settings.ConnectionString))
            {
                throw new InvalidOperationException(
                    "Azure Storage Connection String is not configured. " +
                    "Please set the 'AzureStorage:ConnectionString' configuration value or environment variable.");
            }

            // Get or create container
            var containerClient = _blobServiceClient.GetBlobContainerClient(_settings.ContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            // Build blob path: InvoicesAttachment/{InvoiceNumber}/{fileName}
            var blobPath = $"{_settings.InvoiceAttachmentsFolder}/{invoiceNumber}/{fileName}";
            var blobClient = containerClient.GetBlobClient(blobPath);

            // Set content type
            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            };

            // Upload file
            await blobClient.UploadAsync(fileStream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders
            });

            // Return the blob URL
            return blobClient.Uri.ToString();
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw configuration errors as is
        }
        catch (Azure.RequestFailedException ex)
        {
            throw new InvalidOperationException(
                $"Azure Storage request failed while uploading '{fileName}'. " +
                $"Status: {ex.Status}, Error Code: {ex.ErrorCode}, Message: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to upload file '{fileName}' to Azure Blob Storage. " +
                $"Container: '{_settings.ContainerName}', Path: '{_settings.InvoiceAttachmentsFolder}/{invoiceNumber}'. " +
                $"Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deletes a file from Azure Blob Storage
    /// </summary>
    /// <param name="fileUrl">Full URL of the file to delete</param>
    /// <returns>True if deleted successfully</returns>
    /// <exception cref="InvalidOperationException">If deletion fails</exception>
    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            // Extract blob name from URL
            var uri = new Uri(fileUrl);
            var blobName = uri.AbsolutePath.TrimStart('/');
            
            // Remove container name from path if present
            if (blobName.StartsWith(_settings.ContainerName + "/"))
            {
                blobName = blobName.Substring(_settings.ContainerName.Length + 1);
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(_settings.ContainerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Delete the blob
            var response = await blobClient.DeleteIfExistsAsync();
            
            return response.Value;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to delete file from Azure Blob Storage: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Downloads a file from Azure Blob Storage by URL.
    /// </summary>
    public async Task<(byte[] Content, string ContentType)> DownloadFileAsync(string fileUrl)
    {
        try
        {
            var uri = new Uri(fileUrl);
            var blobName = uri.AbsolutePath.TrimStart('/');

            if (blobName.StartsWith(_settings.ContainerName + "/"))
                blobName = blobName.Substring(_settings.ContainerName.Length + 1);

            var containerClient = _blobServiceClient.GetBlobContainerClient(_settings.ContainerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
                throw new InvalidOperationException($"Blob not found: {fileUrl}");

            var download = await blobClient.DownloadContentAsync();
            var contentType = download.Value.Details.ContentType ?? "application/octet-stream";
            return (download.Value.Content.ToArray(), contentType);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to download file from Azure Blob Storage: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Validates if a file extension is allowed
    /// </summary>
    /// <param name="fileName">Name of the file</param>
    /// <returns>True if extension is allowed</returns>
    public bool IsFileExtensionAllowed(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return _settings.AllowedFileExtensions.Contains(extension);
    }

    /// <summary>
    /// Validates if file size is within allowed limit
    /// </summary>
    /// <param name="fileSizeBytes">File size in bytes</param>
    /// <returns>True if size is within limit</returns>
    public bool IsFileSizeAllowed(long fileSizeBytes)
    {
        var maxSizeBytes = _settings.MaxFileSizeMB * 1024 * 1024;
        return fileSizeBytes <= maxSizeBytes;
    }

    /// <summary>
    /// Gets the maximum allowed file size in MB
    /// </summary>
    public int GetMaxFileSizeMB() => _settings.MaxFileSizeMB;

    /// <summary>
    /// Gets the list of allowed file extensions
    /// </summary>
    public List<string> GetAllowedExtensions() => _settings.AllowedFileExtensions;
}


