using ApplicationLayer.Corporate;
using ApplicationLayer.Shared;
using ApiLayer.Tools;
using BusinessLayer.Corporate.Commands;
using BusinessLayer.Corporate.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ApiLayer.Controllers.Corporate;

/// <summary>
/// Controlador para gestionar Facturas (Invoices)
/// </summary>
[ApiController]
[Route("api/corporate/[controller]")]
[Authorize]
public class InvoiceController : ControllerBase
{
    private readonly InvoiceService _invoiceService;
    private readonly ValidationService _validationService;

    public InvoiceController(
        InvoiceService invoiceService,
        ValidationService validationService)
    {
        _invoiceService = invoiceService;
        _validationService = validationService;
    }

    #region POST - Generate Invoices

    /// <summary>
    /// Genera todas las facturas automáticamente para un contrato de monto fijo (FeeTypeId = 1)
    /// </summary>
    /// <param name="contractId">ID del contrato</param>
    /// <returns>Resultado de la generación</returns>
    /// <remarks>
    /// This endpoint generates all invoices for fixed amount contracts.
    /// Each invoice will have the same amount (contract's FeeAmount).
    /// If invoices already exist, it will generate only the missing ones.
    /// </remarks>
    [HttpPost("generate-fixed/{contractId}")]
    public async Task<IActionResult> GenerateFixedInvoices(int contractId)
    {
        var request = new GenerateInvoicesRequest { ContractId = contractId };

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
            var result = await _invoiceService.GenerateInvoicesAsync(request);

            if (!result.Success)
            {
                // Ya existen facturas
                var warningResponse = ResponseStructure<GenerateInvoicesResponse>.ValidationError(
                    result.Message);
                warningResponse.Data = result;
                return BadRequest(warningResponse);
            }

            var successResponse = ResponseStructure<GenerateInvoicesResponse>.Success(
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
            // Capturar el inner exception para más detalles
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            var fullMessage = $"Error al guardar en la base de datos: {innerMessage}";
            
            var errorResponse = ResponseStructure<object>.Error(fullMessage, 500);
            return StatusCode(500, errorResponse);
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? string.Empty;
            var fullMessage = $"Error al generar facturas: {ex.Message}";
            if (!string.IsNullOrEmpty(innerMessage))
                fullMessage += $" | Inner: {innerMessage}";
                
            var errorResponse = ResponseStructure<object>.Error(fullMessage, 500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region POST - Create Manual Invoice (Percentage Contracts)

    /// <summary>
    /// Crea una factura manualmente para contratos de porcentaje (FeeTypeId = 2)
    /// </summary>
    /// <param name="request">Datos de la factura</param>
    /// <returns>Resultado de la creación</returns>
    [HttpPost("create-manual")]
    public async Task<IActionResult> CreateManualInvoice([FromBody] CreateManualInvoiceRequest request)
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
            var result = await _invoiceService.CreateManualInvoiceAsync(request);

            var successResponse = ResponseStructure<CreateManualInvoiceResponse>.Success(
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
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? string.Empty;
            var fullMessage = $"Error creating manual invoice: {ex.Message}";
            if (!string.IsNullOrEmpty(innerMessage))
                fullMessage += $" | Inner: {innerMessage}";
                
            var errorResponse = ResponseStructure<object>.Error(fullMessage, 500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region POST - Generate Percentage Invoices

    /// <summary>
    /// Genera todas las facturas automáticamente con monto 0 para contratos de porcentaje (FeeTypeId = 2)
    /// </summary>
    /// <param name="contractId">ID del contrato</param>
    /// <returns>Resultado de la generación</returns>
    /// <remarks>
    /// This endpoint generates all invoice periods for percentage contracts with zero amounts.
    /// The user can then manually edit each invoice to set the correct amount.
    /// </remarks>
    [HttpPost("generate-percentage/{contractId}")]
    public async Task<IActionResult> GeneratePercentageInvoices(int contractId)
    {
        var request = new GeneratePercentageInvoicesRequest { ContractId = contractId };

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
            var result = await _invoiceService.GeneratePercentageInvoicesAsync(request);

            if (!result.Success)
            {
                var warningResponse = ResponseStructure<GeneratePercentageInvoicesResponse>.ValidationError(
                    result.Message);
                warningResponse.Data = result;
                return BadRequest(warningResponse);
            }

            var successResponse = ResponseStructure<GeneratePercentageInvoicesResponse>.Success(
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
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? string.Empty;
            var fullMessage = $"Error generating percentage invoices: {ex.Message}";
            if (!string.IsNullOrEmpty(innerMessage))
                fullMessage += $" | Inner: {innerMessage}";
                
            var errorResponse = ResponseStructure<object>.Error(fullMessage, 500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region PUT - Update Invoice

    /// <summary>
    /// Actualiza una factura (solo si no está pagada)
    /// </summary>
    /// <param name="invoiceId">ID de la factura</param>
    /// <param name="request">Datos actualizados</param>
    /// <returns>Resultado de la actualización</returns>
    [HttpPut("{invoiceId}")]
    public async Task<IActionResult> UpdateInvoice(int invoiceId, [FromBody] UpdateInvoiceRequest request)
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
            var result = await _invoiceService.UpdateInvoiceAsync(invoiceId, request);

            var successResponse = ResponseStructure<UpdateInvoiceResponse>.Success(
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
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? string.Empty;
            var fullMessage = $"Error updating invoice: {ex.Message}";
            if (!string.IsNullOrEmpty(innerMessage))
                fullMessage += $" | Inner: {innerMessage}";
                
            var errorResponse = ResponseStructure<object>.Error(fullMessage, 500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region PUT - Mark Invoice as Paid

    /// <summary>
    /// Marca una factura como pagada
    /// </summary>
    /// <param name="invoiceId">ID de la factura</param>
    /// <param name="request">Datos del pago</param>
    /// <returns>Resultado de la operación</returns>
    [HttpPut("{invoiceId}/mark-as-paid")]
    public async Task<IActionResult> MarkInvoiceAsPaid(int invoiceId, [FromBody] MarkInvoiceAsPaidRequest request)
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
            // Obtener UserId: primero del request, si no del token JWT
            int userId = 0;
            
            if (request.UserId.HasValue && request.UserId.Value > 0)
            {
                userId = request.UserId.Value;
            }
            else
            {
                // Intentar obtener del token JWT
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out userId) || userId <= 0)
                {
                    var errorResponse = ResponseStructure<object>.ValidationError(
                        "User ID is required. Please provide 'userId' in the request body or ensure your authentication token includes a valid user ID.");
                    return BadRequest(errorResponse);
                }
            }

            var result = await _invoiceService.MarkInvoiceAsPaidAsync(invoiceId, request, userId);

            var successResponse = ResponseStructure<MarkInvoiceAsPaidResponse>.Success(
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
            // Capturar el inner exception para más detalles
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            var fullMessage = $"Database error while marking invoice as paid. InvoiceId: {invoiceId}. Error: {innerMessage}";
            
            var errorResponse = ResponseStructure<object>.Error(fullMessage, 500);
            return StatusCode(500, errorResponse);
        }
        catch (Exception ex)
        {
            // Capturar el inner exception para más detalles
            var innerMessage = ex.InnerException?.Message ?? string.Empty;
            var fullMessage = $"Error al marcar factura como pagada. InvoiceId: {invoiceId}. Error: {ex.Message}";
            if (!string.IsNullOrEmpty(innerMessage))
                fullMessage += $" | Inner exception: {innerMessage}";
            
            var errorResponse = ResponseStructure<object>.Error(fullMessage, 500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region GET - Query Invoices (Unified)

    /// <summary>
    /// Obtiene facturas con filtros opcionales (Query unificado)
    /// </summary>
    /// <param name="invoiceId">Filtro por ID de factura</param>
    /// <param name="contractId">Filtro por ID de contrato</param>
    /// <param name="invoiceNumber">Filtro por número de factura (búsqueda parcial)</param>
    /// <param name="status">Filtro por estado (Draft, Sent, Paid, Overdue, Cancelled)</param>
    /// <param name="paymentStatus">Filtro por estado de pago (Unpaid, Paid)</param>
    /// <param name="currencyCode">Filtro por código de moneda</param>
    /// <param name="invoiceDateFrom">Filtro por fecha de emisión desde</param>
    /// <param name="invoiceDateTo">Filtro por fecha de emisión hasta</param>
    /// <param name="dueDateFrom">Filtro por fecha de vencimiento desde</param>
    /// <param name="dueDateTo">Filtro por fecha de vencimiento hasta</param>
    /// <param name="isOverdue">Filtro por facturas vencidas</param>
    /// <param name="orderBy">Ordenamiento (InvoiceDate, DueDate, Total, Status)</param>
    /// <param name="pageNumber">Número de página (para paginación)</param>
    /// <param name="pageSize">Tamaño de página (para paginación)</param>
    /// <returns>Lista de facturas filtradas</returns>
    /// <remarks>
    /// Este es un endpoint unificado que maneja todos los escenarios de consulta:
    /// - Obtener todas las facturas
    /// - Obtener facturas de un contrato específico
    /// - Buscar por número de factura
    /// - Filtrar por estado, moneda, fechas, etc.
    /// - Paginación y ordenamiento
    /// 
    /// Ejemplos de uso:
    /// - GET /api/corporate/invoice?contractId=CONT-001 (todas las facturas de un contrato)
    /// - GET /api/corporate/invoice?status=Draft (todas las facturas en borrador)
    /// - GET /api/corporate/invoice?isOverdue=true (facturas vencidas)
    /// - GET /api/corporate/invoice?contractId=CONT-001&amp;pageNumber=1&amp;pageSize=10 (con paginación)
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] int? invoiceId = null,
        [FromQuery] int? contractId = null,
        [FromQuery] string? invoiceNumber = null,
        [FromQuery] string? status = null,
        [FromQuery] string? paymentStatus = null,
        [FromQuery] string? currencyCode = null,
        [FromQuery] DateTime? invoiceDateFrom = null,
        [FromQuery] DateTime? invoiceDateTo = null,
        [FromQuery] DateTime? dueDateFrom = null,
        [FromQuery] DateTime? dueDateTo = null,
        [FromQuery] bool? isOverdue = null,
        [FromQuery] string? orderBy = null,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
    {
        try
        {
            var filter = new InvoiceQueryFilter
            {
                InvoiceId = invoiceId,
                ContractId = contractId,
                InvoiceNumber = invoiceNumber,
                Status = status,
                PaymentStatus = paymentStatus,
                CurrencyCode = currencyCode,
                InvoiceDateFrom = invoiceDateFrom,
                InvoiceDateTo = invoiceDateTo,
                DueDateFrom = dueDateFrom,
                DueDateTo = dueDateTo,
                IsOverdue = isOverdue,
                OrderBy = orderBy,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _invoiceService.GetInvoicesAsync(filter);

            var successResponse = ResponseStructure<InvoiceQueryResult>.Success(
                result,
                $"Se encontraron {result.TotalRecords} factura(s)");

            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener facturas: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region GET - Invoice Detail by ID

    /// <summary>
    /// Obtiene el detalle completo de una factura por ID (incluye items y adjuntos)
    /// </summary>
    /// <param name="invoiceId">ID de la factura</param>
    /// <returns>Detalle de la factura</returns>
    [HttpGet("{invoiceId}")]
    public async Task<IActionResult> GetInvoiceById(int invoiceId)
    {
        try
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId);

            if (invoice == null)
            {
                var errorResponse = ResponseStructure<object>.Error(
                    $"Factura con ID {invoiceId} no encontrada", 404);
                return NotFound(errorResponse);
            }

            var successResponse = ResponseStructure<InvoiceDetailDto>.Success(
                invoice,
                "Factura obtenida exitosamente");

            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener factura: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region GET - Invoice Summary by Contract

    /// <summary>
    /// Obtiene el resumen de facturas de un contrato
    /// </summary>
    /// <param name="contractId">ID del contrato</param>
    /// <returns>Resumen de facturas (totales, pagadas, pendientes, vencidas)</returns>
    [HttpGet("summary/{contractId}")]
    public async Task<IActionResult> GetInvoiceSummaryByContract(int contractId)
    {
        try
        {
            var summary = await _invoiceService.GetInvoiceSummaryByContractAsync(contractId);

            var successResponse = ResponseStructure<InvoiceSummaryDto>.Success(
                summary,
                "Resumen de facturas obtenido exitosamente");

            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener resumen de facturas: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region DELETE - Delete Invoices

    /// <summary>
    /// Deletes a single invoice or all invoices from a contract
    /// </summary>
    /// <param name="invoiceId">ID of the specific invoice to delete (optional)</param>
    /// <param name="contractId">ID of the contract to delete all invoices from (optional)</param>
    /// <returns>Result of the operation</returns>
    /// <remarks>
    /// You must provide either invoiceId OR contractId, not both.
    /// - invoiceId: Deletes a specific invoice
    /// - contractId: Deletes ALL invoices from a contract
    /// 
    /// Validations:
    /// - Invoice(s) must not be paid (PaymentStatus != "Paid")
    /// - Invoice(s) must not have attachments
    /// </remarks>
    [HttpDelete]
    public async Task<IActionResult> DeleteInvoices(
        [FromQuery] int? invoiceId = null,
        [FromQuery] int? contractId = null)
    {
        try
        {
            // Validar que se proporcione uno y solo uno de los parámetros
            if (!invoiceId.HasValue && !contractId.HasValue)
            {
                var validationResponse = ResponseStructure<object>.ValidationError(
                    "You must provide either 'invoiceId' to delete a specific invoice or 'contractId' to delete all invoices from a contract");
                return BadRequest(validationResponse);
            }

            if (invoiceId.HasValue && contractId.HasValue)
            {
                var validationResponse = ResponseStructure<object>.ValidationError(
                    "You cannot provide both 'invoiceId' and 'contractId'. Please provide only one parameter");
                return BadRequest(validationResponse);
            }

            DeleteInvoicesResponse result;

            // Eliminar una factura específica
            if (invoiceId.HasValue)
            {
                result = await _invoiceService.DeleteSingleInvoiceAsync(invoiceId.Value);
            }
            // Eliminar todas las facturas de un contrato
            else
            {
                result = await _invoiceService.DeleteAllContractInvoicesAsync(contractId!.Value);
            }

            var successResponse = ResponseStructure<DeleteInvoicesResponse>.Success(
                result,
                result.Message);

            return Ok(successResponse);
        }
        catch (KeyNotFoundException ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                ex.Message,
                404);
            return NotFound(errorResponse);
        }
        catch (InvalidOperationException ex)
        {
            // Error de validación de negocio (factura pagada o con adjuntos)
            var errorResponse = ResponseStructure<object>.ValidationError(
                ex.Message);
            return BadRequest(errorResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"An unexpected error occurred while deleting invoice(s): {ex.Message}",
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region POST - Upload Invoice Attachment

    /// <summary>
    /// Uploads a file attachment to an invoice
    /// </summary>
    /// <param name="invoiceId">ID of the invoice</param>
    /// <param name="file">File to upload</param>
    /// <param name="userId">User ID (optional, if not provided will try to get from JWT token)</param>
    /// <returns>Result of the upload operation</returns>
    /// <remarks>
    /// Allowed file types: PDF, JPG, JPEG, PNG, GIF, DOC, DOCX, XLS, XLSX, TXT, CSV
    /// Maximum file size: 10 MB
    /// Files are stored in Azure Blob Storage under: InvoicesAttachment/{InvoiceNumber}/
    /// </remarks>
    [HttpPost("{invoiceId}/attachments")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = 10485760)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAttachment(int invoiceId, IFormFile file, [FromQuery] int? userId = null)
    {
        try
        {
            // Validación temprana del archivo
            if (file == null || file.Length == 0)
            {
                var validationResponse = ResponseStructure<object>.ValidationError(
                    "No file was provided or the file is empty");
                return BadRequest(validationResponse);
            }

            // Obtener UserId: primero del query parameter, si no del token JWT
            int finalUserId = 0;
            
            if (userId.HasValue && userId.Value > 0)
            {
                finalUserId = userId.Value;
            }
            else
            {
                // Intentar obtener del token JWT
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out finalUserId) || finalUserId <= 0)
                {
                    var errorResponse = ResponseStructure<object>.ValidationError(
                        "User ID is required. Please provide 'userId' as a query parameter (?userId=123) or ensure your authentication token includes a valid user ID.");
                    return BadRequest(errorResponse);
                }
            }

            var result = await _invoiceService.UploadAttachmentAsync(invoiceId, file, finalUserId);

            var successResponse = ResponseStructure<UploadInvoiceAttachmentResponse>.Success(
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
                $"Database error while saving attachment: {innerMessage}",
                500);
            return StatusCode(500, errorResponse);
        }
        catch (Exception ex)
        {
            // Capturar el inner exception para más detalles
            var innerMessage = ex.InnerException?.Message ?? string.Empty;
            var fullMessage = $"An unexpected error occurred while uploading the attachment: {ex.Message}";
            if (!string.IsNullOrEmpty(innerMessage))
                fullMessage += $" | Inner exception: {innerMessage}";
            
            var errorResponse = ResponseStructure<object>.Error(fullMessage, 500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region DELETE - Delete Invoice Attachment

    /// <summary>
    /// Deletes a specific attachment from an invoice
    /// </summary>
    /// <param name="attachmentId">ID of the attachment to delete</param>
    /// <returns>Result of the deletion operation</returns>
    /// <remarks>
    /// This endpoint deletes the attachment from both the database and Azure Blob Storage.
    /// The operation is permanent and cannot be undone.
    /// </remarks>
    [HttpDelete("attachments/{attachmentId}")]
    public async Task<IActionResult> DeleteAttachment(int attachmentId)
    {
        try
        {
            var result = await _invoiceService.DeleteAttachmentAsync(attachmentId);

            var successResponse = ResponseStructure<DeleteInvoiceAttachmentResponse>.Success(
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
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"An unexpected error occurred while deleting the attachment: {ex.Message}",
                500);
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Deletes all attachments from a specific invoice
    /// </summary>
    /// <param name="invoiceId">ID of the invoice</param>
    /// <returns>Result of the deletion operation</returns>
    /// <remarks>
    /// This endpoint deletes ALL attachments associated with the invoice.
    /// Files are removed from both the database and Azure Blob Storage.
    /// The operation is permanent and cannot be undone.
    /// </remarks>
    [HttpDelete("{invoiceId}/attachments")]
    public async Task<IActionResult> DeleteAllAttachmentsForInvoice(int invoiceId)
    {
        try
        {
            var result = await _invoiceService.DeleteAllAttachmentsForInvoiceAsync(invoiceId);

            var successResponse = ResponseStructure<DeleteInvoiceAttachmentResponse>.Success(
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
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"An unexpected error occurred while deleting attachments: {ex.Message}",
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion
}

