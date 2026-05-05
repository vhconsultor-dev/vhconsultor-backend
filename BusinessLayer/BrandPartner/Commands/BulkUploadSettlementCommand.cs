using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.BrandPartner.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.BrandPartner.Commands;

/// <summary>
/// Command para carga masiva de settlement desde datos JSON de Amazon
/// </summary>
public class BulkUploadSettlementCommand
{
    private readonly DBcontext _context;

    public BulkUploadSettlementCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task<BulkUploadSettlementResult> ExecuteAsync(BulkUploadSettlementRequest request, string currentUser)
    {
        var result = new BulkUploadSettlementResult
        {
            Success = true
        };

        try
        {
            // ==== VALIDACIÓN 1: Amazon Account existe ====
            var accountExists = await _context.AmazonAccounts.AnyAsync(a => a.AmazonAccountId == request.AmazonAccountId);
            if (!accountExists)
            {
                throw new BulkUploadSettlementValidationException(
                    $"Amazon Account with ID {request.AmazonAccountId} does not exist.",
                    $"La cuenta Amazon con ID {request.AmazonAccountId} no existe.",
                    "ACCOUNT_NOT_FOUND",
                    "Please verify that the Amazon Account ID is correct."
                );
            }

            // ==== VALIDACIÓN 2: Hay datos para procesar ====
            if (request.SettlementTransactions == null || !request.SettlementTransactions.Any())
            {
                throw new BulkUploadSettlementValidationException(
                    "No settlement transactions provided for processing.",
                    "No se proporcionaron transacciones de settlement para procesar.",
                    "NO_TRANSACTIONS",
                    "Please provide at least one settlement transaction to upload."
                );
            }

            // ==== VALIDACIÓN 3: Límite de transacciones ====
            const int maxTransactionsPerRequest = 5000;
            if (request.SettlementTransactions.Count > maxTransactionsPerRequest)
            {
                throw new BulkUploadSettlementValidationException(
                    $"Too many transactions in request ({request.SettlementTransactions.Count}). Maximum allowed is {maxTransactionsPerRequest}.",
                    $"Demasiadas transacciones en el request ({request.SettlementTransactions.Count}). El máximo permitido es {maxTransactionsPerRequest}.",
                    "TOO_MANY_TRANSACTIONS",
                    $"Please split your request into smaller batches of {maxTransactionsPerRequest} transactions or less."
                );
            }

            // ==== VALIDACIÓN 4: Settlement ID consistente ====
            var settlementIds = request.SettlementTransactions.Select(t => t.SettlementId).Distinct().ToList();
            if (settlementIds.Count != 1)
            {
                throw new BulkUploadSettlementValidationException(
                    $"All transactions must have the same settlement-id. Found {settlementIds.Count} different settlement IDs: [{string.Join(", ", settlementIds)}].",
                    $"Todas las transacciones deben tener el mismo settlement-id. Se encontraron {settlementIds.Count} settlement IDs diferentes: [{string.Join(", ", settlementIds)}].",
                    "INCONSISTENT_SETTLEMENT_ID",
                    "Ensure all transactions belong to the same settlement batch."
                );
            }

            string settlementId = settlementIds.First();
            if (string.IsNullOrWhiteSpace(settlementId))
            {
                throw new BulkUploadSettlementValidationException(
                    "Settlement ID cannot be empty or null.",
                    "El Settlement ID no puede estar vacío o nulo.",
                    "MISSING_SETTLEMENT_ID",
                    "Please provide a valid settlement ID for all transactions."
                );
            }

            result.SettlementId = settlementId;

            // ==== VALIDACIÓN 5: Settlement-id NO duplicado ====
            var existingHeader = await _context.SettlementHeaders
                .FirstOrDefaultAsync(h => h.AmazonAccountId == request.AmazonAccountId && h.SettlementId == settlementId);

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

            // ==== PROCESAR Y VALIDAR DATOS ====
            result.TotalLinesProcessed = request.SettlementTransactions.Count;
            var settlementRows = new List<SettlementRowData>();
            var skusInFile = new HashSet<string>();

            for (int index = 0; index < request.SettlementTransactions.Count; index++)
            {
                var transaction = request.SettlementTransactions[index];
                int rowNumber = index + 1; // Para referencias consistentes

                var rowData = new SettlementRowData
                {
                    RowNumber = rowNumber,
                    SettlementId = transaction.SettlementId?.Trim(),
                    SettlementStartDate = transaction.SettlementStartDate,
                    SettlementEndDate = transaction.SettlementEndDate,
                    DepositDate = transaction.DepositDate,
                    TotalAmount = transaction.TotalAmount,
                    Currency = transaction.Currency?.Trim(),
                    TransactionType = transaction.TransactionType?.Trim(),
                    OrderId = transaction.OrderId?.Trim(),
                    MerchantOrderId = transaction.MerchantOrderId?.Trim(),
                    AdjustmentId = transaction.AdjustmentId?.Trim(),
                    ShipmentId = transaction.ShipmentId?.Trim(),
                    MarketplaceName = transaction.MarketplaceName?.Trim(),
                    ShipmentFeeType = transaction.ShipmentFeeType?.Trim(),
                    ShipmentFeeAmount = transaction.ShipmentFeeAmount,
                    OrderFeeType = transaction.OrderFeeType?.Trim(),
                    OrderFeeAmount = transaction.OrderFeeAmount,
                    FulfillmentId = transaction.FulfillmentId?.Trim(),
                    PostedDate = transaction.PostedDate,
                    OrderItemCode = transaction.OrderItemCode?.Trim(),
                    MerchantOrderItemId = transaction.MerchantOrderItemId?.Trim(),
                    MerchantAdjustmentItemId = transaction.MerchantAdjustmentItemId?.Trim(),
                    Sku = transaction.Sku?.Trim(),
                    QuantityPurchased = transaction.QuantityPurchased,
                    PriceType = transaction.PriceType?.Trim(),
                    PriceAmount = transaction.PriceAmount,
                    ItemRelatedFeeType = transaction.ItemRelatedFeeType?.Trim(),
                    ItemRelatedFeeAmount = transaction.ItemRelatedFeeAmount,
                    MiscFeeAmount = transaction.MiscFeeAmount,
                    OtherFeeAmount = transaction.OtherFeeAmount,
                    OtherFeeReasonDescription = transaction.OtherFeeReasonDescription?.Trim(),
                    PromotionId = transaction.PromotionId?.Trim(),
                    PromotionType = transaction.PromotionType?.Trim(),
                    PromotionAmount = transaction.PromotionAmount,
                    DirectPaymentType = transaction.DirectPaymentType?.Trim(),
                    DirectPaymentAmount = transaction.DirectPaymentAmount,
                    OtherAmount = transaction.OtherAmount
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
                    .Where(i => i.AmazonAccountId == request.AmazonAccountId && skusInFile.Contains(i.Sku))
                    .Select(i => i.Sku)
                    .ToListAsync();

                var missingSKUs = skusInFile.Except(existingSkus).OrderBy(s => s).ToList();

                if (missingSKUs.Any())
                {
                    result.Success = false;
                    result.MissingSKUs = missingSKUs;
                    result.MissingSkuDetails = BuildMissingSkuDetails(missingSKUs, settlementRows);
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
                AmazonAccountId = request.AmazonAccountId,
                SettlementId = settlementId,
                SettlementStartDate = settlementRows.FirstOrDefault()?.SettlementStartDate,
                SettlementEndDate = settlementRows.FirstOrDefault()?.SettlementEndDate,
                DepositDate = settlementRows.FirstOrDefault()?.DepositDate,
                TotalAmount = settlementRows.FirstOrDefault()?.TotalAmount,
                Currency = settlementRows.FirstOrDefault()?.Currency,
                SourceFileName = "JSON_IMPORT",
                ImportedAt = DateTimeService.GetCostaRicaNow(),
                CreatedAt = DateTimeService.GetCostaRicaNow()
            };
            _context.SettlementHeaders.Add(header);
            await _context.SaveChangesAsync();

                // ==== PROCESAR LÍNEAS ====
                var skusAffected = new Dictionary<long, InventoryItem>();
                var manualAdjustmentList = new List<SettlementLineRequiringAdjustment>();
                var orderIdsAddedInThisRequest = new HashSet<string>();
                var pendingInventoryMovements = new List<(SettlementDetail Detail, InventoryMovement Movement)>();

                foreach (var rowData in settlementRows)
                {
                    try
                    {
                        // Detectar duplicado por settlementId + orderId
                        if (!string.IsNullOrWhiteSpace(rowData.OrderId))
                        {
                            var existsInDatabase = await _context.SettlementDetails
                                .AnyAsync(d => d.SettlementHeaderId == header.SettlementHeaderId && d.OrderId == rowData.OrderId);

                            var duplicateEarlierInPayload = orderIdsAddedInThisRequest.Contains(rowData.OrderId);

                            if (existsInDatabase || duplicateEarlierInPayload)
                            {
                                result.LinesSkipped++;
                                var reason = duplicateEarlierInPayload ? "DUPLICATE_ORDER_IN_SAME_REQUEST" : "ORDER_ALREADY_STORED_FOR_THIS_SETTLEMENT";
                                result.LinesSkippedDetails.Add(new SettlementLineSkippedDetail
                                {
                                    RowNumber = rowData.RowNumber,
                                    SkipReasonCode = reason,
                                    MessageEN = duplicateEarlierInPayload
                                        ? $"Duplicate order: OrderId {rowData.OrderId} appears earlier in this request. Only the first occurrence is inserted."
                                        : $"OrderId {rowData.OrderId} was already stored for this settlement. Skipped to avoid duplicates.",
                                    MessageES = duplicateEarlierInPayload
                                        ? $"Orden duplicada: el OrderId {rowData.OrderId} ya aparece antes en esta petición. Solo se inserta la primera ocurrencia."
                                        : $"El OrderId {rowData.OrderId} ya estaba guardado para este settlement. Se omitió para evitar duplicados."
                                });
                                continue;
                            }
                        }

                        // Determinar si afecta inventario
                        bool affectsInventory = ShouldAffectInventory(rowData);
                        int inventoryDelta = 0;
                        long? inventoryItemId = null;

                        if (affectsInventory && !string.IsNullOrWhiteSpace(rowData.Sku))
                        {
                            var inventoryItem = await _context.InventoryItems
                                .FirstOrDefaultAsync(i => i.AmazonAccountId == request.AmazonAccountId && i.Sku == rowData.Sku);

                            if (inventoryItem != null)
                            {
                                inventoryItemId = inventoryItem.InventoryItemId;
                                inventoryDelta = CalculateInventoryDelta(rowData);

                                // Validar si deja inventario en negativo
                                if (inventoryItem.QuantityOnHand + inventoryDelta < 0)
                                {
                                    // No procesar inventario, agregar a lista de ajuste manual
                                    int deltaForReport = inventoryDelta; // Guardar delta ANTES de ponerlo en 0
                                    affectsInventory = false;
                                    inventoryDelta = 0;

                                    manualAdjustmentList.Add(new SettlementLineRequiringAdjustment
                                    {
                                        RowNumber = rowData.RowNumber,
                                        Sku = rowData.Sku,
                                        TransactionType = rowData.TransactionType,
                                        QuantityRequired = Math.Abs(deltaForReport),
                                        QuantityAvailable = inventoryItem.QuantityOnHand,
                                        Deficit = Math.Abs(inventoryItem.QuantityOnHand + deltaForReport),
                                        OrderId = rowData.OrderId,
                                        PostedDate = rowData.PostedDate?.ToString("yyyy-MM-dd"),
                                        MessageEN = $"Insufficient inventory: SKU {rowData.Sku} requires {Math.Abs(deltaForReport)} units but only {inventoryItem.QuantityOnHand} available. Deficit: {Math.Abs(inventoryItem.QuantityOnHand + deltaForReport)} units. Manual adjustment needed.",
                                        MessageES = $"Inventario insuficiente: SKU {rowData.Sku} requiere {Math.Abs(deltaForReport)} unidades pero solo hay {inventoryItem.QuantityOnHand} disponibles. Déficit: {Math.Abs(inventoryItem.QuantityOnHand + deltaForReport)} unidades. Se requiere ajuste manual."
                                    });
                                }
                                else
                                {
                                    // Sí podemos aplicar el movimiento
                                    // Guardar para snapshot ANTES de actualizar
                                    if (!skusAffected.ContainsKey(inventoryItem.InventoryItemId))
                                    {
                                        skusAffected[inventoryItem.InventoryItemId] = new InventoryItem
                                        {
                                            InventoryItemId = inventoryItem.InventoryItemId,
                                            Sku = inventoryItem.Sku,
                                            QuantityOnHand = inventoryItem.QuantityOnHand // cantidad ANTES
                                        };
                                    }
                                    
                                    inventoryItem.QuantityOnHand += inventoryDelta;
                                    inventoryItem.UpdatedAt = DateTimeService.GetCostaRicaNow();
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
                            CreatedAt = DateTimeService.GetCostaRicaNow()
                        };
                        _context.SettlementDetails.Add(detail);
                        if (!string.IsNullOrWhiteSpace(rowData.OrderId))
                            orderIdsAddedInThisRequest.Add(rowData.OrderId);

                        // Movimiento de inventario: se enlaza al SettlementDetailId después del primer SaveChanges
                        // (si no, SettlementDetailId queda 0 y falla la FK contra BrandPartner.SettlementDetails).
                        if (affectsInventory && inventoryItemId.HasValue && inventoryDelta != 0)
                        {
                            var inventoryItem = await _context.InventoryItems.FindAsync(inventoryItemId.Value);
                            if (inventoryItem != null)
                            {
                                var movement = new InventoryMovement
                                {
                                    InventoryItemId = inventoryItemId.Value,
                                    SettlementHeaderId = header.SettlementHeaderId,
                                    SettlementDetailId = null,
                                    MovementType = "SETTLEMENT",
                                    ReasonCode = rowData.TransactionType,
                                    QuantityBefore = inventoryItem.QuantityOnHand - inventoryDelta,
                                    QuantityDelta = inventoryDelta,
                                    QuantityAfter = inventoryItem.QuantityOnHand,
                                    ReferenceType = "SettlementDetail",
                                    ReferenceId = null,
                                    Comments = $"Settlement {settlementId} | {rowData.TransactionType} {rowData.OrderId} | SKU {rowData.Sku}",
                                    CreatedBy = currentUser,
                                    CreatedAt = DateTimeService.GetCostaRicaNow()
                                };
                                pendingInventoryMovements.Add((detail, movement));
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

                await using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1) Persistir detalles + actualizaciones de inventario (genera SettlementDetailId en las entidades rastreadas)
                    await _context.SaveChangesAsync();

                    foreach (var (detail, movement) in pendingInventoryMovements)
                    {
                        movement.SettlementDetailId = detail.SettlementDetailId;
                        movement.ReferenceId = detail.SettlementDetailId.ToString();
                        _context.InventoryMovements.Add(movement);
                    }

                    // ==== FASE 4: Crear snapshots ====
                    foreach (var kvp in skusAffected)
                    {
                        var itemSnapshot = kvp.Value;
                        var currentItem = await _context.InventoryItems.FindAsync(kvp.Key);

                        if (currentItem != null)
                        {
                            int qtyBefore = itemSnapshot.QuantityOnHand;
                            int qtyAfter = currentItem.QuantityOnHand;
                            int delta = qtyAfter - qtyBefore;

                            var snapshot = new InventorySnapshot
                            {
                                SettlementHeaderId = header.SettlementHeaderId,
                                InventoryItemId = currentItem.InventoryItemId,
                                Sku = currentItem.Sku,
                                QuantityBeforeSettlement = qtyBefore,
                                QuantityDeltaSettlement = delta,
                                QuantityAfterSettlement = qtyAfter,
                                SnapshotDate = DateTimeService.GetCostaRicaNow(),
                                CreatedAt = DateTimeService.GetCostaRicaNow()
                            };
                            _context.InventorySnapshots.Add(snapshot);
                        }
                    }

                    // 2) Movimientos de inventario + snapshots
                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                }
                catch
                {
                    await dbTransaction.RollbackAsync();
                    throw;
                }

            result.SkusAffected = skusAffected.Count;
            result.LinesRequiringManualAdjustment = manualAdjustmentList;

            result.LinesSkippedMeaningEN =
                "linesSkipped counts rows not inserted because the OrderId already existed for this settlement: DUPLICATE_ORDER_IN_SAME_REQUEST (same OrderId repeated in this payload) or ORDER_ALREADY_STORED_FOR_THIS_SETTLEMENT (OrderId already saved for this settlement header). Rows without OrderId are never skipped. See linesSkippedDetails per row.";
            result.LinesSkippedMeaningES =
                "linesSkipped cuenta filas no insertadas porque el OrderId ya existía para este settlement: DUPLICATE_ORDER_IN_SAME_REQUEST (mismo OrderId repetido en este payload) o ORDER_ALREADY_STORED_FOR_THIS_SETTLEMENT (OrderId ya guardado para este encabezado de settlement). Filas sin OrderId nunca se omiten. Ver linesSkippedDetails por fila.";

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

            // Ninguna línea aplicada pero hubo errores por fila (ej. esquema BD desactualizado)
            if (result.LinesCreated == 0 && result.Errores.Count > 0)
            {
                result.Success = false;
                result.MessageEN = $"Settlement {settlementId} could not apply detail lines. {result.Errores.Count} row error(s). First error: {result.Errores.First().Error}";
                result.MessageES = $"El settlement {settlementId} no pudo aplicar las líneas de detalle. {result.Errores.Count} error(es) por fila. Primer error: {result.Errores.First().Error}";
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


    private List<MissingSkuDetail> BuildMissingSkuDetails(List<string> missingSkus, List<SettlementRowData> settlementRows)
    {
        return settlementRows
            .Where(r => !string.IsNullOrWhiteSpace(r.Sku) && missingSkus.Contains(r.Sku!))
            .GroupBy(r => r.Sku!)
            .Select(g => new MissingSkuDetail
            {
                Sku = g.Key,
                TotalRows = g.Count(),
                TotalQuantityPurchased = g.Sum(x => x.QuantityPurchased ?? 0),
                Transactions = g.Select(x => new MissingSkuTransactionInfo
                {
                    RowNumber = x.RowNumber,
                    TransactionType = x.TransactionType,
                    OrderId = x.OrderId,
                    MerchantOrderId = x.MerchantOrderId,
                    QuantityPurchased = x.QuantityPurchased,
                    PostedDate = x.PostedDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                    MarketplaceName = x.MarketplaceName,
                    PriceType = x.PriceType,
                    PriceAmount = x.PriceAmount
                }).ToList()
            })
            .OrderBy(x => x.Sku)
            .ToList();
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
    /// <summary>Bilingual explanation of what linesSkipped means (always present for clarity).</summary>
    public string LinesSkippedMeaningEN { get; set; } = string.Empty;
    public string LinesSkippedMeaningES { get; set; } = string.Empty;
    /// <summary>One entry per skipped row: duplicate in payload or already persisted for this settlement header.</summary>
    public List<SettlementLineSkippedDetail> LinesSkippedDetails { get; set; } = new();
    public List<string> MissingSKUs { get; set; } = new();
    public List<MissingSkuDetail> MissingSkuDetails { get; set; } = new();
    public List<SettlementLineRequiringAdjustment> LinesRequiringManualAdjustment { get; set; } = new();
    public List<BulkUploadSettlementError> Errores { get; set; } = new();
}

/// <summary>
/// Línea omitida por duplicado de OrderId.
/// </summary>
public class SettlementLineSkippedDetail
{
    public int RowNumber { get; set; }
    /// <summary>DUPLICATE_ORDER_IN_SAME_REQUEST | ORDER_ALREADY_STORED_FOR_THIS_SETTLEMENT</summary>
    public string SkipReasonCode { get; set; } = string.Empty;
    public string MessageEN { get; set; } = string.Empty;
    public string MessageES { get; set; } = string.Empty;
}

public class MissingSkuDetail
{
    public string Sku { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public decimal TotalQuantityPurchased { get; set; }
    public List<MissingSkuTransactionInfo> Transactions { get; set; } = new();
}

public class MissingSkuTransactionInfo
{
    public int RowNumber { get; set; }
    public string? TransactionType { get; set; }
    public string? OrderId { get; set; }
    public string? MerchantOrderId { get; set; }
    public decimal? QuantityPurchased { get; set; }
    public string? PostedDate { get; set; }
    public string? MarketplaceName { get; set; }
    public string? PriceType { get; set; }
    public decimal? PriceAmount { get; set; }
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
/// Request para bulk upload de settlement con datos JSON
/// </summary>
public class BulkUploadSettlementRequest
{
    public int AmazonAccountId { get; set; }
    public List<SettlementTransactionRequest> SettlementTransactions { get; set; } = new();
}

/// <summary>
/// Datos de una transacción de settlement individual para bulk upload
/// </summary>
public class SettlementTransactionRequest
{
    public string SettlementId { get; set; } = string.Empty;
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
