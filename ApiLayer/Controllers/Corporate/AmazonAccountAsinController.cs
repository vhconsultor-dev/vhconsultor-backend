using ApplicationLayer.Corporate;
using BusinessLayer.Corporate.Commands;
using BusinessLayer.Corporate.Validators;
using BusinessLayer.Corporate.Queries;
using Microsoft.AspNetCore.Mvc;
using ApiLayer.Tools;

namespace ApiLayer.Controllers.Corporate;

[ApiController]
[Route("api/corporate/amazon-account-asins")]
public class AmazonAccountAsinController : ControllerBase
{
    private readonly BulkUploadAmazonAccountAsinsService _bulkUploadService;
    private readonly BulkUploadAmazonAccountAsinsValidator _bulkUploadValidator;
    private readonly AmazonAccountAsinService _asinService;
    private readonly ILogger<AmazonAccountAsinController> _logger;

    public AmazonAccountAsinController(
        BulkUploadAmazonAccountAsinsService bulkUploadService,
        BulkUploadAmazonAccountAsinsValidator bulkUploadValidator,
        AmazonAccountAsinService asinService,
        ILogger<AmazonAccountAsinController> logger)
    {
        _bulkUploadService = bulkUploadService;
        _bulkUploadValidator = bulkUploadValidator;
        _asinService = asinService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene ASINs paginados de una cuenta Amazon con catálogos
    /// </summary>
    /// <param name="amazonAccountId">ID de la cuenta Amazon (requerido)</param>
    /// <param name="pageNumber">Número de página (default: 1)</param>
    /// <param name="pageSize">Registros por página (default: 100, máx: 500)</param>
    /// <param name="searchTerm">Término de búsqueda en ASIN, título o parent ASIN (opcional)</param>
    /// <param name="categoryId">Filtrar por CategoryId (opcional)</param>
    /// <param name="subCategoryId">Filtrar por SubCategoryId (opcional)</param>
    /// <param name="productGroupId">Filtrar por ProductGroupId (opcional)</param>
    /// <param name="replenishmentCategoryId">Filtrar por ReplenishmentCategoryId (opcional)</param>
    /// <returns>Lista paginada de ASINs con información de catálogos</returns>
    [HttpGet]
    public async Task<IActionResult> GetAsinsPaginated(
        [FromQuery] int amazonAccountId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] int? subCategoryId = null,
        [FromQuery] int? productGroupId = null,
        [FromQuery] int? replenishmentCategoryId = null)
    {
        try
        {
            if (amazonAccountId <= 0)
            {
                var errorResponse = ResponseStructure<object>.BadRequest("AmazonAccountId must be greater than 0.");
                return BadRequest(errorResponse);
            }

            var filters = new AsinQueryFilters
            {
                AmazonAccountId = amazonAccountId,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                CategoryId = categoryId,
                SubCategoryId = subCategoryId,
                ProductGroupId = productGroupId,
                ReplenishmentCategoryId = replenishmentCategoryId
            };

            var result = await _asinService.GetAsinsPaginatedAsync(filters);

            var message = $"Retrieved {result.Data.Count} ASINs from page {result.PageNumber} of {result.TotalPages}. Total: {result.TotalRecords} records.";
            var response = ResponseStructure<PaginatedAsinResult>.Success(result, message);
            
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error getting ASINs: {Message}", ex.Message);
            var response = ResponseStructure<object>.BadRequest(ex.Message);
            return BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ASINs for AmazonAccountId {AmazonAccountId}", amazonAccountId);
            var response = ResponseStructure<object>.Error("An error occurred while retrieving ASINs. Please try again.");
            return StatusCode(500, response);
        }
    }

    /// <summary>
    /// Carga masiva de ASINs desde archivo Excel
    /// </summary>
    /// <param name="request">amazonAccountId y archivo Excel (excelFile)</param>
    /// <returns>Resultado de la carga con estadísticas</returns>
    [HttpPost("bulk-upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> BulkUpload([FromForm] BulkUploadAmazonAccountAsinsRequest request)
    {
        try
        {
            var validationResult = await _bulkUploadValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                var errorResponse = ResponseStructure<object>.BadRequest(
                    $"Validation failed: {errors}. " +
                    $"Please ensure: 1) amazonAccountId is greater than 0, " +
                    $"2) excelFile is provided, 3) file has .xlsx or .xls extension, " +
                    $"4) file is not empty."
                );
                return BadRequest(errorResponse);
            }

            var currentUser = User?.Identity?.Name ?? "System";
            var result = await _bulkUploadService.ProcessExcelAsync(request.AmazonAccountId, request.ExcelFile, currentUser);

            var message = $"Bulk upload completed. {result.RegistrosCargadosOk} records loaded successfully, " +
                         $"{result.RegistrosDuplicados} duplicates skipped, {result.RegistrosConError} errors.";

            var response = ResponseStructure<BulkUploadResult>.Success(result, message);
            return Ok(response);
        }
        catch (BulkUploadValidationException validationEx)
        {
            _logger.LogWarning(validationEx, "Validation error in bulk upload: {ErrorCode} - {Message}", 
                validationEx.ErrorCode, validationEx.Message);
            
            var detailedError = new
            {
                error = validationEx.Message,
                errorCode = validationEx.ErrorCode,
                suggestion = validationEx.Suggestion,
                timestamp = DateTime.UtcNow.AddHours(-6)
            };
            
            var errorResponse = ResponseStructure<object>.BadRequest(
                $"{validationEx.Message} [Error Code: {validationEx.ErrorCode}] Suggestion: {validationEx.Suggestion}"
            );
            return BadRequest(errorResponse);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error in bulk upload: {Message}", ex.Message);
            var response = ResponseStructure<object>.BadRequest(
                $"{ex.Message} Please verify your data and try again."
            );
            return BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bulk upload for AmazonAccountId {AmazonAccountId}. Error: {ErrorMessage}", 
                request.AmazonAccountId, ex.Message);
            
            var response = ResponseStructure<object>.Error(
                $"An unexpected error occurred while processing the Excel file. " +
                $"Please verify: 1) The file is a valid Excel format (.xlsx or .xls), " +
                $"2) The file can be opened in Microsoft Excel, " +
                $"3) Row 1 contains headers 'ASIN' and 'Product Title', " +
                $"4) There is at least one data row. " +
                $"If the problem persists, contact support with this error: {ex.Message}"
            );
            return StatusCode(500, response);
        }
    }
}
