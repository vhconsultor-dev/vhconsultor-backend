using ApiLayer.Tools;
using ApplicationLayer.BrandPartner;
using ApplicationLayer.Shared;
using BusinessLayer.BrandPartner.Commands;
using BusinessLayer.BrandPartner.Queries;
using BusinessLayer.BrandPartner.Validators;
using Microsoft.AspNetCore.Mvc;
using ModelLayer.Shared;

namespace ApiLayer.Controllers.BrandPartner;

[ApiController]
[Route("api/brand-partner/settlement")]
public class SettlementController : ControllerBase
{
    private readonly SettlementService _settlementService;
    private readonly IErrorLogService _errorLogService;
    private readonly BulkUploadSettlementValidator _bulkUploadSettlementValidator;

    public SettlementController(
        SettlementService settlementService,
        IErrorLogService errorLogService,
        BulkUploadSettlementValidator bulkUploadSettlementValidator)
    {
        _settlementService = settlementService;
        _errorLogService = errorLogService;
        _bulkUploadSettlementValidator = bulkUploadSettlementValidator;
    }

    /// <summary>
    /// Carga masiva de settlement desde Excel de Amazon
    /// </summary>
    [HttpPost("bulk-upload")]
    public async Task<ActionResult<ResponseStructure<BulkUploadSettlementResult>>> BulkUploadSettlement(
        [FromForm] BulkUploadSettlementRequest request)
    {
        try
        {
            var validationResult = await _bulkUploadSettlementValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ResponseStructure<BulkUploadSettlementResult>.BadRequest(
                    $"Validation failed: {errorMessage}",
                    "VALIDATION_ERROR",
                    $"Validación fallida: {errorMessage}"
                ));
            }

            var currentUser = HttpContext.User.Identity?.Name ?? "system";
            var result = await _settlementService.BulkUploadSettlementAsync(
                request.AmazonAccountId,
                request.ExcelFile,
                currentUser);

            if (!result.Success)
            {
                return BadRequest(new ResponseStructure<BulkUploadSettlementResult>(
                    false,
                    400,
                    result,
                    result.MessageEN,
                    "BULK_UPLOAD_FAILED",
                    result.MessageES
                ));
            }

            return Ok(ResponseStructure<BulkUploadSettlementResult>.Success(
                result,
                result.MessageEN,
                result.MessageES
            ));
        }
        catch (BulkUploadSettlementValidationException buvEx)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(
                buvEx,
                HttpContext,
                "BulkUploadSettlement"
            );

            return BadRequest(ResponseStructure<BulkUploadSettlementResult>.BadRequest(
                buvEx.Message,
                errorNumber,
                buvEx.MessageES
            ));
        }
        catch (InvalidOperationException ioEx) when (ioEx.Message.Contains("ExcelPackage.License", StringComparison.OrdinalIgnoreCase))
        {
            var errorNumber = await _errorLogService.LogErrorAsync(
                ioEx,
                HttpContext,
                "BulkUploadSettlement"
            );

            return BadRequest(ResponseStructure<BulkUploadSettlementResult>.BadRequest(
                "The Excel processing component is not configured correctly. Please contact support.",
                errorNumber,
                "El componente de procesamiento de Excel no esta configurado correctamente. Por favor contacte a soporte."
            ));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(
                ex,
                HttpContext,
                "BulkUploadSettlement"
            );

            return StatusCode(500, ResponseStructure<BulkUploadSettlementResult>.Error(
                "An unexpected error occurred while processing the settlement upload.",
                500,
                errorNumber,
                "Ocurrió un error inesperado al procesar la carga de settlement."
            ));
        }
    }

    /// <summary>
    /// Obtener settlements por AmazonAccount con filtros
    /// </summary>
    [HttpGet("account/{amazonAccountId}")]
    public async Task<ActionResult<ResponseStructure>> GetSettlementHeaders(
        int amazonAccountId,
        [FromQuery] string? settlementId,
        [FromQuery] DateTime? depositDateFrom,
        [FromQuery] DateTime? depositDateTo,
        [FromQuery] string? currency,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var filters = new SettlementHeaderFilters
            {
                SettlementId = settlementId,
                DepositDateFrom = depositDateFrom,
                DepositDateTo = depositDateTo,
                Currency = currency,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var headers = await _settlementService.GetSettlementHeadersAsync(amazonAccountId, filters);

            return Ok(ResponseStructure<object>.Success(
                new { headers, pageNumber, pageSize, count = headers.Count() },
                "Settlement headers retrieved successfully.",
                "Encabezados de settlement obtenidos exitosamente."
            ));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(
                ex,
                HttpContext,
                "GetSettlementHeaders"
            );

            return StatusCode(500, ResponseStructure.Error(
                "An unexpected error occurred while retrieving settlement headers.",
                500,
                errorNumber,
                "Ocurrió un error inesperado al obtener los encabezados de settlement."
            ));
        }
    }

    /// <summary>
    /// Obtener settlement header por ID
    /// </summary>
    [HttpGet("{settlementHeaderId}")]
    public async Task<ActionResult<ResponseStructure>> GetSettlementHeaderById(long settlementHeaderId)
    {
        try
        {
            var header = await _settlementService.GetSettlementHeaderByIdAsync(settlementHeaderId);

            if (header == null)
            {
                return NotFound(ResponseStructure.NotFound(
                    $"Settlement header with ID {settlementHeaderId} was not found.",
                    "NOT_FOUND",
                    $"Encabezado de settlement con ID {settlementHeaderId} no fue encontrado."
                ));
            }

            return Ok(ResponseStructure<object>.Success(
                header,
                "Settlement header retrieved successfully.",
                "Encabezado de settlement obtenido exitosamente."
            ));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(
                ex,
                HttpContext,
                "GetSettlementHeaderById"
            );

            return StatusCode(500, ResponseStructure.Error(
                "An unexpected error occurred while retrieving the settlement header.",
                500,
                errorNumber,
                "Ocurrió un error inesperado al obtener el encabezado de settlement."
            ));
        }
    }

    /// <summary>
    /// Obtener detalles de un settlement específico
    /// </summary>
    [HttpGet("{settlementHeaderId}/details")]
    public async Task<ActionResult<ResponseStructure>> GetSettlementDetails(
        long settlementHeaderId,
        [FromQuery] string? transactionType,
        [FromQuery] string? orderId,
        [FromQuery] string? sku,
        [FromQuery] DateTime? postedDateFrom,
        [FromQuery] DateTime? postedDateTo,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100)
    {
        try
        {
            var filters = new SettlementDetailFilters
            {
                TransactionType = transactionType,
                OrderId = orderId,
                Sku = sku,
                PostedDateFrom = postedDateFrom,
                PostedDateTo = postedDateTo,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var details = await _settlementService.GetSettlementDetailsAsync(settlementHeaderId, filters);

            return Ok(ResponseStructure<object>.Success(
                new { details, pageNumber, pageSize, count = details.Count() },
                "Settlement details retrieved successfully.",
                "Detalles de settlement obtenidos exitosamente."
            ));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(
                ex,
                HttpContext,
                "GetSettlementDetails"
            );

            return StatusCode(500, ResponseStructure.Error(
                "An unexpected error occurred while retrieving settlement details.",
                500,
                errorNumber,
                "Ocurrió un error inesperado al obtener los detalles de settlement."
            ));
        }
    }

    /// <summary>
    /// Obtener detalles por OrderId
    /// </summary>
    [HttpGet("order/{orderId}")]
    public async Task<ActionResult<ResponseStructure>> GetSettlementDetailsByOrderId(string orderId)
    {
        try
        {
            var details = await _settlementService.GetSettlementDetailsByOrderIdAsync(orderId);

            return Ok(ResponseStructure<object>.Success(
                new { details, count = details.Count() },
                "Settlement details retrieved successfully.",
                "Detalles de settlement obtenidos exitosamente."
            ));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(
                ex,
                HttpContext,
                "GetSettlementDetailsByOrderId"
            );

            return StatusCode(500, ResponseStructure.Error(
                "An unexpected error occurred while retrieving settlement details.",
                500,
                errorNumber,
                "Ocurrió un error inesperado al obtener los detalles de settlement."
            ));
        }
    }

    /// <summary>
    /// Obtener snapshots de inventario por settlement
    /// </summary>
    [HttpGet("{settlementHeaderId}/snapshots")]
    public async Task<ActionResult<ResponseStructure>> GetSnapshotsBySettlement(long settlementHeaderId)
    {
        try
        {
            var snapshots = await _settlementService.GetSnapshotsBySettlementAsync(settlementHeaderId);

            return Ok(ResponseStructure<object>.Success(
                new { snapshots, count = snapshots.Count() },
                "Inventory snapshots retrieved successfully.",
                "Snapshots de inventario obtenidos exitosamente."
            ));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(
                ex,
                HttpContext,
                "GetSnapshotsBySettlement"
            );

            return StatusCode(500, ResponseStructure.Error(
                "An unexpected error occurred while retrieving inventory snapshots.",
                500,
                errorNumber,
                "Ocurrió un error inesperado al obtener los snapshots de inventario."
            ));
        }
    }

    /// <summary>
    /// Obtener snapshots de inventario por item en un rango de fechas
    /// </summary>
    [HttpGet("snapshots/item/{inventoryItemId}")]
    public async Task<ActionResult<ResponseStructure>> GetSnapshotsByInventoryItem(
        long inventoryItemId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo)
    {
        try
        {
            var snapshots = await _settlementService.GetSnapshotsByInventoryItemAsync(
                inventoryItemId,
                dateFrom,
                dateTo);

            return Ok(ResponseStructure<object>.Success(
                new { snapshots, count = snapshots.Count() },
                "Inventory snapshots retrieved successfully.",
                "Snapshots de inventario obtenidos exitosamente."
            ));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(
                ex,
                HttpContext,
                "GetSnapshotsByInventoryItem"
            );

            return StatusCode(500, ResponseStructure.Error(
                "An unexpected error occurred while retrieving inventory snapshots.",
                500,
                errorNumber,
                "Ocurrió un error inesperado al obtener los snapshots de inventario."
            ));
        }
    }
}
