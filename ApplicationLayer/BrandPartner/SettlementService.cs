using BusinessLayer.BrandPartner.Commands;
using BusinessLayer.BrandPartner.Queries;
using ModelLayer.BrandPartner.Entities;

namespace ApplicationLayer.BrandPartner;

/// <summary>
/// Service de orquestación para settlements
/// </summary>
public class SettlementService
{
    private readonly BulkUploadSettlementCommand _bulkUploadSettlementCommand;
    private readonly SettlementHeaderQueryRepository _settlementHeaderQueryRepository;
    private readonly SettlementDetailQueryRepository _settlementDetailQueryRepository;
    private readonly InventorySnapshotQueryRepository _inventorySnapshotQueryRepository;

    public SettlementService(
        BulkUploadSettlementCommand bulkUploadSettlementCommand,
        SettlementHeaderQueryRepository settlementHeaderQueryRepository,
        SettlementDetailQueryRepository settlementDetailQueryRepository,
        InventorySnapshotQueryRepository inventorySnapshotQueryRepository)
    {
        _bulkUploadSettlementCommand = bulkUploadSettlementCommand;
        _settlementHeaderQueryRepository = settlementHeaderQueryRepository;
        _settlementDetailQueryRepository = settlementDetailQueryRepository;
        _inventorySnapshotQueryRepository = inventorySnapshotQueryRepository;
    }

    /// <summary>
    /// Carga masiva de settlement desde datos JSON de Amazon
    /// </summary>
    public async Task<BulkUploadSettlementResult> BulkUploadSettlementAsync(BulkUploadSettlementRequest request, string currentUser)
    {
        return await _bulkUploadSettlementCommand.ExecuteAsync(request, currentUser);
    }

    /// <summary>
    /// Obtener settlements por AmazonAccount con filtros
    /// </summary>
    public async Task<IEnumerable<SettlementHeader>> GetSettlementHeadersAsync(int amazonAccountId, SettlementHeaderFilters filters)
    {
        return await _settlementHeaderQueryRepository.GetByAmazonAccountAsync(amazonAccountId, filters);
    }

    /// <summary>
    /// Obtener settlement header por ID
    /// </summary>
    public async Task<SettlementHeader?> GetSettlementHeaderByIdAsync(long settlementHeaderId)
    {
        return await _settlementHeaderQueryRepository.GetByIdAsync(settlementHeaderId);
    }

    /// <summary>
    /// Obtener detalles de un settlement específico
    /// </summary>
    public async Task<IEnumerable<SettlementDetail>> GetSettlementDetailsAsync(long settlementHeaderId, SettlementDetailFilters filters)
    {
        return await _settlementDetailQueryRepository.GetByHeaderIdAsync(settlementHeaderId, filters);
    }

    /// <summary>
    /// Obtener detalles por OrderId (buscar transacciones de una orden específica)
    /// </summary>
    public async Task<IEnumerable<SettlementDetail>> GetSettlementDetailsByOrderIdAsync(string orderId)
    {
        return await _settlementDetailQueryRepository.GetByOrderIdAsync(orderId);
    }

    /// <summary>
    /// Obtener snapshots de inventario por settlement
    /// </summary>
    public async Task<IEnumerable<InventorySnapshot>> GetSnapshotsBySettlementAsync(long settlementHeaderId)
    {
        return await _inventorySnapshotQueryRepository.GetByHeaderIdAsync(settlementHeaderId);
    }

    /// <summary>
    /// Obtener snapshots de inventario por item en un rango de fechas
    /// </summary>
    public async Task<IEnumerable<InventorySnapshot>> GetSnapshotsByInventoryItemAsync(long inventoryItemId, DateTime? dateFrom, DateTime? dateTo)
    {
        return await _inventorySnapshotQueryRepository.GetByInventoryItemAsync(inventoryItemId, dateFrom, dateTo);
    }
}
