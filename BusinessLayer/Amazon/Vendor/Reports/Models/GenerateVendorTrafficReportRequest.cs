namespace BusinessLayer.Amazon.Vendor.Reports.Models;

/// <summary>
/// Request para generar un reporte de tráfico de Vendor
/// </summary>
public class GenerateVendorTrafficReportRequest
{
    public string ReportType { get; set; } = "GET_VENDOR_TRAFFIC_REPORT";
    public List<string> MarketplaceIds { get; set; } = new();
    public string DataStartTime { get; set; } = string.Empty;
    public string DataEndTime { get; set; } = string.Empty;
    public VendorTrafficReportOptions? ReportOptions { get; set; }
}

/// <summary>
/// Opciones específicas del reporte de tráfico
/// </summary>
public class VendorTrafficReportOptions
{
    public string ReportPeriod { get; set; } = "WEEK";
}
