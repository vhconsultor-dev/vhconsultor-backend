using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using ApplicationLayer.Amazon.Vendor.Reports;
using ApplicationLayer.Shared;
using ApiLayer.Tools;
using BusinessLayer.Amazon.Vendor.Reports.Models;

namespace ApiLayer.Controllers.Amazon.Vendor.Reports;

/// <summary>
/// Controlador para reportes de Amazon Vendor
/// </summary>
[ApiController]
[Route("api/amazon/vendor/reports")]
public class VendorReportsController : ControllerBase
{
    private readonly VendorReportService _vendorReportService;
    private readonly ValidationService _validationService;
    private readonly IConfiguration _configuration;

    public VendorReportsController(
        VendorReportService vendorReportService,
        ValidationService validationService,
        IConfiguration configuration)
    {
        _vendorReportService = vendorReportService;
        _validationService = validationService;
        _configuration = configuration;
    }

    /// <summary>
    /// Genera un reporte de tráfico de Vendor (GET_VENDOR_TRAFFIC_REPORT)
    /// </summary>
    /// <param name="request">Datos del reporte a generar</param>
    /// <param name="accessToken">Access token de Amazon (x-amz-access-token header)</param>
    /// <returns>Report ID generado por Amazon</returns>
    [HttpPost("vendor-traffic")]
    [Authorize]
    public async Task<IActionResult> GenerateVendorTrafficReport(
        [FromBody] GenerateVendorTrafficReportRequest request,
        [FromHeader(Name = "x-amz-access-token")] string accessToken)
    {
        // Validar el access token
        if (string.IsNullOrEmpty(accessToken))
        {
            var errorResponse = ResponseStructure<object>.BadRequest(
                "El header 'x-amz-access-token' es requerido. Debes proporcionar el access token de Amazon generado previamente.");
            return BadRequest(errorResponse);
        }

        // Validación del request usando FluentValidation
        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errorResponse = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(errorResponse);
        }

        // Leer configuración de variables de entorno de Azure
        var vendorReportsSettings = _configuration.GetSection("AmazonVendorReports");
        var accessKey = vendorReportsSettings["AccessKey"] ?? string.Empty;
        var secretKey = vendorReportsSettings["SecretKey"] ?? string.Empty;
        var awsRegion = vendorReportsSettings["AwsRegion"] ?? "eu-west-1";
        var serviceName = vendorReportsSettings["ServiceName"] ?? "execute-api";
        var reportsUrl = vendorReportsSettings["BaseUrl"] ?? "https://sellingpartnerapi-eu.amazon.com/reports/2021-06-30";
        var reportsEndpointUrl = $"{reportsUrl.TrimEnd('/')}/reports";

        // Validar que las variables de entorno estén configuradas
        if (string.IsNullOrEmpty(accessKey))
        {
            var errorResponse = ResponseStructure<object>.BadRequest(
                "La variable de entorno AmazonVendorReports__AccessKey no está configurada en Azure");
            return BadRequest(errorResponse);
        }

        if (string.IsNullOrEmpty(secretKey))
        {
            var errorResponse = ResponseStructure<object>.BadRequest(
                "La variable de entorno AmazonVendorReports__SecretKey no está configurada en Azure");
            return BadRequest(errorResponse);
        }

        try
        {
            var result = await _vendorReportService.GenerateVendorTrafficReportAsync(
                request,
                accessToken,
                accessKey,
                secretKey,
                awsRegion,
                serviceName,
                reportsEndpointUrl);

            if (!result.Success)
            {
                var errorResponse = ResponseStructure<object>.BadRequest(result.Message);
                return BadRequest(errorResponse);
            }

            var response = ResponseStructure<object>.Success(
                new
                {
                    reportId = result.ReportId
                },
                result.Message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al generar el reporte de tráfico: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Consulta el estado de un reporte previamente generado
    /// </summary>
    /// <param name="reportId">ID del reporte obtenido al generar el reporte</param>
    /// <param name="accessToken">Access token de Amazon (x-amz-access-token header)</param>
    /// <returns>Estado del reporte y detalles</returns>
    [HttpGet("status")]
    [Authorize]
    public async Task<IActionResult> GetReportStatus(
        [FromQuery] string reportId,
        [FromHeader(Name = "x-amz-access-token")] string accessToken)
    {
        // Validar el reportId
        if (string.IsNullOrWhiteSpace(reportId))
        {
            var errorResponse = ResponseStructure<object>.BadRequest(
                "El parámetro 'reportId' es requerido. Debes proporcionar el ID del reporte que deseas consultar.");
            return BadRequest(errorResponse);
        }

        // Validar el access token
        if (string.IsNullOrEmpty(accessToken))
        {
            var errorResponse = ResponseStructure<object>.BadRequest(
                "El header 'x-amz-access-token' es requerido. Debes proporcionar el access token de Amazon generado previamente.");
            return BadRequest(errorResponse);
        }

        // Leer configuración de variables de entorno de Azure
        var vendorReportsSettings = _configuration.GetSection("AmazonVendorReports");
        var accessKey = vendorReportsSettings["AccessKey"] ?? string.Empty;
        var secretKey = vendorReportsSettings["SecretKey"] ?? string.Empty;
        var awsRegion = vendorReportsSettings["AwsRegion"] ?? "eu-west-1";
        var serviceName = vendorReportsSettings["ServiceName"] ?? "execute-api";
        var reportsUrl = vendorReportsSettings["BaseUrl"] ?? "https://sellingpartnerapi-eu.amazon.com/reports/2021-06-30";
        var reportsEndpointUrl = $"{reportsUrl.TrimEnd('/')}/reports";

        // Validar que las variables de entorno estén configuradas
        if (string.IsNullOrEmpty(accessKey))
        {
            var errorResponse = ResponseStructure<object>.BadRequest(
                "La variable de entorno AmazonVendorReports__AccessKey no está configurada en Azure");
            return BadRequest(errorResponse);
        }

        if (string.IsNullOrEmpty(secretKey))
        {
            var errorResponse = ResponseStructure<object>.BadRequest(
                "La variable de entorno AmazonVendorReports__SecretKey no está configurada en Azure");
            return BadRequest(errorResponse);
        }

        try
        {
            var result = await _vendorReportService.GetReportStatusAsync(
                reportId,
                accessToken,
                accessKey,
                secretKey,
                awsRegion,
                serviceName,
                reportsEndpointUrl);

            if (!result.Success)
            {
                var errorResponse = ResponseStructure<object>.BadRequest(result.Message);
                return BadRequest(errorResponse);
            }

            var response = ResponseStructure<object>.Success(
                new
                {
                    reportType = result.ReportStatus?.ReportType,
                    processingStatus = result.ReportStatus?.ProcessingStatus,
                    reportId = result.ReportStatus?.ReportId,
                    reportDocumentId = result.ReportStatus?.ReportDocumentId,
                    marketplaceIds = result.ReportStatus?.MarketplaceIds,
                    dataStartTime = result.ReportStatus?.DataStartTime,
                    dataEndTime = result.ReportStatus?.DataEndTime,
                    createdTime = result.ReportStatus?.CreatedTime,
                    processingStartTime = result.ReportStatus?.ProcessingStartTime,
                    processingEndTime = result.ReportStatus?.ProcessingEndTime
                },
                result.Message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al consultar el estado del reporte: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Descarga el documento de un reporte completado, descomprime el GZIP y retorna el JSON del reporte
    /// </summary>
    /// <param name="reportDocumentId">ID del documento de reporte obtenido cuando el estado es DONE</param>
    /// <param name="accessToken">Access token de Amazon (x-amz-access-token header)</param>
    /// <returns>Contenido JSON del reporte descomprimido</returns>
    [HttpGet("download")]
    [Authorize]
    public async Task<IActionResult> DownloadReport(
        [FromQuery] string reportDocumentId,
        [FromHeader(Name = "x-amz-access-token")] string accessToken)
    {
        // Validar el reportDocumentId
        if (string.IsNullOrWhiteSpace(reportDocumentId))
        {
            var errorResponse = ResponseStructure<object>.BadRequest(
                "El parámetro 'reportDocumentId' es requerido. " +
                "Debes proporcionar el ID del documento obtenido cuando el estado del reporte es DONE.");
            return BadRequest(errorResponse);
        }

        // Validar el access token
        if (string.IsNullOrEmpty(accessToken))
        {
            var errorResponse = ResponseStructure<object>.BadRequest(
                "El header 'x-amz-access-token' es requerido. Debes proporcionar el access token de Amazon generado previamente.");
            return BadRequest(errorResponse);
        }

        // Leer configuración de variables de entorno de Azure
        var vendorReportsSettings = _configuration.GetSection("AmazonVendorReports");
        var accessKey = vendorReportsSettings["AccessKey"] ?? string.Empty;
        var secretKey = vendorReportsSettings["SecretKey"] ?? string.Empty;
        var awsRegion = vendorReportsSettings["AwsRegion"] ?? "eu-west-1";
        var serviceName = vendorReportsSettings["ServiceName"] ?? "execute-api";
        var reportsBaseUrl = vendorReportsSettings["BaseUrl"] ?? "https://sellingpartnerapi-eu.amazon.com/reports/2021-06-30";

        // Validar que las variables de entorno estén configuradas
        if (string.IsNullOrEmpty(accessKey))
        {
            var errorResponse = ResponseStructure<object>.BadRequest(
                "La variable de entorno AmazonVendorReports__AccessKey no está configurada en Azure");
            return BadRequest(errorResponse);
        }

        if (string.IsNullOrEmpty(secretKey))
        {
            var errorResponse = ResponseStructure<object>.BadRequest(
                "La variable de entorno AmazonVendorReports__SecretKey no está configurada en Azure");
            return BadRequest(errorResponse);
        }

        try
        {
            var result = await _vendorReportService.GetAndDownloadReportDocumentAsync(
                reportDocumentId,
                accessToken,
                accessKey,
                secretKey,
                awsRegion,
                serviceName,
                reportsBaseUrl);

            if (!result.Success)
            {
                var errorResponse = ResponseStructure<object>.BadRequest(result.Message);
                return BadRequest(errorResponse);
            }

            var response = ResponseStructure<object>.Success(
                result.ReportContent ?? new object(),
                result.Message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error($"Error al descargar el reporte: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }
}
