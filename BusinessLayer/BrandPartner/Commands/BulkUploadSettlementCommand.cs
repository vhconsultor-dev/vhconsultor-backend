using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.BrandPartner.Entities;
using ModelLayer.Shared;
using OfficeOpenXml;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BusinessLayer.BrandPartner.Commands;

/// <summary>
/// Command para carga masiva de settlement desde Excel de Amazon
/// Implementa validaciones estrictas e idempotencia por hash de línea
/// </summary>
public class BulkUploadSettlementCommand
{
    private readonly DBcontext _context;

    public BulkUploadSettlementCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task<BulkUploadSettlementResult> ExecuteAsync(int amazonAccountId, IFormFile excelFile, string currentUser)
    {
        var result = new BulkUploadSettlementResult
        {
            Success = true
        };

        try
        {
            // ==== VALIDACIÓN 1: Amazon Account existe ====
            var accountExists = await _context.AmazonAccounts.AnyAsync(a => a.AmazonAccountId == amazonAccountId);
            if (!accountExists)
            {
                throw new BulkUploadSettlementValidationException(
                    $"Amazon Account with ID {amazonAccountId} does not exist.",
                    $"La cuenta Amazon con ID {amazonAccountId} no existe.",
                    "ACCOUNT_NOT_FOUND",
                    "Please verify that the Amazon Account ID is correct."
                );
            }

            if (excelFile.Length == 0)
            {
                throw new BulkUploadSettlementValidationException(
                    "The uploaded file is empty (0 bytes).",
                    "El archivo subido está vacío (0 bytes).",
                    "EMPTY_FILE",
                    "Please select a valid Excel file with data."
                );
            }

            const long maxFileSize = 10 * 1024 * 1024; // 10 MB
            if (excelFile.Length > maxFileSize)
            {
                throw new BulkUploadSettlementValidationException(
                    $"File size ({excelFile.Length / (1024.0 * 1024.0):F2} MB) exceeds maximum of 10 MB.",
                    $"El tamaño del archivo ({excelFile.Length / (1024.0 * 1024.0):F2} MB) excede el máximo de 10 MB.",
                    "FILE_TOO_LARGE",
                    "Please reduce the file size."
                );
            }

            using var stream = new MemoryStream();
            await excelFile.CopyToAsync(stream);
            stream.Position = 0;

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            ExcelPackage package;
            try
            {
                package = new ExcelPackage(stream);
            }
            catch (Exception ex)
            {
                throw new BulkUploadSettlementValidationException(
                    "Failed to open Excel file. The file may be corrupted.",
                    "No se pudo abrir el archivo Excel. El archivo puede estar corrupto.",
                    "INVALID_EXCEL_FORMAT",
                    $"Please ensure the file is valid Excel (.xlsx). Error: {ex.Message}"
                );
            }

            using (package)
            {
                if (package.Workbook.Worksheets.Count == 0)
                {
                    throw new BulkUploadSettlementValidationException(
                        "The Excel file does not contain any worksheets.",
                        "El archivo Excel no contiene hojas de cálculo.",
                        "NO_WORKSHEETS",
                        "Please ensure the Excel file contains at least one worksheet with data."
                    );
                }

                var worksheet = package.Workbook.Worksheets[0];

                if (worksheet.Dimension == null)
                {
                    throw new BulkUploadSettlementValidationException(
                        "The worksheet is empty.",
                        "La hoja de cálculo está vacía.",
                        "EMPTY_WORKSHEET",
                        "Please provide a worksheet with headers and data rows."
                    );
                }

                // Extraer settlement-id de la primera fila de datos
                string? settlementId = worksheet.Cells[2, 1].Value?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(settlementId))
                {
                    throw new BulkUploadSettlementValidationException(
                        "Cannot determine settlement-id from the Excel file. First data row is missing settlement-id.",
                        "No se puede determinar el settlement-id del archivo Excel. La primera fila de datos no tiene settlement-id.",
                        "MISSING_SETTLEMENT_ID",
                        "Please ensure the first data row contains a valid settlement-id in column A."
                    );
                }

                result.SettlementId = settlementId;

                // ==== VALIDACIÓN 2: Settlement-id NO duplicado ====
                var existingHeader = await _context.SettlementHeaders
                    .FirstOrDefaultAsync(h => h.AmazonAccountId == amazonAccountId && h.SettlementId == settlementId);

                if (existingHeader != null)
                {
                    throw new BulkUploadSettlementValidationException(
                        $"Settlement ID {settlementId} has already been processed for this Amazon account. " +
                        $"Settlement was imported on {existingHeader.ImportedAt:yyyy-MM-dd HH:mm:ss}. " +
                        "If you need to reprocess, please delete the existing settlement first.",
                        $"El Settlement ID {settlementId} ya fue procesado para esta cuenta Amazon. " +
                        $"El settlement fue importado el {existingHeader.ImportedAt:yyyy-MM-dd HH:mm:ss}. " +
                        "Si necesita reprocesar, por favor elimine el settlement existente primero.",
                        "SETTLEMENT_ALREADY_EXISTS",
                        "Delete the existing settlement or verify you are uploading the correct file."
                    );
                }

                // ==== PARSEAR TODAS LAS LÍNEAS ====
                int totalRows = worksheet.Dimension.End.Row;
                result.TotalLinesProcessed = totalRows - 1; // excluir header

                var settlementRows = new List<SettlementRowData>();
                var skusInFile = new HashSet<string>();

                for (int row = 2; row <= totalRows; row++)
                {
                    var rowData = new SettlementRowData
                    {
                        RowNumber = row,
                        SettlementId = worksheet.Cells[row, 1].Value?.ToString()?.Trim(),
                        SettlementStartDate = ParseDateTime(worksheet.Cells[row, 2].Value?.ToString()),
                        SettlementEndDate = ParseDateTime(worksheet.Cells[row, 3].Value?.ToString()),
                        DepositDate = ParseDateTime(worksheet.Cells[row, 4].Value?.ToString()),
                        TotalAmount = ParseDecimal(worksheet.Cells[row, 5].Value?.ToString()),
                        Currency = worksheet.Cells[row, 6].Value?.ToString()?.Trim(),
                        TransactionType = worksheet.Cells[row, 7].Value?.ToString()?.Trim(),
                        OrderId = worksheet.Cells[row, 8].Value?.ToString()?.Trim(),
                        MerchantOrderId = worksheet.Cells[row, 9].Value?.ToString()?.Trim(),
                        AdjustmentId = worksheet.Cells[row, 10].Value?.ToString()?.Trim(),
                        ShipmentId = worksheet.Cells[row, 11].Value?.ToString()?.Trim(),
                        MarketplaceName = worksheet.Cells[row, 12].Value?.ToString()?.Trim(),
                        ShipmentFeeType = worksheet.Cells[row, 13].Value?.ToString()?.Trim(),
                        ShipmentFeeAmount = ParseDecimal(worksheet.Cells[row, 14].Value?.ToString()),
                        OrderFeeType = worksheet.Cells[row, 15].Value?.ToString()?.Trim(),
                        OrderFeeAmount = ParseDecimal(worksheet.Cells[row, 16].Value?.ToString()),
                        FulfillmentId = worksheet.Cells[row, 17].Value?.ToString()?.Trim(),
                        PostedDate = ParseDateTime(worksheet.Cells[row, 18].Value?.ToString()),
                        OrderItemCode = worksheet.Cells[row, 19].Value?.ToString()?.Trim(),
                        MerchantOrderItemId = worksheet.Cells[row, 20].Value?.ToString()?.Trim(),
                        MerchantAdjustmentItemId = worksheet.Cells[row, 21].Value?.ToString()?.Trim(),
                        Sku = worksheet.Cells[row, 22].Value?.ToString()?.Trim(),
                        QuantityPurchased = ParseDecimal(worksheet.Cells[row, 23].Value?.ToString()),
                        PriceType = worksheet.Cells[row, 24].Value?.ToString()?.Trim(),
                        PriceAmount = ParseDecimal(worksheet.Cells[row, 25].Value?.ToString()),
                        ItemRelatedFeeType = worksheet.Cells[row, 26].Value?.ToString()?.Trim(),
                        ItemRelatedFeeAmount = ParseDecimal(worksheet.Cells[row, 27].Value?.ToString()),
                        MiscFeeAmount = ParseDecimal(worksheet.Cells[row, 28].Value?.ToString()),
                        OtherFeeAmount = ParseDecimal(worksheet.Cells[row, 29].Value?.ToString()),
                        OtherFeeReasonDescription = worksheet.Cells[row, 30].Value?.ToString()?.Trim(),
                        PromotionId = worksheet.Cells[row, 31].Value?.ToString()?.Trim(),
                        PromotionType = worksheet.Cells[row, 32].Value?.ToString()?.Trim(),
                        PromotionAmount = ParseDecimal(worksheet.Cells[row, 33].Value?.ToString()),
                        DirectPaymentType = worksheet.Cells[row, 34].Value?.ToString()?.Trim(),
                        DirectPaymentAmount = ParseDecimal(worksheet.Cells[row, 35].Value?.ToString()),
                        OtherAmount = ParseDecimal(worksheet.Cells[row, 36].Value?.ToString())
                    };

                    settlementRows.Add(rowData);

                    if (!string.IsNullOrWhiteSpace(rowData.Sku))
                    {
                        skusInFile.Add(rowData.Sku);
                    }
                }

                // ==== FASE 1: Validar TODOS los SKUs existen ====
                if (skusInFile.Count > 0)
                {
                    var existingSkus = await _context.InventoryItems
                        .Where(i => i.AmazonAccountId == amazonAccountId && skusInFile.Contains(i.Sku))
                        .Select(i => i.Sku)
                        .ToListAsync();

                    var missingSKUs = skusInFile.Except(existingSkus).OrderBy(s => s).ToList();

                    if (missingSKUs.Any())
                    {
                        result.Success = false;
                        result.MissingSKUs = missingSKUs;
                        result.MessageEN = $"Cannot process settlement {settlementId}. The following {missingSKUs.Count} SKU(s) do not exist in inventory: [{string.Join(", ", missingSKUs)}]. " +
                                          "Please create these SKUs first using the inventory bulk upload or manual creation endpoint.";
                        result.MessageES = $"No se puede procesar el settlement {settlementId}. Los siguientes {missingSKUs.Count} SKU(s) no existen en el inventario: [{string.Join(", ", missingSKUs)}]. " +
                                          "Por favor cree estos SKUs primero usando la carga masiva de inventario o el endpoint de creación manual.";
                        return result;
                    }
                }

                // ==== CREAR SETTLEMENT HEADER ====
                var header = new SettlementHeader
                {
                    AmazonAccountId = amazonAccountId,
                    SettlementId = settlementId,
                    SettlementStartDate = settlementRows.FirstOrDefault()?.SettlementStartDate,
                    SettlementEndDate = settlementRows.FirstOrDefault()?.SettlementEndDate,
                    DepositDate = settlementRows.FirstOrDefault()?.DepositDate,
                    TotalAmount = settlementRows.FirstOrDefault()?.TotalAmount,
                    Currency = settlementRows.FirstOrDefault()?.Currency,
                    SourceFileName = excelFile.FileName,
                    ImportedAt = DateTimeService.GetCostaRicaNow(),
                    CreatedAt = DateTimeService.GetCostaRicaNow()
                };
                _context.SettlementHeaders.Add(header);
                await _context.SaveChangesAsync();

                // ==== PROCESAR LÍNEAS CON IDEMPOTENCIA ====
                var skusAffected = new Dictionary<long, InventoryItem>();
                var manualAdjustmentList = new List<SettlementLineRequiringAdjustment>();

                foreach (var rowData in settlementRows)
                {
                    try
                    {
                        // Calcular hash para idempotencia
                        var rowHash = CalculateRowHash(rowData);

                        // Verificar si ya fue procesada
                        var exists = await _context.SettlementDetails
                            .AnyAsync(d => d.SettlementHeaderId == header.SettlementHeaderId && d.RowHash == rowHash);

                        if (exists)
                        {
                            result.LinesSkipped++;
                            continue; // ya procesada
                        }

                        // Determinar si afecta inventario
                        bool affectsInventory = ShouldAffectInventory(rowData);
                        int inventoryDelta = 0;
                        long? inventoryItemId = null;

                        if (affectsInventory && !string.IsNullOrWhiteSpace(rowData.Sku))
                        {
                            var inventoryItem = await _context.InventoryItems
                                .FirstOrDefaultAsync(i => i.AmazonAccountId == amazonAccountId && i.Sku == rowData.Sku);

                            if (inventoryItem != null)
                            {
                                inventoryItemId = inventoryItem.InventoryItemId;
                                inventoryDelta = CalculateInventoryDelta(rowData);

                                // Validar si deja inventario en negativo
                                if (inventoryItem.QuantityOnHand + inventoryDelta < 0)
                                {
                                    // No procesar inventario, agregar a lista de ajuste manual
                                    affectsInventory = false;
                                    inventoryDelta = 0;

                                    manualAdjustmentList.Add(new SettlementLineRequiringAdjustment
                                    {
                                        RowNumber = rowData.RowNumber,
                                        Sku = rowData.Sku,
                                        TransactionType = rowData.TransactionType,
                                        QuantityRequired = Math.Abs(inventoryDelta),
                                        QuantityAvailable = inventoryItem.QuantityOnHand,
                                        Deficit = Math.Abs(inventoryItem.QuantityOnHand + inventoryDelta),
                                        OrderId = rowData.OrderId,
                                        PostedDate = rowData.PostedDate?.ToString("yyyy-MM-dd"),
                                        MessageEN = $"Insufficient inventory: SKU {rowData.Sku} requires {Math.Abs(inventoryDelta)} units but only {inventoryItem.QuantityOnHand} available. Deficit: {Math.Abs(inventoryItem.QuantityOnHand + inventoryDelta)} units. Manual adjustment needed.",
                                        MessageES = $"Inventario insuficiente: SKU {rowData.Sku} requiere {Math.Abs(inventoryDelta)} unidades pero solo hay {inventoryItem.QuantityOnHand} disponibles. Déficit: {Math.Abs(inventoryItem.QuantityOnHand + inventoryDelta)} unidades. Se requiere ajuste manual."
                                    });
                                }
                                else
                                {
                                    // Sí podemos aplicar el movimiento
                                    int qtyBefore = inventoryItem.QuantityOnHand;
                                    inventoryItem.QuantityOnHand += inventoryDelta;
                                    inventoryItem.UpdatedAt = DateTimeService.GetCostaRicaNow();

                                    // Guardar para snapshot
                                    if (!skusAffected.ContainsKey(inventoryItem.InventoryItemId))
                                    {
                                        skusAffected[inventoryItem.InventoryItemId] = inventoryItem;
                                    }
                                }
                            }
                        }

                        // Crear SettlementDetail
                        var detail = new SettlementDetail
                        {
                            SettlementHeaderId = header.SettlementHeaderId,
                            RowNumber = rowData.RowNumber,
                            TransactionType = rowData.TransactionType,
                            OrderId = rowData.OrderId,
                            MerchantOrderId = rowData.MerchantOrderId,
                            AdjustmentId = rowData.AdjustmentId,
                            ShipmentId = rowData.ShipmentId,
                            MarketplaceName = rowData.MarketplaceName,
                            ShipmentFeeType = rowData.ShipmentFeeType,
                            ShipmentFeeAmount = rowData.ShipmentFeeAmount,
                            OrderFeeType = rowData.OrderFeeType,
                            OrderFeeAmount = rowData.OrderFeeAmount,
                            FulfillmentId = rowData.FulfillmentId,
                            PostedDate = rowData.PostedDate,
                            OrderItemCode = rowData.OrderItemCode,
                            MerchantOrderItemId = rowData.MerchantOrderItemId,
                            MerchantAdjustmentItemId = rowData.MerchantAdjustmentItemId,
                            Sku = rowData.Sku,
                            QuantityPurchased = rowData.QuantityPurchased,
                            PriceType = rowData.PriceType,
                            PriceAmount = rowData.PriceAmount,
                            ItemRelatedFeeType = rowData.ItemRelatedFeeType,
                            ItemRelatedFeeAmount = rowData.ItemRelatedFeeAmount,
                            MiscFeeAmount = rowData.MiscFeeAmount,
                            OtherFeeAmount = rowData.OtherFeeAmount,
                            OtherFeeReasonDescription = rowData.OtherFeeReasonDescription,
                            PromotionId = rowData.PromotionId,
                            PromotionType = rowData.PromotionType,
                            PromotionAmount = rowData.PromotionAmount,
                            DirectPaymentType = rowData.DirectPaymentType,
                            DirectPaymentAmount = rowData.DirectPaymentAmount,
                            OtherAmount = rowData.OtherAmount,
                            AffectsInventory = affectsInventory,
                            InventoryDelta = affectsInventory ? inventoryDelta : null,
                            RowHash = rowHash,
                            RawRowJson = JsonSerializer.Serialize(rowData),
                            CreatedAt = DateTimeService.GetCostaRicaNow()
                        };
                        _context.SettlementDetails.Add(detail);
                        await _context.SaveChangesAsync();

                        // Si afectó inventario, crear InventoryMovement
                        if (affectsInventory && inventoryItemId.HasValue && inventoryDelta != 0)
                        {
                            var inventoryItem = await _context.InventoryItems.FindAsync(inventoryItemId.Value);
                            if (inventoryItem != null)
                            {
                                var movement = new InventoryMovement
                                {
                                    InventoryItemId = inventoryItemId.Value,
                                    SettlementHeaderId = header.SettlementHeaderId,
                                    SettlementDetailId = detail.SettlementDetailId,
                                    MovementType = "SETTLEMENT",
                                    ReasonCode = rowData.TransactionType,
                                    QuantityBefore = inventoryItem.QuantityOnHand - inventoryDelta,
                                    QuantityDelta = inventoryDelta,
                                    QuantityAfter = inventoryItem.QuantityOnHand,
                                    ReferenceType = "SettlementDetail",
                                    ReferenceId = detail.SettlementDetailId.ToString(),
                                    Comments = $"Settlement {settlementId} | {rowData.TransactionType} {rowData.OrderId} | SKU {rowData.Sku}",
                                    CreatedBy = $"system:settlement-import",
                                    CreatedAt = DateTimeService.GetCostaRicaNow()
                                };
                                _context.InventoryMovements.Add(movement);
                                await _context.SaveChangesAsync();
                            }
                        }

                        result.LinesCreated++;
                    }
                    catch (DbUpdateException dbEx)
                    {
                        result.Errores.Add(new BulkUploadSettlementError
                        {
                            Fila = rowData.RowNumber,
                            Error = $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}",
                            ErrorCode = "DB_ERROR"
                        });
                    }
                    catch (Exception ex)
                    {
                        result.Errores.Add(new BulkUploadSettlementError
                        {
                            Fila = rowData.RowNumber,
                            Error = ex.Message,
                            ErrorCode = "PROCESSING_ERROR"
                        });
                    }
                }

                // ==== FASE 4: Crear snapshots ====
                foreach (var kvp in skusAffected)
                {
                    var item = kvp.Value;
                    var snapshot = new InventorySnapshot
                    {
                        SettlementHeaderId = header.SettlementHeaderId,
                        InventoryItemId = item.InventoryItemId,
                        Sku = item.Sku,
                        QuantityBeforeSettlement = item.QuantityOnHand, // ya está actualizado
                        QuantityDeltaSettlement = 0, // calcular delta total
                        QuantityAfterSettlement = item.QuantityOnHand,
                        SnapshotDate = DateTimeService.GetCostaRicaNow(),
                        CreatedAt = DateTimeService.GetCostaRicaNow()
                    };
                    _context.InventorySnapshots.Add(snapshot);
                }
                await _context.SaveChangesAsync();

                result.SkusAffected = skusAffected.Count;
                result.LinesRequiringManualAdjustment = manualAdjustmentList;

                if (manualAdjustmentList.Any())
                {
                    result.MessageEN = $"Settlement {settlementId} was processed with warnings. {result.LinesCreated} lines applied, {result.LinesSkipped} lines skipped (already processed), " +
                                      $"{manualAdjustmentList.Count} lines require manual adjustment because they would leave inventory negative. " +
                                      $"Please review the linesRequiringManualAdjustment field for details.";
                    result.MessageES = $"El settlement {settlementId} se procesó con advertencias. {result.LinesCreated} líneas aplicadas, {result.LinesSkipped} líneas omitidas (ya procesadas), " +
                                      $"{manualAdjustmentList.Count} líneas requieren ajuste manual porque la operación dejaría el inventario en negativo. " +
                                      $"Revise el campo linesRequiringManualAdjustment para detalles.";
                }
                else
                {
                    result.MessageEN = $"Settlement {settlementId} processed successfully. {result.LinesCreated} lines applied, {result.LinesSkipped} lines skipped (already processed), {result.SkusAffected} SKUs affected.";
                    result.MessageES = $"Settlement {settlementId} procesado exitosamente. {result.LinesCreated} líneas aplicadas, {result.LinesSkipped} líneas omitidas (ya procesadas), {result.SkusAffected} SKUs afectados.";
                }
            }
        }
        catch (BulkUploadSettlementValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.MessageEN = $"Unexpected error: {ex.Message}";
            result.MessageES = $"Error inesperado: {ex.Message}";
            throw;
        }

        return result;
    }

    private string CalculateRowHash(SettlementRowData row)
    {
        var hashInput = $"{row.SettlementId}|{row.OrderId}|{row.Sku}|" +
                       $"{row.PostedDate:yyyy-MM-dd HH:mm:ss}|{row.PriceAmount}|" +
                       $"{row.TransactionType}|{row.QuantityPurchased}";
        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
        return Convert.ToBase64String(hashBytes);
    }

    private bool ShouldAffectInventory(SettlementRowData row)
    {
        if (string.IsNullOrWhiteSpace(row.Sku)) return false;
        if (!row.QuantityPurchased.HasValue || row.QuantityPurchased.Value == 0) return false;

        var txType = row.TransactionType?.ToUpper() ?? "";
        return txType == "ORDER" || txType == "REFUND";
    }

    private int CalculateInventoryDelta(SettlementRowData row)
    {
        if (!row.QuantityPurchased.HasValue) return 0;

        var txType = row.TransactionType?.ToUpper() ?? "";
        if (txType == "ORDER")
        {
            return -(int)row.QuantityPurchased.Value; // restar inventario
        }
        else if (txType == "REFUND")
        {
            return (int)row.QuantityPurchased.Value; // sumar inventario
        }
        return 0;
    }

    private DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTime.TryParse(value, out var result)) return result;
        return null;
    }

    private decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (decimal.TryParse(value, out var result)) return result;
        return null;
    }
}

/// <summary>
/// Datos de una fila de settlement
/// </summary>
public class SettlementRowData
{
    public int RowNumber { get; set; }
    public string? SettlementId { get; set; }
    public DateTime? SettlementStartDate { get; set; }
    public DateTime? SettlementEndDate { get; set; }
    public DateTime? DepositDate { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? Currency { get; set; }
    public string? TransactionType { get; set; }
    public string? OrderId { get; set; }
    public string? MerchantOrderId { get; set; }
    public string? AdjustmentId { get; set; }
    public string? ShipmentId { get; set; }
    public string? MarketplaceName { get; set; }
    public string? ShipmentFeeType { get; set; }
    public decimal? ShipmentFeeAmount { get; set; }
    public string? OrderFeeType { get; set; }
    public decimal? OrderFeeAmount { get; set; }
    public string? FulfillmentId { get; set; }
    public DateTime? PostedDate { get; set; }
    public string? OrderItemCode { get; set; }
    public string? MerchantOrderItemId { get; set; }
    public string? MerchantAdjustmentItemId { get; set; }
    public string? Sku { get; set; }
    public decimal? QuantityPurchased { get; set; }
    public string? PriceType { get; set; }
    public decimal? PriceAmount { get; set; }
    public string? ItemRelatedFeeType { get; set; }
    public decimal? ItemRelatedFeeAmount { get; set; }
    public decimal? MiscFeeAmount { get; set; }
    public decimal? OtherFeeAmount { get; set; }
    public string? OtherFeeReasonDescription { get; set; }
    public string? PromotionId { get; set; }
    public string? PromotionType { get; set; }
    public decimal? PromotionAmount { get; set; }
    public string? DirectPaymentType { get; set; }
    public decimal? DirectPaymentAmount { get; set; }
    public decimal? OtherAmount { get; set; }
}

/// <summary>
/// Resultado de carga masiva de settlement
/// </summary>
public class BulkUploadSettlementResult
{
    public bool Success { get; set; }
    public string MessageEN { get; set; } = string.Empty;
    public string MessageES { get; set; } = string.Empty;
    public string? SettlementId { get; set; }
    public int TotalLinesProcessed { get; set; }
    public int LinesSkipped { get; set; }
    public int LinesCreated { get; set; }
    public int SkusAffected { get; set; }
    public List<string> MissingSKUs { get; set; } = new();
    public List<SettlementLineRequiringAdjustment> LinesRequiringManualAdjustment { get; set; } = new();
    public List<BulkUploadSettlementError> Errores { get; set; } = new();
}

/// <summary>
/// Línea que requiere ajuste manual
/// </summary>
public class SettlementLineRequiringAdjustment
{
    public int RowNumber { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? TransactionType { get; set; }
    public int QuantityRequired { get; set; }
    public int QuantityAvailable { get; set; }
    public int Deficit { get; set; }
    public string? OrderId { get; set; }
    public string? PostedDate { get; set; }
    public string MessageEN { get; set; } = string.Empty;
    public string MessageES { get; set; } = string.Empty;
}

/// <summary>
/// Error de procesamiento de settlement
/// </summary>
public class BulkUploadSettlementError
{
    public int Fila { get; set; }
    public string Error { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
}

/// <summary>
/// Excepción para validaciones de settlement
/// </summary>
public class BulkUploadSettlementValidationException : Exception
{
    public string MessageES { get; }
    public string ErrorCode { get; }
    public string Suggestion { get; }

    public BulkUploadSettlementValidationException(string messageEN, string messageES, string errorCode, string suggestion)
        : base(messageEN)
    {
        MessageES = messageES;
        ErrorCode = errorCode;
        Suggestion = suggestion;
    }
}

/// <summary>
/// Request para bulk upload de settlement
/// </summary>
public class BulkUploadSettlementRequest
{
    public int AmazonAccountId { get; set; }
    public IFormFile ExcelFile { get; set; } = null!;
}
