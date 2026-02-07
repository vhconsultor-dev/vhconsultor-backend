using ApplicationLayer.Corporate;
using ApplicationLayer.Shared;
using ApiLayer.Tools;
using BusinessLayer.Corporate.Commands;
using BusinessLayer.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ApiLayer.Controllers.Corporate;

/// <summary>
/// Controlador para gestionar Contracts (CRUD Completo)
/// </summary>
[ApiController]
[Route("api/corporate/[controller]")]
[Authorize]
public class ContractController : ControllerBase
{
    private readonly ContractService _contractService;
    private readonly ValidationService _validationService;
    private readonly CraftMyPdfService _craftMyPdfService;
    private readonly AzureStorageSettings _azureStorageSettings;
    private readonly SendGridService _sendGridService;
    private readonly SendGridSettings _sendGridSettings;

    public ContractController(
        ContractService contractService,
        ValidationService validationService,
        CraftMyPdfService craftMyPdfService,
        IOptions<AzureStorageSettings> azureStorageSettings,
        SendGridService sendGridService,
        IOptions<SendGridSettings> sendGridSettings)
    {
        _contractService = contractService;
        _validationService = validationService;
        _craftMyPdfService = craftMyPdfService;
        _azureStorageSettings = azureStorageSettings.Value;
        _sendGridService = sendGridService;
        _sendGridSettings = sendGridSettings.Value;
    }

    #region POST - Create Contract

    /// <summary>
    /// Crea un nuevo Contract
    /// </summary>
    /// <param name="request">Datos del contrato</param>
    /// <returns>ID del contrato creado</returns>
    [HttpPost]
    public async Task<IActionResult> CreateContract([FromBody] CreateContractRequest request)
    {
        // Validación usando FluentValidation
        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var response = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(response);
        }

        try
        {
            // ContractId es auto-generado (IDENTITY), no se valida existencia previa
            var contractId = await _contractService.CreateContractAsync(request);
            
            var successResponse = ResponseStructure<int>.Success(
                contractId, 
                "Contrato creado exitosamente");
            
            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al crear el contrato: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region PUT - Update Contract

    /// <summary>
    /// Actualiza un Contract existente
    /// </summary>
    /// <param name="contractId">ID del contrato a actualizar</param>
    /// <param name="request">Datos actualizados del contrato</param>
    /// <returns>Resultado de la operación</returns>
    [HttpPut("{contractId}")]
    public async Task<IActionResult> UpdateContract(int contractId, [FromBody] UpdateContractRequest request)
    {
        // Validación usando FluentValidation
        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var response = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(response);
        }

        try
        {
            // Verificar si el contrato existe
            var existingContract = await _contractService.GetContractByIdAsync(contractId);
            if (existingContract == null)
            {
                var response = ResponseStructure<object>.Error(
                    $"No se encontró el contrato con ID {contractId}", 
                    404);
                return NotFound(response);
            }

            var result = await _contractService.UpdateContractAsync(contractId, request);
            
            if (result)
            {
                var successResponse = ResponseStructure<bool>.Success(
                    true, 
                    "Contrato actualizado exitosamente");
                return Ok(successResponse);
            }
            else
            {
                var errorResponse = ResponseStructure<object>.Error(
                    "No se pudo actualizar el contrato", 
                    500);
                return StatusCode(500, errorResponse);
            }
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al actualizar el contrato: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region DELETE - Delete Contract

    /// <summary>
    /// Elimina (cancela) un Contract
    /// </summary>
    /// <param name="contractId">ID del contrato a eliminar</param>
    /// <param name="deletedBy">Usuario que elimina (opcional)</param>
    /// <returns>Resultado de la operación</returns>
    [HttpDelete("{contractId}")]
    public async Task<IActionResult> DeleteContract(int contractId, [FromQuery] string? deletedBy = null)
    {
        try
        {
            // Verificar si el contrato existe
            var existingContract = await _contractService.GetContractByIdAsync(contractId);
            if (existingContract == null)
            {
                var response = ResponseStructure<object>.Error(
                    $"Contract with ID {contractId} not found", 
                    404);
                return NotFound(response);
            }

            var result = await _contractService.DeleteContractAsync(contractId, deletedBy);
            
            if (result)
            {
                var successResponse = ResponseStructure<bool>.Success(
                    true, 
                    $"Contract '{existingContract.ContractNumber}' has been successfully cancelled");
                return Ok(successResponse);
            }
            else
            {
                var errorResponse = ResponseStructure<object>.Error(
                    "Unable to cancel the contract", 
                    500);
                return StatusCode(500, errorResponse);
            }
        }
        catch (InvalidOperationException ex)
        {
            // Error de validación de negocio (ej: tiene facturas asociadas)
            var errorResponse = ResponseStructure<object>.ValidationError(
                ex.Message);
            return BadRequest(errorResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"An unexpected error occurred while cancelling the contract: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region GET - Get Contracts (Flexible Search)

    /// <summary>
    /// Obtiene contracts con búsqueda flexible usando parámetros opcionales
    /// </summary>
    /// <param name="contractId">ID del contrato</param>
    /// <param name="customerId">ID del customer</param>
    /// <param name="contractNumber">Número de contrato para búsqueda parcial</param>
    /// <param name="status">Estado del contrato</param>
    /// <param name="contractTypeId">ID del tipo de contrato</param>
    /// <param name="currencyCode">Código de moneda</param>
    /// <param name="startDateFrom">Fecha de inicio desde</param>
    /// <param name="startDateTo">Fecha de inicio hasta</param>
    /// <param name="endDateFrom">Fecha de fin desde</param>
    /// <param name="endDateTo">Fecha de fin hasta</param>
    /// <returns>Lista de contracts que coinciden con los criterios</returns>
    [HttpGet]
    public async Task<IActionResult> GetContracts(
        [FromQuery] int? contractId = null,
        [FromQuery] int? customerId = null,
        [FromQuery] string? contractNumber = null,
        [FromQuery] string? status = null,
        [FromQuery] int? contractTypeId = null,
        [FromQuery] string? currencyCode = null,
        [FromQuery] DateTime? startDateFrom = null,
        [FromQuery] DateTime? startDateTo = null,
        [FromQuery] DateTime? endDateFrom = null,
        [FromQuery] DateTime? endDateTo = null)
    {
        try
        {
            // Si se especifica ID, devolver solo ese contrato
            if (contractId.HasValue)
            {
                var contract = await _contractService.GetContractByIdAsync(contractId.Value);
                
                if (contract == null)
                {
                    var notFoundResponse = ResponseStructure<object>.Error(
                        "Contrato no encontrado", 
                        404);
                    return NotFound(notFoundResponse);
                }

                var singleResponse = ResponseStructure<ModelLayer.Corporate.Entities.Contract>.Success(
                    contract, 
                    "Contrato obtenido exitosamente");
                
                return Ok(singleResponse);
            }

            // Si se especifica customerId, obtener contratos del customer
            if (customerId.HasValue)
            {
                var customerContracts = await _contractService.GetContractsByCustomerIdAsync(customerId.Value);
                
                var customerResponse = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.Contract>>.Success(
                    customerContracts, 
                    "Contratos del customer obtenidos exitosamente");
                
                return Ok(customerResponse);
            }

            // Devolver todos los contratos con los filtros aplicados
            var contracts = await _contractService.GetContractsAsync(
                contractId, customerId, contractNumber, status, contractTypeId,
                currencyCode, startDateFrom, startDateTo, endDateFrom, endDateTo);
            
            var response = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.Contract>>.Success(
                contracts, 
                "Contratos obtenidos exitosamente");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener los contratos: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region GET - Get Active Contracts

    /// <summary>
    /// Obtiene todos los contratos activos
    /// </summary>
    /// <returns>Lista de contratos activos</returns>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveContracts()
    {
        try
        {
            var contracts = await _contractService.GetActiveContractsAsync();
            
            var response = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.Contract>>.Success(
                contracts, 
                "Contratos activos obtenidos exitosamente");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener los contratos activos: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region POST - Upload Signed Contract Document

    /// <summary>
    /// Sube el documento PDF firmado de un contrato a Azure Storage
    /// </summary>
    /// <param name="contractId">ID del contrato</param>
    /// <param name="file">Archivo PDF del contrato firmado</param>
    /// <param name="lastModifiedBy">Usuario que realiza la subida (opcional)</param>
    /// <returns>Resultado de la operación con la URL del documento</returns>
    /// <remarks>
    /// Este endpoint permite subir el contrato firmado por el cliente.
    /// 
    /// Validaciones:
    /// - Solo se aceptan archivos PDF
    /// - Tamaño máximo: 10 MB
    /// - El contrato debe existir en la base de datos
    /// 
    /// El archivo se almacena en Azure Blob Storage en la ruta:
    /// InvoicesAttachment/{ContractNumber}/signed_contract_{timestamp}.pdf
    /// 
    /// Solo se actualiza el campo SignedDocumentUrl del contrato, los demás campos permanecen sin cambios.
    /// </remarks>
    [HttpPost("{contractId}/upload-signed-document")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = 10485760)] // 10 MB
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadSignedDocument(
        int contractId, 
        IFormFile file, 
        [FromQuery] string? lastModifiedBy = null)
    {
        try
        {
            // Validación temprana del archivo
            if (file == null || file.Length == 0)
            {
                var validationResponse = ResponseStructure<object>.ValidationError(
                    "No se proporcionó ningún archivo o el archivo está vacío. " +
                    "Por favor selecciona un archivo PDF válido para subir.");
                return BadRequest(validationResponse);
            }

            // Validar extensión del archivo
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (fileExtension != ".pdf")
            {
                var validationResponse = ResponseStructure<object>.ValidationError(
                    $"Solo se aceptan archivos PDF para contratos firmados. " +
                    $"El archivo '{file.FileName}' tiene la extensión '{fileExtension}'. " +
                    $"Por favor sube un archivo con extensión .pdf");
                return BadRequest(validationResponse);
            }

            var result = await _contractService.UploadSignedDocumentAsync(contractId, file, lastModifiedBy);

            var successResponse = ResponseStructure<UploadSignedContractDocumentResponse>.Success(
                result,
                result.Message);

            return Ok(successResponse);
        }
        catch (KeyNotFoundException ex)
        {
            var errorResponse = ResponseStructure<object>.Error(ex.Message, 404);
            return NotFound(errorResponse);
        }
        catch (InvalidOperationException ex)
        {
            var errorResponse = ResponseStructure<object>.ValidationError(ex.Message);
            return BadRequest(errorResponse);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al guardar en la base de datos: {innerMessage}",
                500);
            return StatusCode(500, errorResponse);
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? string.Empty;
            var fullMessage = $"Error inesperado al subir el documento firmado del contrato: {ex.Message}";
            if (!string.IsNullOrEmpty(innerMessage))
                fullMessage += $" | Detalles adicionales: {innerMessage}";
            
            var errorResponse = ResponseStructure<object>.Error(fullMessage, 500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region POST - Generate Contract PDF

    /// <summary>
    /// Genera el PDF de un contrato usando CraftMyPDF
    /// </summary>
    /// <param name="request">Datos del contrato para generar el PDF</param>
    /// <returns>Archivo PDF del contrato</returns>
    [HttpPost("generate-pdf")]
    public async Task<IActionResult> GenerateContractPdf([FromBody] GenerateContractPdfRequest request)
    {
        // Validación usando FluentValidation
        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var response = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(response);
        }

        try
        {
            // Generar el PDF usando CraftMyPDF
            var pdfBytes = await _craftMyPdfService.GeneratePdfAsync(request.TemplateId, request.Data);

            // Retornar el PDF como archivo descargable
            var fileName = $"contrato_{request.Data.FullName.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
            
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al generar el PDF del contrato: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region GET - Get Contract Document URL with SAS

    /// <summary>
    /// Obtiene la URL del documento del contrato con token SAS para descarga
    /// </summary>
    /// <param name="documentUrl">URL del documento en Azure Storage</param>
    /// <returns>URL del documento con token SAS para descarga</returns>
    /// <remarks>
    /// Este endpoint agrega el token SAS a la URL del documento para permitir su descarga.
    /// La URL debe ser válida y pertenecer al contenedor de Azure Storage configurado.
    /// </remarks>
    [HttpGet("document-url")]
    public IActionResult GetContractDocumentUrlWithSas([FromQuery] string documentUrl)
    {
        try
        {
            // Validar que se proporcionó la URL
            if (string.IsNullOrWhiteSpace(documentUrl))
            {
                var validationResponse = ResponseStructure<object>.ValidationError(
                    "La URL del documento es requerida. Por favor proporciona el parámetro 'documentUrl' en la consulta.");
                return BadRequest(validationResponse);
            }

            // Validar que la URL sea válida
            if (!Uri.TryCreate(documentUrl, UriKind.Absolute, out var uri) || 
                (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                var validationResponse = ResponseStructure<object>.ValidationError(
                    $"La URL proporcionada no es válida: '{documentUrl}'. " +
                    $"Por favor proporciona una URL válida de Azure Storage.");
                return BadRequest(validationResponse);
            }

            // Validar que la URL pertenezca al contenedor correcto
            var expectedContainerPath = $"/{_azureStorageSettings.ContainerName}/";
            if (!uri.AbsolutePath.Contains(expectedContainerPath, StringComparison.OrdinalIgnoreCase))
            {
                var validationResponse = ResponseStructure<object>.ValidationError(
                    $"La URL proporcionada no pertenece al contenedor de Azure Storage configurado. " +
                    $"Se esperaba que la URL contenga el contenedor '{_azureStorageSettings.ContainerName}'. " +
                    $"URL recibida: '{documentUrl}'");
                return BadRequest(validationResponse);
            }

            // Validar que el SAS token esté configurado
            if (string.IsNullOrWhiteSpace(_azureStorageSettings.BlobSasToken))
            {
                var errorResponse = ResponseStructure<object>.Error(
                    "El token SAS de Azure Storage no está configurado. " +
                    "Por favor contacta al administrador del sistema.",
                    500);
                return StatusCode(500, errorResponse);
            }

            // Concatenar el SAS token a la URL
            // Si la URL ya tiene query parameters, usar &, si no, usar ?
            var separator = uri.Query.Length > 0 ? "&" : "?";
            var urlWithSas = $"{documentUrl}{separator}{_azureStorageSettings.BlobSasToken}";

            var response = new
            {
                documentUrl = documentUrl,
                urlWithSas = urlWithSas,
                expiresAt = "2030-01-02T02:38:09Z" // Fecha de expiración del SAS (puedes hacerlo dinámico si lo necesitas)
            };

            var successResponse = ResponseStructure<object>.Success(
                response,
                "URL con token SAS generada exitosamente");

            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error inesperado al generar la URL con SAS: {ex.Message}",
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region POST - Send Contract Email

    /// <summary>
    /// Envía el contrato por correo electrónico al cliente usando SendGrid
    /// </summary>
    /// <param name="request">Datos del correo y contrato</param>
    /// <param name="pdfFile">Archivo PDF del contrato (opcional)</param>
    /// <returns>Resultado de la operación</returns>
    /// <remarks>
    /// Este endpoint envía un correo al cliente con el contrato para su firma.
    /// 
    /// Datos requeridos:
    /// - toEmail: Correo del destinatario
    /// - data: Información del contrato (nombre completo, identificación, número de contrato, etc.)
    /// 
    /// Opcionales:
    /// - ccEmail: Correo en copia (CC)
    /// - pdfFile: Archivo PDF adjunto del contrato
    /// 
    /// El correo usa una plantilla de SendGrid configurada en las variables de entorno.
    /// </remarks>
    [HttpPost("send-contract-email")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = 10485760)] // 10 MB
    public async Task<IActionResult> SendContractEmail(
        [FromForm] string requestJson,
        IFormFile? pdfFile = null)
    {
        try
        {
            // Deserializar el request JSON del form-data
            SendContractEmailRequest? request;
            try
            {
                request = System.Text.Json.JsonSerializer.Deserialize<SendContractEmailRequest>(
                    requestJson,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch (Exception ex)
            {
                var parseErrorResponse = ResponseStructure<object>.ValidationError(
                    $"Error al procesar los datos del correo: {ex.Message}. " +
                    $"Por favor verifica que el JSON sea válido.");
                return BadRequest(parseErrorResponse);
            }

            if (request == null)
            {
                var validationResponse = ResponseStructure<object>.ValidationError(
                    "Los datos del correo son requeridos. Por favor proporciona 'requestJson' con los datos.");
                return BadRequest(validationResponse);
            }

            // Validación usando FluentValidation
            var validationResult = await _validationService.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var response = ResponseStructure<object>.ValidationError(
                    string.Join(", ", validationResult.Errors));
                return BadRequest(response);
            }

            // Validar archivo PDF si se proporciona
            byte[]? pdfContent = null;
            string? pdfFileName = null;

            if (pdfFile != null)
            {
                // Validar que sea un PDF
                var fileExtension = Path.GetExtension(pdfFile.FileName).ToLowerInvariant();
                if (fileExtension != ".pdf")
                {
                    var validationResponse = ResponseStructure<object>.ValidationError(
                        $"Solo se aceptan archivos PDF como adjunto. " +
                        $"El archivo '{pdfFile.FileName}' tiene la extensión '{fileExtension}'.");
                    return BadRequest(validationResponse);
                }

                // Validar tamaño (10 MB máximo)
                if (pdfFile.Length > 10485760)
                {
                    var validationResponse = ResponseStructure<object>.ValidationError(
                        $"El archivo PDF es demasiado grande. Tamaño máximo permitido: 10 MB. " +
                        $"Tamaño del archivo: {pdfFile.Length / 1024 / 1024:F2} MB.");
                    return BadRequest(validationResponse);
                }

                // Leer el contenido del archivo
                using (var memoryStream = new MemoryStream())
                {
                    await pdfFile.CopyToAsync(memoryStream);
                    pdfContent = memoryStream.ToArray();
                }
                pdfFileName = pdfFile.FileName;
            }

            // Validar que el template ID esté configurado
            if (string.IsNullOrWhiteSpace(_sendGridSettings.ContractTemplateId))
            {
                var errorResponse = ResponseStructure<object>.Error(
                    "El template de SendGrid para contratos no está configurado. " +
                    "Por favor contacta al administrador del sistema.",
                    500);
                return StatusCode(500, errorResponse);
            }

            // Preparar los datos del template para SendGrid
            // SendGrid espera que los nombres de las variables coincidan exactamente con el template
            // Crear un objeto con los nombres en camelCase (formato estándar de SendGrid)
            // Si el template usa otros nombres, ajustar aquí
            var templateData = new Dictionary<string, object>
            {
                { "fullName", request.Data.FullName },
                { "identification", request.Data.Identification },
                { "contractNumber", request.Data.ContractNumber },
                { "companyName", request.Data.CompanyName },
                { "day", request.Data.Day },
                { "month", request.Data.Month },
                { "year", request.Data.Year }
            };

            // Enviar el correo usando SendGrid
            var emailResult = await _sendGridService.SendTemplateEmailAsync(
                request.ToEmail,
                _sendGridSettings.ContractTemplateId,
                templateData,
                request.CcEmail,
                pdfContent,
                pdfFileName);

            if (emailResult.Success)
            {
                var response = new SendContractEmailResponse
                {
                    Success = true,
                    Message = "Correo enviado exitosamente al cliente"
                };

                var successResponse = ResponseStructure<SendContractEmailResponse>.Success(
                    response,
                    "Correo enviado exitosamente");

                return Ok(successResponse);
            }
            else
            {
                var errorResponse = ResponseStructure<object>.Error(
                    $"No se pudo enviar el correo: {emailResult.Message}. " +
                    $"Detalles: {emailResult.ErrorDetails}",
                    500);
                return StatusCode(500, errorResponse);
            }
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error inesperado al enviar el correo del contrato: {ex.Message}",
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion
}

