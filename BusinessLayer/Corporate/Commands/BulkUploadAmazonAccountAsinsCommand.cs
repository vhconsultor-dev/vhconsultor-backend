using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Corporate.Entities;
using OfficeOpenXml;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command para carga masiva de ASINs desde archivo Excel
/// </summary>
public class BulkUploadAmazonAccountAsinsCommand
{
    private readonly DBcontext _context;

    public BulkUploadAmazonAccountAsinsCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Procesa un archivo Excel con ASINs y los carga masivamente
    /// </summary>
    public async Task<BulkUploadResult> ExecuteAsync(int amazonAccountId, IFormFile excelFile, string currentUser)
    {
        var result = new BulkUploadResult();

        try
        {
            // Validar que el AmazonAccount existe
            var accountExists = await _context.AmazonAccounts.AnyAsync(a => a.AmazonAccountId == amazonAccountId);
            if (!accountExists)
            {
                throw new BulkUploadValidationException(
                    $"Amazon Account with ID {amazonAccountId} does not exist in the database.",
                    "ACCOUNT_NOT_FOUND",
                    $"Please verify that the Amazon Account ID is correct. You can check existing accounts in the system before uploading."
                );
            }

            // Validar tamaño del archivo
            if (excelFile.Length == 0)
            {
                throw new BulkUploadValidationException(
                    "The uploaded file is empty (0 bytes).",
                    "EMPTY_FILE",
                    "Please select a valid Excel file with data. The file must contain at least the header row and one data row."
                );
            }

            // Validar tamaño máximo (10 MB configurado en Program.cs)
            const long maxFileSize = 10 * 1024 * 1024; // 10 MB
            if (excelFile.Length > maxFileSize)
            {
                throw new BulkUploadValidationException(
                    $"File size ({excelFile.Length / (1024.0 * 1024.0):F2} MB) exceeds the maximum allowed size of 10 MB.",
                    "FILE_TOO_LARGE",
                    "Please reduce the file size by splitting your data into multiple smaller files or removing unnecessary columns/rows."
                );
            }

            using var stream = new MemoryStream();
            await excelFile.CopyToAsync(stream);
            stream.Position = 0;

            // EPPlus 8+ requiere configurar la licencia antes de usar ExcelPackage.
            // Uso no comercial (organización). Para uso comercial: comprar licencia en epplussoftware.com
            ExcelPackage.License.SetNonCommercialOrganization("VHConsultor");

            ExcelPackage package;
            try
            {
                package = new ExcelPackage(stream);
            }
            catch (Exception ex)
            {
                throw new BulkUploadValidationException(
                    "Failed to open the Excel file. The file may be corrupted or in an unsupported format.",
                    "INVALID_EXCEL_FORMAT",
                    $"Please ensure the file is a valid Excel file (.xlsx or .xls). Try opening the file in Microsoft Excel and saving it again. Error details: {ex.Message}"
                );
            }

            using (package)
            {
                // Validar que el archivo tiene al menos una hoja
                if (package.Workbook.Worksheets.Count == 0)
                {
                    throw new BulkUploadValidationException(
                        "The Excel file does not contain any worksheets.",
                        "NO_WORKSHEETS",
                        "Please ensure the Excel file contains at least one worksheet with data."
                    );
                }

                var worksheet = package.Workbook.Worksheets[0]; // Primera hoja

                // Validar que la hoja tiene datos
                if (worksheet.Dimension == null)
                {
                    throw new BulkUploadValidationException(
                        "The first worksheet in the Excel file is empty.",
                        "EMPTY_WORKSHEET",
                        "Please ensure the first worksheet contains the header row (row 1) with column names: 'ASIN', 'Product Title', and optionally other columns."
                    );
                }

                // Validar columnas obligatorias
                var headerRow = 1;
                var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                
                for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                {
                    var headerValue = worksheet.Cells[headerRow, col].Text.Trim();
                    if (!string.IsNullOrWhiteSpace(headerValue))
                    {
                        headers[headerValue] = col;
                    }
                }

                // Validar que hay al menos una columna
                if (headers.Count == 0)
                {
                    throw new BulkUploadValidationException(
                        "No column headers found in row 1 of the Excel file.",
                        "NO_HEADERS",
                        "Please ensure row 1 contains column headers. Required columns are: 'ASIN' and 'Product Title'. Optional columns include: 'Category', 'SubCategory', 'ProductGroup', etc."
                    );
                }

                // Validar columnas requeridas
                var requiredColumns = new[] { "ASIN", "Product Title" };
                var missingColumns = requiredColumns.Where(c => !headers.ContainsKey(c)).ToList();
                if (missingColumns.Any())
                {
                    var foundColumns = string.Join("', '", headers.Keys);
                    throw new BulkUploadValidationException(
                        $"Missing required columns: {string.Join(", ", missingColumns)}",
                        "MISSING_REQUIRED_COLUMNS",
                        $"The Excel file must contain the following columns in row 1: 'ASIN' and 'Product Title'. " +
                        $"Found columns: '{foundColumns}'. " +
                        $"Please add the missing column(s) to the header row (case-insensitive)."
                    );
                }

                // Validar que hay filas de datos
                var totalRows = worksheet.Dimension.End.Row;
                if (totalRows <= headerRow)
                {
                    throw new BulkUploadValidationException(
                        "The Excel file contains only headers but no data rows.",
                        "NO_DATA_ROWS",
                        "Please add at least one row of data below the header row. Each row should contain an ASIN and Product Title at minimum."
                    );
                }

                result.TotalFilasLeidas = totalRows - headerRow; // Excluir header

                for (int row = headerRow + 1; row <= totalRows; row++)
                {
                    try
                    {
                        var asin = GetCellValue(worksheet, row, headers, "ASIN")?.Trim();

                        // Validar ASIN obligatorio
                        if (string.IsNullOrWhiteSpace(asin))
                        {
                            result.RegistrosConError++;
                            result.Errores.Add(new BulkUploadError
                            {
                                Fila = row,
                                Asin = string.Empty,
                                Error = "ASIN is required and cannot be empty.",
                                ErrorCode = "MISSING_ASIN",
                                Suggestion = $"Please provide a valid ASIN value in row {row}, column '{headers["ASIN"]}'."
                            });
                            continue;
                        }

                        // Validar Product Title obligatorio
                        var productTitle = GetCellValue(worksheet, row, headers, "Product Title")?.Trim();
                        if (string.IsNullOrWhiteSpace(productTitle))
                        {
                            result.RegistrosConError++;
                            result.Errores.Add(new BulkUploadError
                            {
                                Fila = row,
                                Asin = asin,
                                Error = "Product Title is required and cannot be empty.",
                                ErrorCode = "MISSING_PRODUCT_TITLE",
                                Suggestion = $"Please provide a Product Title for ASIN '{asin}' in row {row}."
                            });
                            continue;
                        }

                        // Verificar si ya existe (UK: AmazonAccountId + Asin)
                        var exists = await _context.AmazonAccountAsins
                            .AnyAsync(a => a.AmazonAccountId == amazonAccountId && a.Asin == asin);

                        if (exists)
                        {
                            result.RegistrosDuplicados++;
                            result.Errores.Add(new BulkUploadError
                            {
                                Fila = row,
                                Asin = asin,
                                Error = $"ASIN '{asin}' already exists for this Amazon Account.",
                                ErrorCode = "DUPLICATE_ASIN",
                                Suggestion = $"This ASIN is already registered. If you need to update it, please use the update endpoint instead of bulk upload.",
                                IsWarning = true
                            });
                            continue;
                        }

                        // Procesar catálogos con manejo de errores
                        int? categoryId = null;
                        int? subCategoryId = null;
                        int? productGroupId = null;
                        int? replenishmentCategoryId = null;

                        try
                        {
                            categoryId = await ProcessCategoryAsync(GetCellValue(worksheet, row, headers, "Category"), currentUser);
                            subCategoryId = await ProcessSubCategoryAsync(GetCellValue(worksheet, row, headers, "SubCategory"), categoryId, currentUser);
                            productGroupId = await ProcessProductGroupAsync(GetCellValue(worksheet, row, headers, "ProductGroup"), currentUser);
                            replenishmentCategoryId = await ProcessReplenishmentCategoryAsync(GetCellValue(worksheet, row, headers, "Replenishment Category"), currentUser);
                        }
                        catch (Exception catEx)
                        {
                            result.RegistrosConError++;
                            result.Errores.Add(new BulkUploadError
                            {
                                Fila = row,
                                Asin = asin,
                                Error = $"Error processing catalog data: {catEx.Message}",
                                ErrorCode = "CATALOG_ERROR",
                                Suggestion = "Please verify the format of Category/SubCategory/ProductGroup columns. Expected format: 'Code Description' (e.g., '2000 Hardware')."
                            });
                            continue;
                        }

                        // Crear registro
                        var asinRecord = new AmazonAccountAsin
                        {
                            AmazonAccountId = amazonAccountId,
                            Asin = asin,
                            ProductTitle = productTitle,
                            ManufacturerCode = GetCellValue(worksheet, row, headers, "Manufacturer Code"),
                            ParentAsin = GetCellValue(worksheet, row, headers, "Parent ASIN"),
                            Upc = GetCellValue(worksheet, row, headers, "UPC"),
                            Ean = GetCellValue(worksheet, row, headers, "EAN"),
                            Isbn = GetCellValue(worksheet, row, headers, "ISBN"),
                            ModelNumber = GetCellValue(worksheet, row, headers, "Model Number"),
                            CategoryId = categoryId,
                            SubCategoryId = subCategoryId,
                            ProductGroupId = productGroupId,
                            ReleaseDate = ParseDateTime(GetCellValue(worksheet, row, headers, "Release Date")),
                            ReplenishmentCategoryId = replenishmentCategoryId,
                            PrepInstructionsRequired = GetCellValue(worksheet, row, headers, "Prep Instructions Required"),
                            PrepInstructionsVendorState = GetCellValue(worksheet, row, headers, "Prep Instructions Vendor State"),
                            CreatedDate = DateTime.UtcNow.AddHours(-6), // Costa Rica time
                            CreatedBy = currentUser
                        };

                        _context.AmazonAccountAsins.Add(asinRecord);
                        
                        try
                        {
                            await _context.SaveChangesAsync();
                            result.RegistrosCargadosOk++;
                        }
                        catch (DbUpdateException dbEx)
                        {
                            result.RegistrosConError++;
                            var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                            result.Errores.Add(new BulkUploadError
                            {
                                Fila = row,
                                Asin = asin,
                                Error = $"Database error while saving: {innerMessage}",
                                ErrorCode = "DATABASE_ERROR",
                                Suggestion = "This may be due to a constraint violation or database connection issue. Please contact support if the problem persists."
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        result.RegistrosConError++;
                        result.Errores.Add(new BulkUploadError
                        {
                            Fila = row,
                            Asin = GetCellValue(worksheet, row, headers, "ASIN") ?? string.Empty,
                            Error = $"Unexpected error: {ex.Message}",
                            ErrorCode = "UNEXPECTED_ERROR",
                            Suggestion = $"An unexpected error occurred while processing row {row}. Please verify the data in this row and try again."
                        });
                    }
                }
            }

            return result;
        }
        catch (BulkUploadValidationException)
        {
            // Re-throw validation exceptions as-is
            throw;
        }
        catch (Exception ex)
        {
            throw new BulkUploadValidationException(
                $"An unexpected error occurred while processing the Excel file: {ex.Message}",
                "PROCESSING_ERROR",
                "Please verify the file format and content. If the problem persists, contact support with the error details."
            );
        }
    }

    private string? GetCellValue(ExcelWorksheet worksheet, int row, Dictionary<string, int> headers, string columnName)
    {
        if (!headers.ContainsKey(columnName))
            return null;

        var col = headers[columnName];
        var value = worksheet.Cells[row, col].Text?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private DateTime? ParseDateTime(string? dateValue)
    {
        if (string.IsNullOrWhiteSpace(dateValue))
            return null;

        if (DateTime.TryParse(dateValue, out var parsedDate))
            return parsedDate;

        return null;
    }

    private async Task<int?> ProcessCategoryAsync(string? categoryValue, string currentUser)
    {
        if (string.IsNullOrWhiteSpace(categoryValue))
            return null;

        // Formato: "2000 Hardware" → código=2000, nombre=Hardware
        var (code, name) = ParseCodeAndName(categoryValue);

        // Buscar por código o nombre
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryCode == code || c.CategoryName == name);

        if (category == null)
        {
            category = new Category
            {
                CategoryCode = code,
                CategoryName = name,
                IsActive = true,
                CreatedDate = DateTime.UtcNow.AddHours(-6)
            };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
        }

        return category.CategoryId;
    }

    private async Task<int?> ProcessSubCategoryAsync(string? subCategoryValue, int? categoryId, string currentUser)
    {
        if (string.IsNullOrWhiteSpace(subCategoryValue) || !categoryId.HasValue)
            return null;

        var (code, name) = ParseCodeAndName(subCategoryValue);

        var subCategory = await _context.SubCategories
            .FirstOrDefaultAsync(sc => sc.CategoryId == categoryId.Value && 
                                      (sc.SubCategoryCode == code || sc.SubCategoryName == name));

        if (subCategory == null)
        {
            subCategory = new SubCategory
            {
                CategoryId = categoryId.Value,
                SubCategoryCode = code,
                SubCategoryName = name,
                IsActive = true,
                CreatedDate = DateTime.UtcNow.AddHours(-6)
            };
            _context.SubCategories.Add(subCategory);
            await _context.SaveChangesAsync();
        }

        return subCategory.SubCategoryId;
    }

    private async Task<int?> ProcessProductGroupAsync(string? productGroupValue, string currentUser)
    {
        if (string.IsNullOrWhiteSpace(productGroupValue))
            return null;

        var (code, name) = ParseCodeAndName(productGroupValue);

        var productGroup = await _context.ProductGroups
            .FirstOrDefaultAsync(pg => pg.ProductGroupCode == code || pg.ProductGroupName == name);

        if (productGroup == null)
        {
            productGroup = new ProductGroup
            {
                ProductGroupCode = code,
                ProductGroupName = name,
                IsActive = true,
                CreatedDate = DateTime.UtcNow.AddHours(-6)
            };
            _context.ProductGroups.Add(productGroup);
            await _context.SaveChangesAsync();
        }

        return productGroup.ProductGroupId;
    }

    private async Task<int?> ProcessReplenishmentCategoryAsync(string? replenishmentValue, string currentUser)
    {
        if (string.IsNullOrWhiteSpace(replenishmentValue))
            return null;

        var (code, name) = ParseCodeAndName(replenishmentValue);

        var replenishmentCategory = await _context.ReplenishmentCategories
            .FirstOrDefaultAsync(rc => rc.ReplenishmentCategoryCode == code || rc.ReplenishmentCategoryName == name);

        if (replenishmentCategory == null)
        {
            replenishmentCategory = new ReplenishmentCategory
            {
                ReplenishmentCategoryCode = code,
                ReplenishmentCategoryName = name,
                IsActive = true,
                CreatedDate = DateTime.UtcNow.AddHours(-6)
            };
            _context.ReplenishmentCategories.Add(replenishmentCategory);
            await _context.SaveChangesAsync();
        }

        return replenishmentCategory.ReplenishmentCategoryId;
    }

    /// <summary>
    /// Separa código y descripción de formato "código descripción"
    /// </summary>
    private (string code, string name) ParseCodeAndName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return (value ?? string.Empty, value ?? string.Empty);

        var parts = value.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
        var code = parts[0].Trim();
        var name = parts.Length > 1 ? parts[1].Trim() : code;

        return (code, name);
    }
}

/// <summary>
/// Resultado de la carga masiva
/// </summary>
public class BulkUploadResult
{
    public int TotalFilasLeidas { get; set; }
    public int RegistrosCargadosOk { get; set; }
    public int RegistrosDuplicados { get; set; }
    public int RegistrosConError { get; set; }
    public List<BulkUploadError> Errores { get; set; } = new List<BulkUploadError>();
}

/// <summary>
/// Error de carga masiva con información detallada
/// </summary>
public class BulkUploadError
{
    public int Fila { get; set; }
    public string Asin { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
    public bool IsWarning { get; set; } = false;
}

/// <summary>
/// Excepción específica para validaciones de bulk upload con información detallada
/// </summary>
public class BulkUploadValidationException : Exception
{
    public string ErrorCode { get; }
    public string Suggestion { get; }

    public BulkUploadValidationException(string message, string errorCode, string suggestion) 
        : base(message)
    {
        ErrorCode = errorCode;
        Suggestion = suggestion;
    }
}
