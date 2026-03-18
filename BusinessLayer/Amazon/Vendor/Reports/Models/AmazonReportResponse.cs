namespace BusinessLayer.Amazon.Vendor.Reports.Models;

/// <summary>
/// Respuesta de Amazon al crear un reporte
/// </summary>
public class AmazonReportResponse
{
    public string ReportId { get; set; } = string.Empty;
}

/// <summary>
/// Resultado de la creación de un reporte
/// </summary>
public class GenerateReportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ReportId { get; set; } = string.Empty;
}
