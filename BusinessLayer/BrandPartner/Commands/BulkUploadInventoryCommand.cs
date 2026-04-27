using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.BrandPartner.Entities;
using ModelLayer.Shared;
using OfficeOpenXml;
using System.Text.Json;

namespace BusinessLayer.BrandPartner.Commands;

/// <summary>
/// Command para carga inicial masiva de inventario desde Excel
/// </summary>
public class BulkUploadInventoryCommand
{
    private readonly DBcontext _context;

    public BulkUploadInventoryCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task<BulkUploadInventoryResult> ExecuteAsync(int amazonAccountId, IFormFile excelFile, string currentUser)
    {
        var result = new BulkUploadInventoryResult
        {
            Success = true
        };

        try
        {
            // Validar que el AmazonAccount existe
            var accountExists = await _context.AmazonAccounts.AnyAsync(a => a.AmazonAccountId == amazonAccountId);
            if (!accountExists)
            {
                throw new BulkUploadInventoryValidationException(
                    $"Amazon Account with ID {amazonAccountId} does not exist.",
                    "ACCOUNT_NOT_FOUND",
                    "Please verify that the Amazon Account ID is correct."
                );
            }

            if (excelFile.Length == 0)
            {
                throw new BulkUploadInventoryValidationException(
                    "The uploaded file is empty (0 bytes).",
                    "EMPTY_FILE",
                    "Please select a valid Excel file with data."
                );
            }

            const long maxFileSize = 10 * 1024 * 1024; // 10 MB
            if (excelFile.Length > maxFileSize)
            {
                throw new BulkUploadInventoryValidationException(
                    $"File size ({excelFile.Length / (1024.0 * 1024.0):F2} MB) exceeds maximum of 10 MB.",
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
                throw new BulkUploadInventoryValidationException(
                    "Failed to open Excel file. The file may be corrupted.",
                    "INVALID_EXCEL_FORMAT",
                    $"Please ensure the file is valid Excel (.xlsx). Error: {ex.Message}"
                );
            }

            using (package)
            {
                if (package.Workbook.Worksheets.Count == 0)
                {
                    throw new BulkUploadInventoryValidationException(
                        "The Excel file does not contain any worksheets.",
                        "NO_WORKSHEETS",
                        "Please ensure the Excel file contains at least one worksheet with data."
                    );
                }

                var worksheet = package.Workbook.Worksheets[0];

                if (worksheet.Dimension == null)
                {
                    throw new BulkUploadInventoryValidationException(
                        "The worksheet is empty.",
                        "EMPTY_WORKSHEET",
                        "Please provide a worksheet with headers and data rows."
                    );
                }

                // Headers esperados
                var expectedHeaders = new List<string>
                {
                    "Merchant SKU", "Quantity", "Prep owner", "Labeling owner",
                    "Units per box", "Number of boxes", "Box length (in)", "Box width (in)",
                    "Box height (in)", "Box weight (lb)"
                };

                // Verificar headers
                var headers = new List<string>();
                for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                {
                    var headerCell = worksheet.Cells[1, col].Value?.ToString()?.Trim();
                    headers.Add(headerCell ?? "");
                }

                // Validar que los headers esperados estén presentes
                var missingHeaders = new List<string>();
                for (int i = 0; i < expectedHeaders.Count && i < headers.Count; i++)
                {
                    if (!string.Equals(headers[i], expectedHeaders[i], StringComparison.OrdinalIgnoreCase))
                    {
                        missingHeaders.Add(expectedHeaders[i]);
                    }
                }

                if (missingHeaders.Any())
                {
                    throw new BulkUploadInventoryValidationException(
                        $"Invalid Excel headers. Expected headers in order: [{string.Join(", ", expectedHeaders)}]. " +
                        $"Please ensure your Excel file has the correct column headers.",
                        "INVALID_HEADERS",
                        "Verify the Excel file structure matches the expected format."
                    );
                }

                // Procesar filas
                int totalRows = worksheet.Dimension.End.Row;
                result.TotalFilasLeidas = totalRows - 1; // excluir header

                for (int row = 2; row <= totalRows; row++)
                {
                    try
                    {
                        var sku = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                        if (string.IsNullOrWhiteSpace(sku))
                        {
                            result.RegistrosConError++;
                            result.Errores.Add(new BulkUploadInventoryError
                            {
                                Fila = row,
                                Sku = "",
                                Error = "SKU is required",
                                ErrorCode = "MISSING_SKU"
                            });
                            continue;
                        }

                        var quantityStr = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                        int quantity = 0;
                        if (!string.IsNullOrEmpty(quantityStr) && !int.TryParse(quantityStr, out quantity))
                        {
                            result.RegistrosConError++;
                            result.Errores.Add(new BulkUploadInventoryError
                            {
                                Fila = row,
                                Sku = sku,
                                Error = "Quantity must be a valid integer",
                                ErrorCode = "INVALID_QUANTITY"
                            });
                            continue;
                        }

                        // Buscar si el SKU ya existe para esta cuenta
                        var existingItem = await _context.InventoryItems
                            .FirstOrDefaultAsync(i => i.AmazonAccountId == amazonAccountId && i.Sku == sku);

                        if (existingItem != null)
                        {
                            // Guardar cantidad ANTES de actualizar
                            int qtyBefore = existingItem.QuantityOnHand;
                            
                            // Idempotente: actualizar
                            existingItem.ProductName = null; // asumiendo que no viene en este Excel
                            existingItem.PrepOwner = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                            existingItem.LabelingOwner = worksheet.Cells[row, 4].Value?.ToString()?.Trim();
                            existingItem.UnitsPerBox = ParseDecimal(worksheet.Cells[row, 5].Value?.ToString());
                            existingItem.NumberOfBoxes = ParseInt(worksheet.Cells[row, 6].Value?.ToString());
                            existingItem.BoxLengthIn = ParseDecimal(worksheet.Cells[row, 7].Value?.ToString());
                            existingItem.BoxWidthIn = ParseDecimal(worksheet.Cells[row, 8].Value?.ToString());
                            existingItem.BoxHeightIn = ParseDecimal(worksheet.Cells[row, 9].Value?.ToString());
                            existingItem.BoxWeightLb = ParseDecimal(worksheet.Cells[row, 10].Value?.ToString());
                            existingItem.QuantityOnHand = quantity;
                            existingItem.UpdatedAt = DateTimeService.GetCostaRicaNow();

                            await _context.SaveChangesAsync();

                            // Crear movimiento con datos correctos
                            int delta = quantity - qtyBefore;
                            var movement = new InventoryMovement
                            {
                                InventoryItemId = existingItem.InventoryItemId,
                                MovementType = "ADJUSTMENT",
                                ReasonCode = "BULK_UPDATE",
                                QuantityBefore = qtyBefore,
                                QuantityDelta = delta,
                                QuantityAfter = quantity,
                                ReferenceType = "BulkInventoryUpload",
                                Comments = $"Bulk upload update from file: {excelFile.FileName}",
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
                                AmazonAccountId = amazonAccountId,
                                Sku = sku,
                                PrepOwner = worksheet.Cells[row, 3].Value?.ToString()?.Trim(),
                                LabelingOwner = worksheet.Cells[row, 4].Value?.ToString()?.Trim(),
                                UnitsPerBox = ParseDecimal(worksheet.Cells[row, 5].Value?.ToString()),
                                NumberOfBoxes = ParseInt(worksheet.Cells[row, 6].Value?.ToString()),
                                BoxLengthIn = ParseDecimal(worksheet.Cells[row, 7].Value?.ToString()),
                                BoxWidthIn = ParseDecimal(worksheet.Cells[row, 8].Value?.ToString()),
                                BoxHeightIn = ParseDecimal(worksheet.Cells[row, 9].Value?.ToString()),
                                BoxWeightLb = ParseDecimal(worksheet.Cells[row, 10].Value?.ToString()),
                                QuantityOnHand = quantity,
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
                                QuantityDelta = quantity,
                                QuantityAfter = quantity,
                                ReferenceType = "BulkInventoryUpload",
                                Comments = $"Initial load from file: {excelFile.FileName}",
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
                            Fila = row,
                            Sku = worksheet.Cells[row, 1].Value?.ToString() ?? "",
                            Error = $"Database error: {innerMessage}",
                            ErrorCode = "DB_ERROR"
                        });
                    }
                    catch (Exception ex)
                    {
                        result.RegistrosConError++;
                        result.Errores.Add(new BulkUploadInventoryError
                        {
                            Fila = row,
                            Sku = worksheet.Cells[row, 1].Value?.ToString() ?? "",
                            Error = ex.Message,
                            ErrorCode = "PROCESSING_ERROR"
                        });
                    }
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

    private decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (decimal.TryParse(value, out var result)) return result;
        return null;
    }

    private int? ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (int.TryParse(value, out var result)) return result;
        return null;
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
/// Request para bulk upload de inventario
/// </summary>
public class BulkUploadInventoryRequest
{
    public int AmazonAccountId { get; set; }
    public IFormFile ExcelFile { get; set; } = null!;
}
