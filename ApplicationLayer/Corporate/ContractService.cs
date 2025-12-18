using BusinessLayer.Corporate.Commands;
using BusinessLayer.Corporate.Queries;
using ModelLayer.Corporate.Entities;

namespace ApplicationLayer.Corporate;

/// <summary>
/// Servicio de aplicación para gestionar Contracts
/// </summary>
public class ContractService
{
    private readonly CreateContractCommand _createContractCommand;
    private readonly UpdateContractCommand _updateContractCommand;
    private readonly DeleteContractCommand _deleteContractCommand;
    private readonly ContractQueryRepository _contractQueryRepository;

    public ContractService(
        CreateContractCommand createContractCommand,
        UpdateContractCommand updateContractCommand,
        DeleteContractCommand deleteContractCommand,
        ContractQueryRepository contractQueryRepository)
    {
        _createContractCommand = createContractCommand;
        _updateContractCommand = updateContractCommand;
        _deleteContractCommand = deleteContractCommand;
        _contractQueryRepository = contractQueryRepository;
    }

    #region Commands

    /// <summary>
    /// Crea un nuevo contract
    /// </summary>
    public async Task<int> CreateContractAsync(CreateContractRequest request)
    {
        return await _createContractCommand.ExecuteAsync(request);
    }

    /// <summary>
    /// Actualiza un contract existente
    /// </summary>
    public async Task<bool> UpdateContractAsync(int contractId, UpdateContractRequest request)
    {
        return await _updateContractCommand.ExecuteAsync(contractId, request);
    }

    /// <summary>
    /// Elimina (soft delete) un contract
    /// </summary>
    public async Task<bool> DeleteContractAsync(int contractId, string? deletedBy = null)
    {
        return await _deleteContractCommand.ExecuteAsync(contractId, deletedBy);
    }

    #endregion

    #region Queries

    /// <summary>
    /// Obtiene contracts con filtros opcionales
    /// </summary>
    public async Task<IEnumerable<Contract>> GetContractsAsync(
        int? contractId = null,
        int? customerId = null,
        string? contractNumber = null,
        string? status = null,
        int? contractTypeId = null,
        string? currencyCode = null,
        DateTime? startDateFrom = null,
        DateTime? startDateTo = null,
        DateTime? endDateFrom = null,
        DateTime? endDateTo = null)
    {
        return await _contractQueryRepository.GetContractsAsync(
            contractId, customerId, contractNumber, status, contractTypeId,
            currencyCode, startDateFrom, startDateTo, endDateFrom, endDateTo);
    }

    /// <summary>
    /// Obtiene un contract por su ID
    /// </summary>
    public async Task<Contract?> GetContractByIdAsync(int contractId)
    {
        return await _contractQueryRepository.GetByIdAsync(contractId);
    }

    /// <summary>
    /// Obtiene contracts por customer ID
    /// </summary>
    public async Task<IEnumerable<Contract>> GetContractsByCustomerIdAsync(int customerId)
    {
        return await _contractQueryRepository.GetByCustomerIdAsync(customerId);
    }

    /// <summary>
    /// Obtiene contracts activos
    /// </summary>
    public async Task<IEnumerable<Contract>> GetActiveContractsAsync()
    {
        return await _contractQueryRepository.GetActiveContractsAsync();
    }

    #endregion
}

