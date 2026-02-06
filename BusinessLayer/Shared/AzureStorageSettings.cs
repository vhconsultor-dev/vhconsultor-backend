namespace BusinessLayer.Shared;

/// <summary>
/// Settings for Azure Blob Storage configuration
/// </summary>
public class AzureStorageSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public string InvoiceAttachmentsFolder { get; set; } = string.Empty;
    public string BlobSasToken { get; set; } = string.Empty;
    public int MaxFileSizeMB { get; set; } = 10;
    public List<string> AllowedFileExtensions { get; set; } = new();
}



