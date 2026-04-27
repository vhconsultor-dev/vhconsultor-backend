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
[Route("api/brand-partner/inventory")]
public class InventoryController : ControllerBase
{
    private readonly InventoryService _inventoryService;
    private readonly IErrorLogService _errorLogService;
    private readonly BulkUploadInventoryValidator _bulkUploadInventoryValidator;
    private readonly CreateInventoryItemValidator _createInventoryItemValidator;
    private readonly UpdateInventoryItemValidator _updateInventoryItemValidator;
    private readonly AdjustInventoryManualValidator _adjustInventoryManualValidator;

    public InventoryController(
        InventoryService inventoryService,
        IErrorLogService errorLogService,
        BulkUploadInventoryValidator bulkUploadInventoryValidator,
        CreateInventoryItemValidator createInventoryItemValidator,
        UpdateInventoryItemValidator updateInventoryItemValidator,
        AdjustInventoryManualValidator adjustInventoryManualValidator)
    {
        _inventoryService = inventoryService;
        _errorLogService = errorLogService;
        _bulkUploadInventoryValidator = bulkUploadInventoryValidator;
        _createInventoryItemValidator = createInventoryItemValidator;
        _updateInventoryItemValidator = updateInventoryItemValidator;
        _adjustInventoryManualValidator = adjustInventoryManualValidator;
    }

    /// <summary>
    /// Carga masiva de inventario desde Excel
    /// </summary>
    [HttpPost("bulk-upload")]
    public async Task<ActionResult<ResponseStructure<BulkUploadInventoryResult>>> BulkUploadInventory(
        [FromForm] BulkUploadInventoryRequest request)
    {
        try
        {
            var validationResult = await _bulkUploadInventoryValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ResponseStructure<BulkUploadInventoryResult>.BadRequest(
                    $"Validation failed: {errorMessage}",
                    "VALIDATION_ERROR",
                    $"Validación fallida: {errorMessage}"
                ));
            }

            var currentUser = HttpContext.User.Identity?.Name ?? "system";
            var result = await _inventoryService.BulkUploadInventoryAsync(
                request.AmazonAccountId,
                request.ExcelFile,
                currentUser);

            return Ok(ResponseStructure<BulkUploadInventoryResult>.Success(
                result,
                result.MessageEN,
                result.MessageES
            ));
        }
        catch (BulkUploadInventoryValidationException buvEx)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(
                buvEx,
                HttpContext,
                "BulkUploadInventory"
            );

            return BadRequest(ResponseStructure<BulkUploadInventoryResult>.BadRequest(
                buvEx.Message,
                errorNumber,
                buvEx.Message
            ));
        }
        catch (InvalidOperationException ioEx) when (ioEx.Message.Contains("ExcelPackage.License", StringComparison.OrdinalIgnoreCase))
        {
            var errorNumber = await _errorLogService.LogErrorAsync(
                ioEx,
                HttpContext,
                "BulkUploadInventory"
            );

            return BadRequest(ResponseStructure<BulkUploadInventoryResult>.BadRequest(
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
                "BulkUploadInventory"
            );

            return StatusCode(500, ResponseStructure<BulkUploadInventoryResult>.Error(
                "An unexpected error occurred while processing the inventory upload.",
                500,
                errorNumber,
                "Ocurrió un error inesperado al procesar la carga de inventario."
            ));
        }
    }

    /// <summary>
    /// Crear item de inventario manualmente
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ResponseStructure<long>>> CreateInventoryItem(
        [FromBody] CreateInventoryItemRequest request)
    {
        try
        {
            var validationResult = await _createInventoryItemValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ResponseStructure<long>.BadRequest(
                    $"Validation failed: {errorMessage}",
                    "VALIDATION_ERROR",
                    $"Validación fallida: {errorMessage}"
                ));
            }

            var inventoryItemId = await _inventoryService.CreateInventoryItemAsync(request);

            return StatusCode(201, ResponseStructure<long>.Success(
                inventoryItemId,
                201,
                "Inventory item created successfully.",
                "Item de inventario creado exitosamente."
            ));
        }
        catch (InvalidOperationException ioEx)
        {
            return BadRequest(ResponseStructure<long>.BadRequest(
                ioEx.Message,
                "INVALID_OPERATION",
                ioEx.Message
            ));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(
                ex,
                HttpContext,
                "CreateInventoryItem"
            );

            return StatusCode(500, ResponseStructure<long>.Error(
                "An unexpected error occurred while creating the inventory item.",
                500,
                errorNumber,
                "Ocurrió un error inesperado al crear el item de inventario."
            ));
        }
    }

    /// <summary>
    /// Actualizar item de inventario
    /// </summary>
    [HttpPut("{inventoryItemId}")]
    public async Task<ActionResult<ResponseStructure>> UpdateInventoryItem(
        long inventoryItemId,
        [FromBody] UpdateInventoryItemRequest request)
    {
        try
        {
            var validationResult = await _updateInventoryItemValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ResponseStructure.BadRequest(
                    $"Validation failed: {errorMessage}",
                    "VALIDATION_ERROR",
                    $"Validación fallida: {errorMessage}"
                ));
            }

            await _inventoryService.UpdateInventoryItemAsync(inventoryItemId, request);

            return Ok(ResponseStructure.Success(
                "Inventory item updated successfully.",
                "Item de inventario actualizado exitosamente."
            ));
        }
        catch (InvalidOperationException ioEx)
        {
            return NotFound(ResponseStructure.NotFound(
                ioEx.Message,
                "NOT_FOUND",
                ioEx.Message
            ));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(
                ex,
                HttpContext,
                "UpdateInventoryItem"
            );

            return StatusCode(500, ResponseStructure.Error(
                "An unexpected error occurred while updating the inventory item.",
                500,
                errorNumber,
                "Ocurrió un error inesperado al actualizar el item de inventario."
            ));
        }
    }

    /// <summary>
    /// Ajustar inventario manualmente
    /// </summary>
    [HttpPost("adjust")]
    public async Task<ActionResult<ResponseStructure<AdjustInventoryManualResult>>> AdjustInventoryManual(
        [FromBody] AdjustInventoryManualRequest request)
    {
        try
        {
            var validationResult = await _adjustInventoryManualValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ResponseStructure<AdjustInventoryManualResult>.BadRequest(
                    $"Validation failed: {errorMessage}",
                    "VALIDATION_ERROR",
                    $"Validación fallida: {errorMessage}"
                ));
            }

            var result = await _inventoryService.AdjustInventoryManualAsync(request);

            return Ok(ResponseStructure<AdjustInventoryManualResult>.Success(
                result,
                result.MessageEN,
                result.MessageES
            ));
        }
        catch (InvalidOperationException ioEx)
        {
            return BadRequest(ResponseStructure<AdjustInventoryManualResult>.BadRequest(
                ioEx.Message,
                "INVALID_OPERATION",
                ioEx.Message
            ));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(
                ex,
                HttpContext,
                "AdjustInventoryManual"
            );

            return StatusCode(500, ResponseStructure<AdjustInventoryManualResult>.Error(
                "An unexpected error occurred while adjusting inventory.",
                500,
                errorNumber,
                "Ocurrió un error inesperado al ajustar el inventario."
            ));
        }
    }

    /// <summary>
    /// Obtener inventario por AmazonAccount con filtros
    /// </summary>
    [HttpGet("account/{amazonAccountId}")]
    public async Task<ActionResult<ResponseStructure>> GetInventoryByAccount(
        int amazonAccountId,
        [FromQuery] string? sku,
        [FromQuery] string? asin,
        [FromQuery] string? prepOwner,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var filters = new InventoryItemFilters
            {
                Sku = sku,
                Asin = asin,
                PrepOwner = prepOwner,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var items = await _inventoryService.GetInventoryByAccountAsync(amazonAccountId, filters);

            return Ok(ResponseStructure<object>.Success(
                new { items, pageNumber, pageSize, count = items.Count() },
                "Inventory retrieved successfully.",
                "Inventario obtenido exitosamente."
            ));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(
                ex,
                HttpContext,
                "GetInventoryByAccount"
            );

            return StatusCode(500, ResponseStructure.Error(
                "An unexpected error occurred while retrieving inventory.",
                500,
                errorNumber,
                "Ocurrió un error inesperado al obtener el inventario."
            ));
        }
    }

    /// <summary>
    /// Obtener item de inventario por ID
    /// </summary>
    [HttpGet("{inventoryItemId}")]
    public async Task<ActionResult<ResponseStructure>> GetInventoryItemById(long inventoryItemId)
    {
        try
        {
            var item = await _inventoryService.GetInventoryItemByIdAsync(inventoryItemId);

            if (item == null)
            {
                return NotFound(ResponseStructure.NotFound(
                    $"Inventory item with ID {inventoryItemId} was not found.",
                    "NOT_FOUND",
                    $"Item de inventario con ID {inventoryItemId} no fue encontrado."
                ));
            }

            return Ok(ResponseStructure<object>.Success(
                item,
                "Inventory item retrieved successfully.",
                "Item de inventario obtenido exitosamente."
            ));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(
                ex,
                HttpContext,
                "GetInventoryItemById"
            );

            return StatusCode(500, ResponseStructure.Error(
                "An unexpected error occurred while retrieving the inventory item.",
                500,
                errorNumber,
                "Ocurrió un error inesperado al obtener el item de inventario."
            ));
        }
    }

    /// <summary>
    /// Obtener movimientos de inventario (auditoría)
    /// </summary>
    [HttpGet("{inventoryItemId}/movements")]
    public async Task<ActionResult<ResponseStructure>> GetInventoryMovements(
        long inventoryItemId,
        [FromQuery] string? movementType,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100)
    {
        try
        {
            var filters = new InventoryMovementFilters
            {
                MovementType = movementType,
                DateFrom = dateFrom,
                DateTo = dateTo,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var movements = await _inventoryService.GetInventoryMovementsAsync(inventoryItemId, filters);

            return Ok(ResponseStructure<object>.Success(
                new { movements, pageNumber, pageSize, count = movements.Count() },
                "Inventory movements retrieved successfully.",
                "Movimientos de inventario obtenidos exitosamente."
            ));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(
                ex,
                HttpContext,
                "GetInventoryMovements"
            );

            return StatusCode(500, ResponseStructure.Error(
                "An unexpected error occurred while retrieving inventory movements.",
                500,
                errorNumber,
                "Ocurrió un error inesperado al obtener los movimientos de inventario."
            ));
        }
    }

    /// <summary>
    /// Obtener movimientos de inventario por AmazonAccount + InventoryItem (auditoría)
    /// </summary>
    [HttpGet("account/{amazonAccountId}/items/{inventoryItemId}/movements")]
    public async Task<ActionResult<ResponseStructure>> GetInventoryMovementsByAccount(
        int amazonAccountId,
        long inventoryItemId,
        [FromQuery] string? movementType,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100)
    {
        try
        {
            var filters = new InventoryMovementFilters
            {
                MovementType = movementType,
                DateFrom = dateFrom,
                DateTo = dateTo,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var movements = await _inventoryService.GetInventoryMovementsByAccountAsync(amazonAccountId, inventoryItemId, filters);

            return Ok(ResponseStructure<object>.Success(
                new { movements, pageNumber, pageSize, count = movements.Count() },
                "Inventory movements retrieved successfully.",
                "Movimientos de inventario obtenidos exitosamente."
            ));
        }
        catch (Exception ex)
        {
            var errorNumber = await _errorLogService.LogErrorAsync(
                ex,
                HttpContext,
                "GetInventoryMovementsByAccount"
            );

            return StatusCode(500, ResponseStructure.Error(
                "An unexpected error occurred while retrieving inventory movements.",
                500,
                errorNumber,
                "Ocurrio un error inesperado al obtener los movimientos de inventario."
            ));
        }
    }
}
