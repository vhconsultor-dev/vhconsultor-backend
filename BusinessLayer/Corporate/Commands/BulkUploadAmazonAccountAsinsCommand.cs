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

        // Validar que el AmazonAccount existe
        var accountExists = await _context.AmazonAccounts.AnyAsync(a => a.AmazonAccountId == amazonAccountId);
        if (!accountExists)
        {
            throw new InvalidOperationException($"Amazon Account with ID {amazonAccountId} does not exist.");
        }

        // Configurar EPPlus para uso no comercial (EPPlus 8+)
        // La licencia debe configurarse una sola vez al inicio de la aplicación
        // Por ahora, simplemente proceder sin configurar (asumiendo que ya está configurado en Program.cs)

        using var stream = new MemoryStream();
        await excelFile.CopyToAsync(stream);
        stream.Position = 0;

        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets[0]; // Primera hoja

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

        // Validar columnas requeridas
        var requiredColumns = new[] { "ASIN", "Product Title" };
        var missingColumns = requiredColumns.Where(c => !headers.ContainsKey(c)).ToList();
        if (missingColumns.Any())
        {
            throw new InvalidOperationException($"Missing required columns: {string.Join(", ", missingColumns)}");
        }

        // Procesar filas
        var totalRows = worksheet.Dimension.End.Row;
        result.TotalFilasLeidas = totalRows - 1; // Excluir header

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
                        Error = "ASIN is required"
                    });
                    continue;
                }

                // Verificar si ya existe (UK: AmazonAccountId + Asin)
                var exists = await _context.AmazonAccountAsins
                    .AnyAsync(a => a.AmazonAccountId == amazonAccountId && a.Asin == asin);

                if (exists)
                {
                    result.RegistrosDuplicados++;
                    continue;
                }

                // Procesar catálogos
                var categoryId = await ProcessCategoryAsync(GetCellValue(worksheet, row, headers, "Category"), currentUser);
                var subCategoryId = await ProcessSubCategoryAsync(GetCellValue(worksheet, row, headers, "SubCategory"), categoryId, currentUser);
                var productGroupId = await ProcessProductGroupAsync(GetCellValue(worksheet, row, headers, "ProductGroup"), currentUser);
                var replenishmentCategoryId = await ProcessReplenishmentCategoryAsync(GetCellValue(worksheet, row, headers, "Replenishment Category"), currentUser);

                // Crear registro
                var asinRecord = new AmazonAccountAsin
                {
                    AmazonAccountId = amazonAccountId,
                    Asin = asin,
                    ProductTitle = GetCellValue(worksheet, row, headers, "Product Title"),
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
                await _context.SaveChangesAsync();

                result.RegistrosCargadosOk++;
            }
            catch (Exception ex)
            {
                result.RegistrosConError++;
                result.Errores.Add(new BulkUploadError
                {
                    Fila = row,
                    Asin = GetCellValue(worksheet, row, headers, "ASIN") ?? string.Empty,
                    Error = ex.Message
                });
            }
        }

        return result;
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
/// Error de carga masiva
/// </summary>
public class BulkUploadError
{
    public int Fila { get; set; }
    public string Asin { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}
