using BusinessLayer.Corporate.Commands;
using Microsoft.AspNetCore.Http;

namespace ApplicationLayer.Corporate;

/// <summary>
/// Service para carga masiva de ASINs
/// </summary>
public class BulkUploadAmazonAccountAsinsService
{
    private readonly BulkUploadAmazonAccountAsinsCommand _bulkUploadCommand;

    public BulkUploadAmazonAccountAsinsService(BulkUploadAmazonAccountAsinsCommand bulkUploadCommand)
    {
        _bulkUploadCommand = bulkUploadCommand;
    }

    /// <summary>
    /// Procesa un archivo Excel con ASINs y los carga masivamente
    /// </summary>
    public async Task<BulkUploadResult> ProcessExcelAsync(int amazonAccountId, IFormFile excelFile, string currentUser)
    {
        return await _bulkUploadCommand.ExecuteAsync(amazonAccountId, excelFile, currentUser);
    }
}
