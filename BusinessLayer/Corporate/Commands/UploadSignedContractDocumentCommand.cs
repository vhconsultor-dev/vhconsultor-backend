using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Shared;
using BusinessLayer.Shared.Services;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command para subir el documento firmado de un contrato a Azure Blob Storage
/// </summary>
public class UploadSignedContractDocumentCommand
{
    private readonly DBcontext _context;
    private readonly AzureBlobStorageService _blobStorageService;

    public UploadSignedContractDocumentCommand(DBcontext context, AzureBlobStorageService blobStorageService)
    {
        _context = context;
        _blobStorageService = blobStorageService;
    }

    /// <summary>
    /// Ejecuta la subida del documento firmado del contrato
    /// </summary>
    /// <param name="contractId">ID del contrato</param>
    /// <param name="file">Archivo PDF del contrato firmado</param>
    /// <param name="lastModifiedBy">Usuario que realiza la subida (opcional)</param>
    /// <returns>Respuesta con la URL del documento subido</returns>
    public async Task<UploadSignedContractDocumentResponse> ExecuteAsync(int contractId, IFormFile file, string? lastModifiedBy = null)
    {
        try
        {
            // 1. Validar que el contrato existe
            var contract = await _context.Contracts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ContractId == contractId);

            if (contract == null)
            {
                throw new KeyNotFoundException(
                    $"No se encontró el contrato con ID {contractId}. " +
                    $"Por favor verifica que el ID del contrato sea correcto.");
            }

            // 2. Validar que se proporcionó un archivo
            if (file == null || file.Length == 0)
            {
                throw new InvalidOperationException(
                    "No se proporcionó ningún archivo o el archivo está vacío. " +
                    "Por favor selecciona un archivo PDF válido para subir.");
            }

            // 3. Validar que el archivo sea PDF
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (fileExtension != ".pdf")
            {
                throw new InvalidOperationException(
                    $"Solo se aceptan archivos PDF para contratos firmados. " +
                    $"El archivo '{file.FileName}' tiene la extensión '{fileExtension}'. " +
                    $"Por favor sube un archivo con extensión .pdf");
            }

            // 4. Validar Content-Type
            if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"El tipo de contenido del archivo no es válido. " +
                    $"Se esperaba 'application/pdf' pero se recibió '{file.ContentType}'. " +
                    $"Por favor asegúrate de subir un archivo PDF válido.");
            }

            // 5. Validar tamaño del archivo (máximo 10 MB)
            if (!_blobStorageService.IsFileSizeAllowed(file.Length))
            {
                var maxSizeMB = _blobStorageService.GetMaxFileSizeMB();
                var fileSizeMB = Math.Round(file.Length / (1024.0 * 1024.0), 2);
                throw new InvalidOperationException(
                    $"El archivo es demasiado grande. " +
                    $"El archivo '{file.FileName}' tiene un tamaño de {fileSizeMB} MB, " +
                    $"pero el tamaño máximo permitido es {maxSizeMB} MB. " +
                    $"Por favor reduce el tamaño del archivo o comprime el PDF.");
            }

            // 6. Generar nombre del archivo: {ContractNumber}_signed.pdf
            var fileName = $"{contract.ContractNumber}_signed.pdf";

            // 7. Subir archivo a Azure Blob Storage
            // Ruta: ContractSignedDocuments/{ContractNumber}/{ContractNumber}_signed.pdf
            string fileUrl;
            try
            {
                using (var stream = file.OpenReadStream())
                {
                    fileUrl = await _blobStorageService.UploadContractDocumentAsync(
                        stream,
                        fileName,
                        contract.ContractNumber,
                        "application/pdf");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error al subir el archivo a Azure Storage. " +
                    $"Archivo: '{file.FileName}', Contrato: '{contract.ContractNumber}'. " +
                    $"Detalles del error: {ex.Message}", ex);
            }

            // 8. Validar que la URL no exceda el límite de la base de datos
            if (fileUrl.Length > 500)
            {
                throw new InvalidOperationException(
                    $"La URL del archivo generada es demasiado larga ({fileUrl.Length} caracteres). " +
                    $"El límite máximo es 500 caracteres. " +
                    $"Por favor contacta al administrador del sistema.");
            }

            // 9. Actualizar solo el campo SignedDocumentUrl del contrato
            var contractToUpdate = await _context.Contracts.FindAsync(contractId);
            
            if (contractToUpdate == null)
            {
                // Intentar eliminar el archivo subido si el contrato ya no existe
                try
                {
                    await _blobStorageService.DeleteFileAsync(fileUrl);
                }
                catch
                {
                    // Ignorar errores al intentar limpiar
                }
                
                throw new KeyNotFoundException(
                    $"El contrato con ID {contractId} ya no existe en la base de datos. " +
                    $"El archivo no se guardó.");
            }

            contractToUpdate.SignedDocumentUrl = fileUrl;
            contractToUpdate.LastModifiedBy = lastModifiedBy;
            contractToUpdate.UpdatedAt = DateTimeService.GetCostaRicaNow();

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Intentar eliminar el archivo subido si falla la actualización
                try
                {
                    await _blobStorageService.DeleteFileAsync(fileUrl);
                }
                catch
                {
                    // Ignorar errores al intentar limpiar
                }

                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                throw new InvalidOperationException(
                    $"Error al guardar la URL del documento en la base de datos. " +
                    $"Contrato ID: {contractId}. " +
                    $"Detalles: {innerMessage}", ex);
            }

            // 10. Retornar respuesta exitosa
            return new UploadSignedContractDocumentResponse
            {
                Success = true,
                Message = $"El documento firmado del contrato '{contract.ContractNumber}' se subió exitosamente",
                ContractId = contractId,
                ContractNumber = contract.ContractNumber,
                SignedDocumentUrl = fileUrl,
                FileName = fileName,
                FileSizeMB = Math.Round(file.Length / (1024.0 * 1024.0), 2),
                UploadedAt = DateTimeService.GetCostaRicaNow()
            };
        }
        catch (KeyNotFoundException)
        {
            throw; // Re-lanzar excepciones de negocio
        }
        catch (InvalidOperationException)
        {
            throw; // Re-lanzar excepciones de validación
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error inesperado al subir el documento firmado del contrato. " +
                $"Contrato ID: {contractId}. " +
                $"Error: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Respuesta del comando de subida de documento firmado
/// </summary>
public class UploadSignedContractDocumentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string SignedDocumentUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public double FileSizeMB { get; set; }
    public DateTime UploadedAt { get; set; }
}
