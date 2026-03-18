namespace BusinessLayer.Amazon.Vendor.Reports.Models;

/// <summary>
/// Respuesta de Amazon al consultar el estado de un reporte
/// </summary>
public class ReportStatusResponse
{
    public string ReportType { get; set; } = string.Empty;
    public string ProcessingEndTime { get; set; } = string.Empty;
    public string ProcessingStatus { get; set; } = string.Empty;
    public List<string> MarketplaceIds { get; set; } = new();
    public string ReportDocumentId { get; set; } = string.Empty;
    public string ReportId { get; set; } = string.Empty;
    public string DataEndTime { get; set; } = string.Empty;
    public string CreatedTime { get; set; } = string.Empty;
    public string ProcessingStartTime { get; set; } = string.Empty;
    public string DataStartTime { get; set; } = string.Empty;
}

/// <summary>
/// Resultado de la consulta del estado de un reporte
/// </summary>
public class GetReportStatusResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ReportStatusResponse? ReportStatus { get; set; }
}
