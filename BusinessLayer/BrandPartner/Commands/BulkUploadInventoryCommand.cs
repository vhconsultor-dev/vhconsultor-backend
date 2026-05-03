using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.BrandPartner.Entities;
using ModelLayer.Shared;
using System.Text.Json;

namespace BusinessLayer.BrandPartner.Commands;

/// <summary>
/// Command para carga inicial masiva de inventario desde datos JSON
/// </summary>
public class BulkUploadInventoryCommand
{
    private readonly DBcontext _context;

    public BulkUploadInventoryCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task<BulkUploadInventoryResult> ExecuteAsync(BulkUploadInventoryRequest request, string currentUser)
    {
        var result = new BulkUploadInventoryResult
        {
            Success = true
        };

        try
        {
            // Validar que el AmazonAccount existe
            var accountExists = await _context.AmazonAccounts.AnyAsync(a => a.AmazonAccountId == request.AmazonAccountId);
            if (!accountExists)
            {
                throw new BulkUploadInventoryValidationException(
                    $"Amazon Account with ID {request.AmazonAccountId} does not exist.",
                    "ACCOUNT_NOT_FOUND",
                    "Please verify that the Amazon Account ID is correct."
                );
            }

            // Validar que hay items para procesar
            if (request.InventoryItems == null || !request.InventoryItems.Any())
            {
                throw new BulkUploadInventoryValidationException(
                    "No inventory items provided for processing.",
                    "NO_ITEMS",
                    "Please provide at least one inventory item to upload."
                );
            }

            // Validar límite de items por request
            const int maxItemsPerRequest = 1000;
            if (request.InventoryItems.Count > maxItemsPerRequest)
            {
                throw new BulkUploadInventoryValidationException(
                    $"Too many items in request ({request.InventoryItems.Count}). Maximum allowed is {maxItemsPerRequest}.",
                    "TOO_MANY_ITEMS",
                    $"Please split your request into smaller batches of {maxItemsPerRequest} items or less."
                );
            }

            result.TotalFilasLeidas = request.InventoryItems.Count;

            // Procesar cada item
            for (int index = 0; index < request.InventoryItems.Count; index++)
            {
                var itemRequest = request.InventoryItems[index];
                int rowNumber = index + 1; // Para que coincida con la fila del Excel original

                try
                {
                    // Validaciones básicas por item
                    if (string.IsNullOrWhiteSpace(itemRequest.Sku))
                    {
                        result.RegistrosConError++;
                        result.Errores.Add(new BulkUploadInventoryError
                        {
                            Fila = rowNumber,
                            Sku = "",
                            Error = "SKU is required",
                            ErrorCode = "MISSING_SKU"
                        });
                        continue;
                    }

                    // Limpiar el SKU
                    itemRequest.Sku = itemRequest.Sku.Trim();

                    // Validar cantidad
                    if (itemRequest.Quantity < 0)
                    {
                        result.RegistrosConError++;
                        result.Errores.Add(new BulkUploadInventoryError
                        {
                            Fila = rowNumber,
                            Sku = itemRequest.Sku,
                            Error = "Quantity cannot be negative",
                            ErrorCode = "INVALID_QUANTITY"
                        });
                        continue;
                    }

                    // Validar valores numéricos opcionales
                    if (itemRequest.UnitsPerBox.HasValue && itemRequest.UnitsPerBox.Value < 0)
                    {
                        result.RegistrosConError++;
                        result.Errores.Add(new BulkUploadInventoryError
                        {
                            Fila = rowNumber,
                            Sku = itemRequest.Sku,
                            Error = "Units per box cannot be negative",
                            ErrorCode = "INVALID_UNITS_PER_BOX"
                        });
                        continue;
                    }

                    if (itemRequest.NumberOfBoxes.HasValue && itemRequest.NumberOfBoxes.Value < 0)
                    {
                        result.RegistrosConError++;
                        result.Errores.Add(new BulkUploadInventoryError
                        {
                            Fila = rowNumber,
                            Sku = itemRequest.Sku,
                            Error = "Number of boxes cannot be negative",
                            ErrorCode = "INVALID_NUMBER_OF_BOXES"
                        });
                        continue;
                    }

                    // Validar dimensiones de caja si están presentes
                    if (itemRequest.BoxLengthIn.HasValue && itemRequest.BoxLengthIn.Value <= 0)
                    {
                        result.RegistrosConError++;
                        result.Errores.Add(new BulkUploadInventoryError
                        {
                            Fila = rowNumber,
                            Sku = itemRequest.Sku,
                            Error = "Box length must be greater than 0",
                            ErrorCode = "INVALID_BOX_LENGTH"
                        });
                        continue;
                    }

                    if (itemRequest.BoxWidthIn.HasValue && itemRequest.BoxWidthIn.Value <= 0)
                    {
                        result.RegistrosConError++;
                        result.Errores.Add(new BulkUploadInventoryError
                        {
                            Fila = rowNumber,
                            Sku = itemRequest.Sku,
                            Error = "Box width must be greater than 0",
                            ErrorCode = "INVALID_BOX_WIDTH"
                        });
                        continue;
                    }

                    if (itemRequest.BoxHeightIn.HasValue && itemRequest.BoxHeightIn.Value <= 0)
                    {
                        result.RegistrosConError++;
                        result.Errores.Add(new BulkUploadInventoryError
                        {
                            Fila = rowNumber,
                            Sku = itemRequest.Sku,
                            Error = "Box height must be greater than 0",
                            ErrorCode = "INVALID_BOX_HEIGHT"
                        });
                        continue;
                    }

                    if (itemRequest.BoxWeightLb.HasValue && itemRequest.BoxWeightLb.Value <= 0)
                    {
                        result.RegistrosConError++;
                        result.Errores.Add(new BulkUploadInventoryError
                        {
                            Fila = rowNumber,
                            Sku = itemRequest.Sku,
                            Error = "Box weight must be greater than 0",
                            ErrorCode = "INVALID_BOX_WEIGHT"
                        });
                        continue;
                    }

                    // Buscar si el SKU ya existe para esta cuenta
                    var existingItem = await _context.InventoryItems
                        .FirstOrDefaultAsync(i => i.AmazonAccountId == request.AmazonAccountId && i.Sku == itemRequest.Sku);

                    if (existingItem != null)
                    {
                        // Guardar cantidad ANTES de actualizar
                        int qtyBefore = existingItem.QuantityOnHand;
                        
                        // Idempotente: actualizar
                        existingItem.ProductName = itemRequest.ProductName?.Trim();
                        existingItem.PrepOwner = itemRequest.PrepOwner?.Trim();
                        existingItem.LabelingOwner = itemRequest.LabelingOwner?.Trim();
                        existingItem.UnitsPerBox = itemRequest.UnitsPerBox;
                        existingItem.NumberOfBoxes = itemRequest.NumberOfBoxes;
                        existingItem.BoxLengthIn = itemRequest.BoxLengthIn;
                        existingItem.BoxWidthIn = itemRequest.BoxWidthIn;
                        existingItem.BoxHeightIn = itemRequest.BoxHeightIn;
                        existingItem.BoxWeightLb = itemRequest.BoxWeightLb;
                        existingItem.QuantityOnHand = itemRequest.Quantity;
                        existingItem.UpdatedAt = DateTimeService.GetCostaRicaNow();

                        await _context.SaveChangesAsync();

                        // Crear movimiento con datos correctos
                        int delta = itemRequest.Quantity - qtyBefore;
                        var movement = new InventoryMovement
                        {
                            InventoryItemId = existingItem.InventoryItemId,
                            MovementType = "ADJUSTMENT",
                            ReasonCode = "BULK_UPDATE",
                            QuantityBefore = qtyBefore,
                            QuantityDelta = delta,
                            QuantityAfter = itemRequest.Quantity,
                            ReferenceType = "BulkInventoryUpload",
                            Comments = $"Bulk upload update from JSON data",
                            CreatedBy = currentUser,
                            CreatedAt = DateTimeService.GetCostaRicaNow()
                        };
                        _context.InventoryMovements.Add(movement);
                        await _context.SaveChangesAsync();

                        result.RegistrosActualizados++;
                    }
                    else
                    {
                        // Crear nuevo
                        var newItem = new InventoryItem
                        {
                            AmazonAccountId = request.AmazonAccountId,
                            Sku = itemRequest.Sku,
                            ProductName = itemRequest.ProductName?.Trim(),
                            PrepOwner = itemRequest.PrepOwner?.Trim(),
                            LabelingOwner = itemRequest.LabelingOwner?.Trim(),
                            UnitsPerBox = itemRequest.UnitsPerBox,
                            NumberOfBoxes = itemRequest.NumberOfBoxes,
                            BoxLengthIn = itemRequest.BoxLengthIn,
                            BoxWidthIn = itemRequest.BoxWidthIn,
                            BoxHeightIn = itemRequest.BoxHeightIn,
                            BoxWeightLb = itemRequest.BoxWeightLb,
                            QuantityOnHand = itemRequest.Quantity,
                            CreatedAt = DateTimeService.GetCostaRicaNow()
                        };
                        _context.InventoryItems.Add(newItem);
                        await _context.SaveChangesAsync();

                        // Crear movimiento INITIAL_LOAD
                        var movement = new InventoryMovement
                        {
                            InventoryItemId = newItem.InventoryItemId,
                            MovementType = "INITIAL_LOAD",
                            ReasonCode = "BULK_UPLOAD",
                            QuantityBefore = 0,
                            QuantityDelta = itemRequest.Quantity,
                            QuantityAfter = itemRequest.Quantity,
                            ReferenceType = "BulkInventoryUpload",
                            Comments = $"Initial load from JSON data",
                            CreatedBy = currentUser,
                            CreatedAt = DateTimeService.GetCostaRicaNow()
                        };
                        _context.InventoryMovements.Add(movement);
                        await _context.SaveChangesAsync();

                        result.RegistrosCargadosOk++;
                    }
                }
                catch (DbUpdateException dbEx)
                {
                    result.RegistrosConError++;
                    var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                    result.Errores.Add(new BulkUploadInventoryError
                    {
                        Fila = rowNumber,
                        Sku = itemRequest?.Sku ?? "",
                        Error = $"Database error: {innerMessage}",
                        ErrorCode = "DB_ERROR"
                    });
                }
                catch (Exception ex)
                {
                    result.RegistrosConError++;
                    result.Errores.Add(new BulkUploadInventoryError
                    {
                        Fila = rowNumber,
                        Sku = itemRequest?.Sku ?? "",
                        Error = ex.Message,
                        ErrorCode = "PROCESSING_ERROR"
                    });
                }
            }

            result.MessageEN = $"Inventory bulk upload completed. {result.RegistrosCargadosOk} created, {result.RegistrosActualizados} updated, {result.RegistrosConError} errors.";
            result.MessageES = $"Carga masiva de inventario completada. {result.RegistrosCargadosOk} creados, {result.RegistrosActualizados} actualizados, {result.RegistrosConError} errores.";
        }
        catch (BulkUploadInventoryValidationException)
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
}

/// <summary>
/// Resultado de carga masiva de inventario
/// </summary>
public class BulkUploadInventoryResult
{
    public bool Success { get; set; }
    public string MessageEN { get; set; } = string.Empty;
    public string MessageES { get; set; } = string.Empty;
    public int TotalFilasLeidas { get; set; }
    public int RegistrosCargadosOk { get; set; }
    public int RegistrosActualizados { get; set; }
    public int RegistrosConError { get; set; }
    public List<BulkUploadInventoryError> Errores { get; set; } = new();
}

/// <summary>
/// Error de carga masiva de inventario
/// </summary>
public class BulkUploadInventoryError
{
    public int Fila { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
}

/// <summary>
/// Excepción para validaciones de bulk upload de inventario
/// </summary>
public class BulkUploadInventoryValidationException : Exception
{
    public string ErrorCode { get; }
    public string Suggestion { get; }

    public BulkUploadInventoryValidationException(string message, string errorCode, string suggestion)
        : base(message)
    {
        ErrorCode = errorCode;
        Suggestion = suggestion;
    }
}

/// <summary>
/// Request para bulk upload de inventario con datos JSON
/// </summary>
public class BulkUploadInventoryRequest
{
    public int AmazonAccountId { get; set; }
    public List<InventoryItemRequest> InventoryItems { get; set; } = new();
}

/// <summary>
/// Datos de un item de inventario individual para bulk upload
/// </summary>
public class InventoryItemRequest
{
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? ProductName { get; set; }
    public string? PrepOwner { get; set; }
    public string? LabelingOwner { get; set; }
    public decimal? UnitsPerBox { get; set; }
    public int? NumberOfBoxes { get; set; }
    public decimal? BoxLengthIn { get; set; }
    public decimal? BoxWidthIn { get; set; }
    public decimal? BoxHeightIn { get; set; }
    public decimal? BoxWeightLb { get; set; }
}
