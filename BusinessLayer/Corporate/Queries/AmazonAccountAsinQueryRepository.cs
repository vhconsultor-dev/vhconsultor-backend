using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Corporate.Entities;

namespace BusinessLayer.Corporate.Queries;

public class AmazonAccountAsinQueryRepository
{
    private readonly DBcontext _context;

    public AmazonAccountAsinQueryRepository(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene ASINs paginados de una cuenta Amazon con sus catálogos (optimizado con índices covering)
    /// </summary>
    public async Task<PaginatedAsinResult> GetAsinsPaginatedAsync(AsinQueryFilters filters)
    {
        var query = _context.AmazonAccountAsins
            .Where(x => x.AmazonAccountId == filters.AmazonAccountId);

        // Filtros opcionales
        if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
        {
            var searchLower = filters.SearchTerm.Trim().ToLower();
            query = query.Where(x => 
                x.Asin.ToLower().Contains(searchLower) ||
                (x.ProductTitle != null && x.ProductTitle.ToLower().Contains(searchLower)) ||
                (x.ParentAsin != null && x.ParentAsin.ToLower().Contains(searchLower))
            );
        }

        if (filters.CategoryId.HasValue)
            query = query.Where(x => x.CategoryId == filters.CategoryId.Value);

        if (filters.SubCategoryId.HasValue)
            query = query.Where(x => x.SubCategoryId == filters.SubCategoryId.Value);

        if (filters.ProductGroupId.HasValue)
            query = query.Where(x => x.ProductGroupId == filters.ProductGroupId.Value);

        if (filters.ReplenishmentCategoryId.HasValue)
            query = query.Where(x => x.ReplenishmentCategoryId == filters.ReplenishmentCategoryId.Value);

        // Contar total antes de paginar
        var totalRecords = await query.CountAsync();

        // Calcular paginación
        var totalPages = (int)Math.Ceiling(totalRecords / (double)filters.PageSize);
        var skip = (filters.PageNumber - 1) * filters.PageSize;

        // Obtener registros paginados con Include para catálogos (optimizado con índices)
        var asins = await query
            .OrderByDescending(x => x.CreatedDate)
            .ThenBy(x => x.Asin)
            .Skip(skip)
            .Take(filters.PageSize)
            .Select(a => new AsinDetailDto
            {
                AmazonAccountAsinId = a.AmazonAccountAsinId,
                AmazonAccountId = a.AmazonAccountId,
                Asin = a.Asin,
                ProductTitle = a.ProductTitle,
                ManufacturerCode = a.ManufacturerCode,
                ParentAsin = a.ParentAsin,
                Upc = a.Upc,
                Ean = a.Ean,
                Isbn = a.Isbn,
                ModelNumber = a.ModelNumber,
                ReleaseDate = a.ReleaseDate,
                PrepInstructionsRequired = a.PrepInstructionsRequired,
                PrepInstructionsVendorState = a.PrepInstructionsVendorState,
                CreatedDate = a.CreatedDate,
                CreatedBy = a.CreatedBy,
                ModifiedDate = a.ModifiedDate,
                ModifiedBy = a.ModifiedBy,
                // Catálogos - se resuelven con LEFT JOIN
                Category = a.CategoryId.HasValue
                    ? _context.Categories
                        .Where(c => c.CategoryId == a.CategoryId.Value)
                        .Select(c => new CatalogDto 
                        { 
                            Id = c.CategoryId, 
                            Code = c.CategoryCode, 
                            Name = c.CategoryName 
                        })
                        .FirstOrDefault()
                    : null,
                SubCategory = a.SubCategoryId.HasValue
                    ? _context.SubCategories
                        .Where(sc => sc.SubCategoryId == a.SubCategoryId.Value)
                        .Select(sc => new CatalogDto 
                        { 
                            Id = sc.SubCategoryId, 
                            Code = sc.SubCategoryCode, 
                            Name = sc.SubCategoryName 
                        })
                        .FirstOrDefault()
                    : null,
                ProductGroup = a.ProductGroupId.HasValue
                    ? _context.ProductGroups
                        .Where(pg => pg.ProductGroupId == a.ProductGroupId.Value)
                        .Select(pg => new CatalogDto 
                        { 
                            Id = pg.ProductGroupId, 
                            Code = pg.ProductGroupCode, 
                            Name = pg.ProductGroupName 
                        })
                        .FirstOrDefault()
                    : null,
                ReplenishmentCategory = a.ReplenishmentCategoryId.HasValue
                    ? _context.ReplenishmentCategories
                        .Where(rc => rc.ReplenishmentCategoryId == a.ReplenishmentCategoryId.Value)
                        .Select(rc => new CatalogDto 
                        { 
                            Id = rc.ReplenishmentCategoryId, 
                            Code = rc.ReplenishmentCategoryCode, 
                            Name = rc.ReplenishmentCategoryName 
                        })
                        .FirstOrDefault()
                    : null
            })
            .ToListAsync();

        return new PaginatedAsinResult
        {
            Data = asins,
            PageNumber = filters.PageNumber,
            PageSize = filters.PageSize,
            TotalRecords = totalRecords,
            TotalPages = totalPages,
            HasPreviousPage = filters.PageNumber > 1,
            HasNextPage = filters.PageNumber < totalPages
        };
    }

    /// <summary>
    /// Verifica si existe una cuenta Amazon
    /// </summary>
    public async Task<bool> AmazonAccountExistsAsync(int amazonAccountId)
    {
        return await _context.AmazonAccounts.AnyAsync(x => x.AmazonAccountId == amazonAccountId);
    }
}

/// <summary>
/// Filtros para consulta de ASINs
/// </summary>
public class AsinQueryFilters
{
    public int AmazonAccountId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 100;
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public int? SubCategoryId { get; set; }
    public int? ProductGroupId { get; set; }
    public int? ReplenishmentCategoryId { get; set; }
}

/// <summary>
/// DTO para un ASIN con información legible de catálogos
/// </summary>
public class AsinDetailDto
{
    public int AmazonAccountAsinId { get; set; }
    public int AmazonAccountId { get; set; }
    public string Asin { get; set; } = string.Empty;
    public string? ProductTitle { get; set; }
    public string? ManufacturerCode { get; set; }
    public string? ParentAsin { get; set; }
    public string? Upc { get; set; }
    public string? Ean { get; set; }
    public string? Isbn { get; set; }
    public string? ModelNumber { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string? PrepInstructionsRequired { get; set; }
    public string? PrepInstructionsVendorState { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
    
    // Catálogos en formato legible
    public CatalogDto? Category { get; set; }
    public CatalogDto? SubCategory { get; set; }
    public CatalogDto? ProductGroup { get; set; }
    public CatalogDto? ReplenishmentCategory { get; set; }
}

/// <summary>
/// DTO para información de catálogo (código + nombre)
/// </summary>
public class CatalogDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Resultado paginado con metadatos
/// </summary>
public class PaginatedAsinResult
{
    public List<AsinDetailDto> Data { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}
