using ApiLayer.Tools;
using ApplicationLayer.Corporate;
using ApplicationLayer.Shared;
using BusinessLayer.Corporate.Commands;
using BusinessLayer.Corporate.Queries;
using BusinessLayer.Corporate.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using ModelLayer.Shared;

namespace ApiLayer.Controllers.Corporate;

[ApiController]
[Route("api/corporate/manual-invoice")]
public class ManualInvoiceController : ControllerBase
{
    private readonly ManualInvoiceService _service;
    private readonly IErrorLogService _errorLogService;
    private readonly IValidator<CreateManualInvoiceHeaderRequest> _createValidator;

    public ManualInvoiceController(
        ManualInvoiceService service,
        IErrorLogService errorLogService,
        IValidator<CreateManualInvoiceHeaderRequest> createValidator)
    {
        _service = service;
        _errorLogService = errorLogService;
        _createValidator = createValidator;
    }

    /// <summary>
    /// Create a manual invoice linked directly to a customer (no contract required).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ResponseStructure<CreateManualInvoiceHeaderResponse>>> Create(
        [FromBody] CreateManualInvoiceHeaderRequest request)
    {
        try
        {
            var validation = await _createValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ResponseStructure<CreateManualInvoiceHeaderResponse>.BadRequest(
                    $"Validation failed: {errors}", "VALIDATION_ERROR", $"Validación fallida: {errors}"));
            }

            var currentUserId = GetCurrentUserId();
            var result = await _service.CreateAsync(request, currentUserId);

            return Ok(ResponseStructure<CreateManualInvoiceHeaderResponse>.Success(
                result,
                $"Manual invoice '{result.InvoiceNumber}' created successfully.",
                $"Factura manual '{result.InvoiceNumber}' creada exitosamente."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ResponseStructure<CreateManualInvoiceHeaderResponse>.NotFound(
                ex.Message, "NOT_FOUND", ex.Message));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, "CreateManualInvoice");
            return StatusCode(500, ResponseStructure<CreateManualInvoiceHeaderResponse>.Error(
                "An unexpected error occurred.", 500, errorNumber, "Ocurrió un error inesperado."));
        }
    }

    /// <summary>
    /// Update a manual invoice (not allowed if already paid or cancelled).
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ResponseStructure<UpdateManualInvoiceHeaderResponse>>> Update(
        int id,
        [FromBody] UpdateManualInvoiceHeaderRequest request)
    {
        try
        {
            var modifiedBy = HttpContext.User.Identity?.Name ?? "system";
            var result = await _service.UpdateAsync(id, request, modifiedBy);

            return Ok(ResponseStructure<UpdateManualInvoiceHeaderResponse>.Success(
                result,
                $"Invoice '{result.InvoiceNumber}' updated successfully.",
                $"Factura '{result.InvoiceNumber}' actualizada exitosamente."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ResponseStructure<UpdateManualInvoiceHeaderResponse>.NotFound(
                ex.Message, "NOT_FOUND", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseStructure<UpdateManualInvoiceHeaderResponse>.BadRequest(
                ex.Message, "INVALID_OPERATION", ex.Message));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, "UpdateManualInvoice");
            return StatusCode(500, ResponseStructure<UpdateManualInvoiceHeaderResponse>.Error(
                "An unexpected error occurred.", 500, errorNumber, "Ocurrió un error inesperado."));
        }
    }

    /// <summary>
    /// Mark a manual invoice as paid.
    /// </summary>
    [HttpPut("{id:int}/mark-as-paid")]
    public async Task<ActionResult<ResponseStructure<MarkManualInvoiceAsPaidResponse>>> MarkAsPaid(
        int id,
        [FromBody] MarkManualInvoiceAsPaidRequest request)
    {
        try
        {
            int userId = request.UserId ?? GetCurrentUserId();
            var result = await _service.MarkAsPaidAsync(id, request, userId);

            return Ok(ResponseStructure<MarkManualInvoiceAsPaidResponse>.Success(
                result,
                $"Invoice '{result.InvoiceNumber}' marked as paid.",
                $"Factura '{result.InvoiceNumber}' marcada como pagada."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ResponseStructure<MarkManualInvoiceAsPaidResponse>.NotFound(
                ex.Message, "NOT_FOUND", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseStructure<MarkManualInvoiceAsPaidResponse>.BadRequest(
                ex.Message, "INVALID_OPERATION", ex.Message));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, "MarkManualInvoiceAsPaid");
            return StatusCode(500, ResponseStructure<MarkManualInvoiceAsPaidResponse>.Error(
                "An unexpected error occurred.", 500, errorNumber, "Ocurrió un error inesperado."));
        }
    }

    /// <summary>
    /// Delete a manual invoice (not allowed if already paid).
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ResponseStructure<DeleteManualInvoiceHeaderResponse>>> Delete(int id)
    {
        try
        {
            var result = await _service.DeleteAsync(id);

            return Ok(ResponseStructure<DeleteManualInvoiceHeaderResponse>.Success(
                result,
                $"Invoice '{result.InvoiceNumber}' deleted successfully.",
                $"Factura '{result.InvoiceNumber}' eliminada exitosamente."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ResponseStructure<DeleteManualInvoiceHeaderResponse>.NotFound(
                ex.Message, "NOT_FOUND", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ResponseStructure<DeleteManualInvoiceHeaderResponse>.BadRequest(
                ex.Message, "INVALID_OPERATION", ex.Message));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, "DeleteManualInvoice");
            return StatusCode(500, ResponseStructure<DeleteManualInvoiceHeaderResponse>.Error(
                "An unexpected error occurred.", 500, errorNumber, "Ocurrió un error inesperado."));
        }
    }

    /// <summary>
    /// Query manual invoices for a customer. CustomerId is required; all other filters are optional.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ResponseStructure<object>>> Get(
        [FromQuery] int customerId,
        [FromQuery] int? invoiceId,
        [FromQuery] string? invoiceNumber,
        [FromQuery] string? status,
        [FromQuery] string? paymentStatus,
        [FromQuery] string? currencyCode,
        [FromQuery] DateTime? invoiceDateFrom,
        [FromQuery] DateTime? invoiceDateTo,
        [FromQuery] DateTime? dueDateFrom,
        [FromQuery] DateTime? dueDateTo,
        [FromQuery] bool? isOverdue,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            if (customerId <= 0)
                return BadRequest(ResponseStructure<object>.BadRequest(
                    "customerId is required.", "VALIDATION_ERROR", "customerId es requerido."));

            var filters = new ManualInvoiceFilters
            {
                CustomerId = customerId,
                ManualInvoiceHeaderId = invoiceId,
                InvoiceNumber = invoiceNumber,
                Status = status,
                PaymentStatus = paymentStatus,
                CurrencyCode = currencyCode,
                InvoiceDateFrom = invoiceDateFrom,
                InvoiceDateTo = invoiceDateTo,
                DueDateFrom = dueDateFrom,
                DueDateTo = dueDateTo,
                IsOverdue = isOverdue,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var invoices = await _service.GetAsync(filters);
            var list = invoices.ToList();

            return Ok(ResponseStructure<object>.Success(
                new { invoices = list, count = list.Count, pageNumber, pageSize },
                "Manual invoices retrieved successfully.",
                "Facturas manuales obtenidas exitosamente."));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(ex, HttpContext, "GetManualInvoices");
            return StatusCode(500, ResponseStructure<object>.Error(
                "An unexpected error occurred.", 500, errorNumber, "Ocurrió un error inesperado."));
        }
    }

    private int GetCurrentUserId()
    {
        var claim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "userId" || c.Type == "sub");
        return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
    }
}
