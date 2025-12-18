using BusinessLayer.Corporate.Commands;
using BusinessLayer.Corporate.Queries;
using ModelLayer.Corporate.Entities;

namespace ApplicationLayer.Corporate;

/// <summary>
/// Servicio de aplicación para gestionar Contract Services
/// </summary>
public class ContractServiceService
{
    private readonly CreateContractServiceCommand _createContractServiceCommand;
    private readonly UpdateContractServiceCommand _updateContractServiceCommand;
    private readonly DeleteContractServiceCommand _deleteContractServiceCommand;
    private readonly ContractServiceQueryRepository _contractServiceQueryRepository;

    public ContractServiceService(
        CreateContractServiceCommand createContractServiceCommand,
        UpdateContractServiceCommand updateContractServiceCommand,
        DeleteContractServiceCommand deleteContractServiceCommand,
        ContractServiceQueryRepository contractServiceQueryRepository)
    {
        _createContractServiceCommand = createContractServiceCommand;
        _updateContractServiceCommand = updateContractServiceCommand;
        _deleteContractServiceCommand = deleteContractServiceCommand;
        _contractServiceQueryRepository = contractServiceQueryRepository;
    }

    #region Commands

    /// <summary>
    /// Crea un nuevo contract service
    /// </summary>
    public async Task<int> CreateContractServiceAsync(CreateContractServiceRequest request)
    {
        return await _createContractServiceCommand.ExecuteAsync(request);
    }

    /// <summary>
    /// Actualiza un contract service existente
    /// </summary>
    public async Task<bool> UpdateContractServiceAsync(int contractServiceId, UpdateContractServiceRequest request)
    {
        return await _updateContractServiceCommand.ExecuteAsync(contractServiceId, request);
    }

    /// <summary>
    /// Elimina (soft delete) un contract service
    /// </summary>
    public async Task<bool> DeleteContractServiceAsync(int contractServiceId)
    {
        return await _deleteContractServiceCommand.ExecuteAsync(contractServiceId);
    }

    #endregion

    #region Queries

    /// <summary>
    /// Obtiene contract services con filtros opcionales
    /// </summary>
    public async Task<IEnumerable<ModelLayer.Corporate.Entities.ContractService>> GetContractServicesAsync(
        int? contractServiceId = null,
        int? contractId = null,
        int? serviceId = null,
        bool? isActive = true)
    {
        return await _contractServiceQueryRepository.GetContractServicesAsync(
            contractServiceId, contractId, serviceId, isActive);
    }

    /// <summary>
    /// Obtiene un contract service por su ID
    /// </summary>
    public async Task<ModelLayer.Corporate.Entities.ContractService?> GetContractServiceByIdAsync(int contractServiceId)
    {
        return await _contractServiceQueryRepository.GetByIdAsync(contractServiceId);
    }

    /// <summary>
    /// Obtiene contract services por contract ID
    /// </summary>
    public async Task<IEnumerable<ModelLayer.Corporate.Entities.ContractService>> GetContractServicesByContractIdAsync(int contractId, bool? isActive = true)
    {
        return await _contractServiceQueryRepository.GetByContractIdAsync(contractId, isActive);
    }

    /// <summary>
    /// Obtiene contract services activos
    /// </summary>
    public async Task<IEnumerable<ModelLayer.Corporate.Entities.ContractService>> GetAllActiveContractServicesAsync()
    {
        return await _contractServiceQueryRepository.GetAllActiveAsync();
    }

    /// <summary>
    /// Calcula el total de servicios de un contrato
    /// </summary>
    public async Task<decimal> GetContractTotalAsync(int contractId)
    {
        return await _contractServiceQueryRepository.GetContractTotalAsync(contractId);
    }

    #endregion
}

