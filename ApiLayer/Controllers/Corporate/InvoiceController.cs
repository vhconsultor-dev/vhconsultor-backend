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
    /// Genera todas las facturas automáticamente para un contrato
    /// </summary>
    /// <param name="contractId">ID del contrato</param>
    /// <returns>Resultado de la generación</returns>
    /// <remarks>
    /// Este endpoint evalúa si ya existen facturas para el contrato.
    /// Si existen, retorna un mensaje indicándolo.
    /// Si no existen, genera todas las facturas en estado Draft.
    /// </remarks>
    [HttpPost("generate/{contractId}")]
    public async Task<IActionResult> GenerateInvoices(string contractId)
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
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al generar facturas: {ex.Message}", 500);
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
            // Obtener UserId del token JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                var errorResponse = ResponseStructure<object>.Error("Usuario no autenticado", 401);
                return Unauthorized(errorResponse);
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
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al marcar factura como pagada: {ex.Message}", 500);
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
        [FromQuery] string? contractId = null,
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
    public async Task<IActionResult> GetInvoiceSummaryByContract(string contractId)
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
}

