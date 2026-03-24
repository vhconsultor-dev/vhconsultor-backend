namespace BusinessLayer.Amazon.Vendor.Reports.Models;

/// <summary>
/// Request para generar un reporte de inventario de Vendor
/// </summary>
public class GenerateVendorInventoryReportRequest
{
    public string ReportType { get; set; } = "GET_VENDOR_INVENTORY_REPORT";
    public List<string> MarketplaceIds { get; set; } = new();
    public string DataStartTime { get; set; } = string.Empty;
    public string DataEndTime { get; set; } = string.Empty;
    public VendorInventoryReportOptions? ReportOptions { get; set; }
}

/// <summary>
/// Opciones específicas del reporte de inventario
/// </summary>
public class VendorInventoryReportOptions
{
    public string ReportPeriod { get; set; } = "WEEK";
    public string SellingProgram { get; set; } = "RETAIL";
    public string DistributorView { get; set; } = "MANUFACTURING";
}
