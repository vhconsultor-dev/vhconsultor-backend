using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.BrandPartner.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.BrandPartner.Commands;

/// <summary>
/// Command para ajuste manual de inventario (correcciones, daños, devoluciones)
/// </summary>
public class AdjustInventoryManualCommand
{
    private readonly DBcontext _context;

    public AdjustInventoryManualCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task<AdjustInventoryManualResult> ExecuteAsync(AdjustInventoryManualRequest request)
    {
        var entity = await _context.InventoryItems.FindAsync(request.InventoryItemId);
        if (entity == null)
        {
            throw new InvalidOperationException($"Inventory item with ID {request.InventoryItemId} was not found.");
        }

        int quantityBefore = entity.QuantityOnHand;
        int quantityAfter = quantityBefore + request.QuantityDelta;

        // Validar que no quede negativo
        if (quantityAfter < 0)
        {
            throw new InvalidOperationException(
                $"Cannot adjust inventory. Operation would result in negative quantity. " +
                $"Current: {quantityBefore}, Delta: {request.QuantityDelta}, Result: {quantityAfter}. " +
                $"EN: Adjustment rejected: would result in negative inventory. " +
                $"ES: Ajuste rechazado: resultaría en inventario negativo."
            );
        }

        // Actualizar cantidad
        entity.QuantityOnHand = quantityAfter;
        entity.UpdatedAt = DateTimeService.GetCostaRicaNow();

        // Crear movimiento
        var movement = new InventoryMovement
        {
            InventoryItemId = request.InventoryItemId,
            MovementType = "MANUAL",
            ReasonCode = request.ReasonCode,
            QuantityBefore = quantityBefore,
            QuantityDelta = request.QuantityDelta,
            QuantityAfter = quantityAfter,
            ReferenceType = "Manual",
            Comments = request.Comments,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTimeService.GetCostaRicaNow()
        };
        _context.InventoryMovements.Add(movement);

        await _context.SaveChangesAsync();

        return new AdjustInventoryManualResult
        {
            Success = true,
            MessageEN = $"Inventory adjusted successfully. SKU: {entity.Sku}, Before: {quantityBefore}, Delta: {request.QuantityDelta}, After: {quantityAfter}.",
            MessageES = $"Inventario ajustado exitosamente. SKU: {entity.Sku}, Antes: {quantityBefore}, Delta: {request.QuantityDelta}, Después: {quantityAfter}.",
            QuantityBefore = quantityBefore,
            QuantityDelta = request.QuantityDelta,
            QuantityAfter = quantityAfter
        };
    }
}

/// <summary>
/// Request para ajuste manual de inventario
/// </summary>
public class AdjustInventoryManualRequest
{
    public long InventoryItemId { get; set; }
    public int QuantityDelta { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public string? CreatedBy { get; set; }
}

/// <summary>
/// Resultado de ajuste manual de inventario
/// </summary>
public class AdjustInventoryManualResult
{
    public bool Success { get; set; }
    public string MessageEN { get; set; } = string.Empty;
    public string MessageES { get; set; } = string.Empty;
    public int QuantityBefore { get; set; }
    public int QuantityDelta { get; set; }
    public int QuantityAfter { get; set; }
}
