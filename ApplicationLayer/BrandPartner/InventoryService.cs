using BusinessLayer.BrandPartner.Commands;
using BusinessLayer.BrandPartner.Queries;
using Microsoft.AspNetCore.Http;
using ModelLayer.BrandPartner.Entities;

namespace ApplicationLayer.BrandPartner;

/// <summary>
/// Service de orquestación para inventario
/// </summary>
public class InventoryService
{
    private readonly BulkUploadInventoryCommand _bulkUploadInventoryCommand;
    private readonly CreateInventoryItemCommand _createInventoryItemCommand;
    private readonly UpdateInventoryItemCommand _updateInventoryItemCommand;
    private readonly AdjustInventoryManualCommand _adjustInventoryManualCommand;
    private readonly InventoryItemQueryRepository _inventoryItemQueryRepository;
    private readonly InventoryMovementQueryRepository _inventoryMovementQueryRepository;

    public InventoryService(
        BulkUploadInventoryCommand bulkUploadInventoryCommand,
        CreateInventoryItemCommand createInventoryItemCommand,
        UpdateInventoryItemCommand updateInventoryItemCommand,
        AdjustInventoryManualCommand adjustInventoryManualCommand,
        InventoryItemQueryRepository inventoryItemQueryRepository,
        InventoryMovementQueryRepository inventoryMovementQueryRepository)
    {
        _bulkUploadInventoryCommand = bulkUploadInventoryCommand;
        _createInventoryItemCommand = createInventoryItemCommand;
        _updateInventoryItemCommand = updateInventoryItemCommand;
        _adjustInventoryManualCommand = adjustInventoryManualCommand;
        _inventoryItemQueryRepository = inventoryItemQueryRepository;
        _inventoryMovementQueryRepository = inventoryMovementQueryRepository;
    }

    /// <summary>
    /// Carga masiva de inventario desde Excel
    /// </summary>
    public async Task<BulkUploadInventoryResult> BulkUploadInventoryAsync(int amazonAccountId, IFormFile excelFile, string currentUser)
    {
        return await _bulkUploadInventoryCommand.ExecuteAsync(amazonAccountId, excelFile, currentUser);
    }

    /// <summary>
    /// Crear item de inventario manualmente
    /// </summary>
    public async Task<long> CreateInventoryItemAsync(CreateInventoryItemRequest request)
    {
        return await _createInventoryItemCommand.ExecuteAsync(request);
    }

    /// <summary>
    /// Actualizar item de inventario
    /// </summary>
    public async Task UpdateInventoryItemAsync(long inventoryItemId, UpdateInventoryItemRequest request)
    {
        await _updateInventoryItemCommand.ExecuteAsync(inventoryItemId, request);
    }

    /// <summary>
    /// Ajustar inventario manualmente
    /// </summary>
    public async Task<AdjustInventoryManualResult> AdjustInventoryManualAsync(AdjustInventoryManualRequest request)
    {
        return await _adjustInventoryManualCommand.ExecuteAsync(request);
    }

    /// <summary>
    /// Obtener inventario por AmazonAccount con filtros
    /// </summary>
    public async Task<IEnumerable<InventoryItem>> GetInventoryByAccountAsync(int amazonAccountId, InventoryItemFilters filters)
    {
        return await _inventoryItemQueryRepository.GetByAmazonAccountAsync(amazonAccountId, filters);
    }

    /// <summary>
    /// Obtener item de inventario por ID
    /// </summary>
    public async Task<InventoryItem?> GetInventoryItemByIdAsync(long inventoryItemId)
    {
        return await _inventoryItemQueryRepository.GetByIdAsync(inventoryItemId);
    }

    /// <summary>
    /// Obtener item de inventario por SKU
    /// </summary>
    public async Task<InventoryItem?> GetInventoryItemBySkuAsync(int amazonAccountId, string sku)
    {
        return await _inventoryItemQueryRepository.GetBySkuAsync(amazonAccountId, sku);
    }

    /// <summary>
    /// Obtener movimientos de inventario (auditoría)
    /// </summary>
    public async Task<IEnumerable<InventoryMovement>> GetInventoryMovementsAsync(long inventoryItemId, InventoryMovementFilters filters)
    {
        return await _inventoryMovementQueryRepository.GetByInventoryItemAsync(inventoryItemId, filters);
    }

    /// <summary>
    /// Obtener movimientos de inventario por AmazonAccount + InventoryItem (auditoría)
    /// </summary>
    public async Task<IEnumerable<InventoryMovement>> GetInventoryMovementsByAccountAsync(int amazonAccountId, long inventoryItemId, InventoryMovementFilters filters)
    {
        return await _inventoryMovementQueryRepository.GetByAccountAndInventoryItemAsync(amazonAccountId, inventoryItemId, filters);
    }
}
