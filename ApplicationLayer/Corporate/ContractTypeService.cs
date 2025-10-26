using BusinessLayer.Corporate.Queries;
using ModelLayer.Corporate.Entities;

namespace ApplicationLayer.Corporate;

/// <summary>
/// Servicio de aplicación para gestionar Contract Types
/// </summary>
public class ContractTypeService
{
    private readonly ContractTypeQueryRepository _contractTypeQueryRepository;

    public ContractTypeService(ContractTypeQueryRepository contractTypeQueryRepository)
    {
        _contractTypeQueryRepository = contractTypeQueryRepository;
    }

    #region Queries

    /// <summary>
    /// Obtiene contract types con filtros opcionales
    /// </summary>
    /// <param name="id">ID del contract type (opcional)</param>
    /// <param name="typeName">Nombre del tipo para búsqueda parcial (opcional)</param>
    /// <param name="isActive">Filtrar por estado activo (opcional)</param>
    /// <returns>Lista de contract types</returns>
    public async Task<IEnumerable<ContractType>> GetContractTypesAsync(
        int? id = null,
        string? typeName = null,
        bool? isActive = true)
    {
        return await _contractTypeQueryRepository.GetContractTypesAsync(id, typeName, isActive);
    }

    /// <summary>
    /// Obtiene un contract type por su ID
    /// </summary>
    /// <param name="contractTypeId">ID del contract type</param>
    /// <returns>ContractType o null si no existe</returns>
    public async Task<ContractType?> GetContractTypeByIdAsync(int contractTypeId)
    {
        return await _contractTypeQueryRepository.GetByIdAsync(contractTypeId);
    }

    /// <summary>
    /// Obtiene todos los contract types activos
    /// </summary>
    /// <returns>Lista de contract types activos</returns>
    public async Task<IEnumerable<ContractType>> GetAllActiveContractTypesAsync()
    {
        return await _contractTypeQueryRepository.GetAllActiveAsync();
    }

    #endregion
}

