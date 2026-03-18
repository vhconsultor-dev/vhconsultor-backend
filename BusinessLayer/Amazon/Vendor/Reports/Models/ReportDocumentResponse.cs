namespace BusinessLayer.Amazon.Vendor.Reports.Models;

/// <summary>
/// Respuesta de Amazon al solicitar el documento de un reporte (pre-signed URL + compresión)
/// </summary>
public class ReportDocumentResponse
{
    public string ReportDocumentId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string CompressionAlgorithm { get; set; } = string.Empty;
}

/// <summary>
/// Resultado de la descarga y descompresión del documento de reporte
/// </summary>
public class DownloadReportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Contenido JSON del reporte ya descomprimido, listo para serializar como objeto
    /// </summary>
    public object? ReportContent { get; set; }

    /// <summary>
    /// Algoritmo de compresión usado por Amazon (GZIP o vacío si no viene comprimido)
    /// </summary>
    public string CompressionAlgorithm { get; set; } = string.Empty;
}
