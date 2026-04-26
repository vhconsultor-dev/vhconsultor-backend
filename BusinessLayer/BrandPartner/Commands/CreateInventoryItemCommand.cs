using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.BrandPartner.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.BrandPartner.Commands;

/// <summary>
/// Command para crear un item de inventario manualmente
/// </summary>
public class CreateInventoryItemCommand
{
    private readonly DBcontext _context;

    public CreateInventoryItemCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task<long> ExecuteAsync(CreateInventoryItemRequest request)
    {
        // Validar que AmazonAccount existe
        var accountExists = await _context.AmazonAccounts.AnyAsync(a => a.AmazonAccountId == request.AmazonAccountId);
        if (!accountExists)
        {
            throw new InvalidOperationException($"Amazon Account with ID {request.AmazonAccountId} does not exist.");
        }

        // Validar que SKU no esté duplicado
        var skuExists = await _context.InventoryItems
            .AnyAsync(i => i.AmazonAccountId == request.AmazonAccountId && i.Sku == request.Sku);
        if (skuExists)
        {
            throw new InvalidOperationException($"SKU '{request.Sku}' already exists for this Amazon account.");
        }

        var entity = new InventoryItem
        {
            AmazonAccountId = request.AmazonAccountId,
            Sku = request.Sku.Trim(),
            Asin = request.Asin?.Trim(),
            ProductName = request.ProductName?.Trim(),
            PrepOwner = request.PrepOwner?.Trim(),
            LabelingOwner = request.LabelingOwner?.Trim(),
            UnitsPerBox = request.UnitsPerBox,
            NumberOfBoxes = request.NumberOfBoxes,
            BoxLengthIn = request.BoxLengthIn,
            BoxWidthIn = request.BoxWidthIn,
            BoxHeightIn = request.BoxHeightIn,
            BoxWeightLb = request.BoxWeightLb,
            QuantityOnHand = request.QuantityOnHand,
            CreatedAt = DateTimeService.GetCostaRicaNow()
        };

        _context.InventoryItems.Add(entity);
        await _context.SaveChangesAsync();

        // Crear movimiento INITIAL_LOAD si quantity > 0
        if (request.QuantityOnHand > 0)
        {
            var movement = new InventoryMovement
            {
                InventoryItemId = entity.InventoryItemId,
                MovementType = "INITIAL_LOAD",
                ReasonCode = "MANUAL_CREATION",
                QuantityBefore = 0,
                QuantityDelta = request.QuantityOnHand,
                QuantityAfter = request.QuantityOnHand,
                ReferenceType = "Manual",
                Comments = "Manual inventory item creation",
                CreatedBy = request.CreatedBy,
                CreatedAt = DateTimeService.GetCostaRicaNow()
            };
            _context.InventoryMovements.Add(movement);
            await _context.SaveChangesAsync();
        }

        return entity.InventoryItemId;
    }
}

/// <summary>
/// Request para crear item de inventario
/// </summary>
public class CreateInventoryItemRequest
{
    public int AmazonAccountId { get; set; }
    public string Sku { get; set; } = string.Empty;
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
    public int QuantityOnHand { get; set; }
    public string? CreatedBy { get; set; }
}
