using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Shared;

namespace BusinessLayer.BrandPartner.Commands;

/// <summary>
/// Command para actualizar un item de inventario
/// </summary>
public class UpdateInventoryItemCommand
{
    private readonly DBcontext _context;

    public UpdateInventoryItemCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task ExecuteAsync(long inventoryItemId, UpdateInventoryItemRequest request)
    {
        var entity = await _context.InventoryItems.FindAsync(inventoryItemId);
        if (entity == null)
        {
            throw new InvalidOperationException($"Inventory item with ID {inventoryItemId} was not found.");
        }

        // Actualizar campos (excepto SKU y AmazonAccountId que son clave)
        entity.Asin = request.Asin?.Trim();
        entity.ProductName = request.ProductName?.Trim();
        entity.PrepOwner = request.PrepOwner?.Trim();
        entity.LabelingOwner = request.LabelingOwner?.Trim();
        entity.UnitsPerBox = request.UnitsPerBox;
        entity.NumberOfBoxes = request.NumberOfBoxes;
        entity.BoxLengthIn = request.BoxLengthIn;
        entity.BoxWidthIn = request.BoxWidthIn;
        entity.BoxHeightIn = request.BoxHeightIn;
        entity.BoxWeightLb = request.BoxWeightLb;
        entity.UpdatedAt = DateTimeService.GetCostaRicaNow();

        await _context.SaveChangesAsync();
    }
}

/// <summary>
/// Request para actualizar item de inventario
/// </summary>
public class UpdateInventoryItemRequest
{
    public string? Asin { get; set; }
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
