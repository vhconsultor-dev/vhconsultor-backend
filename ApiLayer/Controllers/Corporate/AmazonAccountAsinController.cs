using ApplicationLayer.Corporate;
using BusinessLayer.Corporate.Commands;
using BusinessLayer.Corporate.Validators;
using Microsoft.AspNetCore.Mvc;
using ApiLayer.Tools;

namespace ApiLayer.Controllers.Corporate;

[ApiController]
[Route("api/corporate/amazon-account-asins")]
public class AmazonAccountAsinController : ControllerBase
{
    private readonly BulkUploadAmazonAccountAsinsService _bulkUploadService;
    private readonly BulkUploadAmazonAccountAsinsValidator _bulkUploadValidator;
    private readonly ILogger<AmazonAccountAsinController> _logger;

    public AmazonAccountAsinController(
        BulkUploadAmazonAccountAsinsService bulkUploadService,
        BulkUploadAmazonAccountAsinsValidator bulkUploadValidator,
        ILogger<AmazonAccountAsinController> logger)
    {
        _bulkUploadService = bulkUploadService;
        _bulkUploadValidator = bulkUploadValidator;
        _logger = logger;
    }

    /// <summary>
    /// Carga masiva de ASINs desde archivo Excel
    /// </summary>
    /// <param name="amazonAccountId">ID de la cuenta Amazon</param>
    /// <param name="excelFile">Archivo Excel con los ASINs</param>
    /// <returns>Resultado de la carga con estadísticas</returns>
    [HttpPost("bulk-upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> BulkUpload([FromForm] int amazonAccountId, [FromForm] IFormFile excelFile)
    {
        try
        {
            // Validar request
            var request = new BulkUploadAmazonAccountAsinsRequest
            {
                AmazonAccountId = amazonAccountId,
                ExcelFile = excelFile
            };

            var validationResult = await _bulkUploadValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                var errorResponse = ResponseStructure<object>.Error(errors);
                return BadRequest(errorResponse);
            }

            // Obtener usuario actual (aquí puedes usar Claims si tienes autenticación)
            var currentUser = User?.Identity?.Name ?? "System";

            // Procesar carga masiva
            var result = await _bulkUploadService.ProcessExcelAsync(amazonAccountId, excelFile, currentUser);

            var message = $"Bulk upload completed. {result.RegistrosCargadosOk} records loaded successfully, " +
                         $"{result.RegistrosDuplicados} duplicates skipped, {result.RegistrosConError} errors.";

            var response = ResponseStructure<BulkUploadResult>.Success(result, message);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error in bulk upload: {Message}", ex.Message);
            var response = ResponseStructure<object>.Error(ex.Message);
            return BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bulk upload for AmazonAccountId {AmazonAccountId}", amazonAccountId);
            var response = ResponseStructure<object>.Error("An error occurred while processing the Excel file. Please check the file format and try again.");
            return StatusCode(500, response);
        }
    }
}
