using Dapper;
using Microsoft.Data.SqlClient;
using ModelLayer.BrandPartner.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.BrandPartner.Queries;

/// <summary>
/// Repository de consultas para InventoryItems
/// </summary>
public class InventoryItemQueryRepository
{
    private readonly IConnectionResolver _connectionResolver;

    public InventoryItemQueryRepository(IConnectionResolver connectionResolver)
    {
        _connectionResolver = connectionResolver;
    }

    public async Task<IEnumerable<InventoryItem>> GetByAmazonAccountAsync(int amazonAccountId, InventoryItemFilters filters)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                InventoryItemId, AmazonAccountId, Sku, Asin, ProductName,
                PrepOwner, LabelingOwner, UnitsPerBox, NumberOfBoxes,
                BoxLengthIn, BoxWidthIn, BoxHeightIn, BoxWeightLb,
                QuantityOnHand, CreatedAt, UpdatedAt
            FROM [BrandPartner].[InventoryItems]
            WHERE AmazonAccountId = @AmazonAccountId";

        var parameters = new DynamicParameters();
        parameters.Add("AmazonAccountId", amazonAccountId);

        if (!string.IsNullOrWhiteSpace(filters.Sku))
        {
            sql += " AND Sku LIKE @Sku";
            parameters.Add("Sku", $"%{filters.Sku}%");
        }

        if (!string.IsNullOrWhiteSpace(filters.Asin))
        {
            sql += " AND Asin LIKE @Asin";
            parameters.Add("Asin", $"%{filters.Asin}%");
        }

        if (!string.IsNullOrWhiteSpace(filters.PrepOwner))
        {
            sql += " AND PrepOwner = @PrepOwner";
            parameters.Add("PrepOwner", filters.PrepOwner);
        }

        sql += " ORDER BY CreatedAt DESC";

        if (filters.PageSize > 0)
        {
            int offset = (filters.PageNumber - 1) * filters.PageSize;
            sql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            parameters.Add("Offset", offset);
            parameters.Add("PageSize", filters.PageSize);
        }

        var items = await connection.QueryAsync<InventoryItem>(sql, parameters);
        return items;
    }

    public async Task<InventoryItem?> GetByIdAsync(long inventoryItemId)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                InventoryItemId, AmazonAccountId, Sku, Asin, ProductName,
                PrepOwner, LabelingOwner, UnitsPerBox, NumberOfBoxes,
                BoxLengthIn, BoxWidthIn, BoxHeightIn, BoxWeightLb,
                QuantityOnHand, CreatedAt, UpdatedAt
            FROM [BrandPartner].[InventoryItems]
            WHERE InventoryItemId = @InventoryItemId";

        return await connection.QueryFirstOrDefaultAsync<InventoryItem>(sql, new { InventoryItemId = inventoryItemId });
    }

    public async Task<InventoryItem?> GetBySkuAsync(int amazonAccountId, string sku)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT 
                InventoryItemId, AmazonAccountId, Sku, Asin, ProductName,
                PrepOwner, LabelingOwner, UnitsPerBox, NumberOfBoxes,
                BoxLengthIn, BoxWidthIn, BoxHeightIn, BoxWeightLb,
                QuantityOnHand, CreatedAt, UpdatedAt
            FROM [BrandPartner].[InventoryItems]
            WHERE AmazonAccountId = @AmazonAccountId AND Sku = @Sku";

        return await connection.QueryFirstOrDefaultAsync<InventoryItem>(sql, new { AmazonAccountId = amazonAccountId, Sku = sku });
    }

    public async Task<bool> SkuExistsAsync(int amazonAccountId, string sku)
    {
        var connectionString = _connectionResolver.GetConnectionString("VH-DB");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT CAST(CASE WHEN EXISTS(
                SELECT 1 FROM [BrandPartner].[InventoryItems]
                WHERE AmazonAccountId = @AmazonAccountId AND Sku = @Sku
            ) THEN 1 ELSE 0 END AS BIT)";

        return await connection.ExecuteScalarAsync<bool>(sql, new { AmazonAccountId = amazonAccountId, Sku = sku });
    }
}

/// <summary>
/// Filtros para consulta de inventario
/// </summary>
public class InventoryItemFilters
{
    public string? Sku { get; set; }
    public string? Asin { get; set; }
    public string? PrepOwner { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
