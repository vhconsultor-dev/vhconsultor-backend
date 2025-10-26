using BusinessLayer.Corporate.Queries;
using ModelLayer.Corporate.Entities;

namespace ApplicationLayer.Corporate;

/// <summary>
/// Servicio de aplicación para gestionar Fee Types
/// </summary>
public class FeeTypeService
{
    private readonly FeeTypeQueryRepository _feeTypeQueryRepository;

    public FeeTypeService(FeeTypeQueryRepository feeTypeQueryRepository)
    {
        _feeTypeQueryRepository = feeTypeQueryRepository;
    }

    #region Queries

    /// <summary>
    /// Obtiene fee types con filtros opcionales
    /// </summary>
    /// <param name="id">ID del fee type (opcional)</param>
    /// <param name="typeName">Nombre del tipo para búsqueda parcial (opcional)</param>
    /// <param name="isActive">Filtrar por estado activo (opcional)</param>
    /// <returns>Lista de fee types</returns>
    public async Task<IEnumerable<FeeType>> GetFeeTypesAsync(
        int? id = null,
        string? typeName = null,
        bool? isActive = true)
    {
        return await _feeTypeQueryRepository.GetFeeTypesAsync(id, typeName, isActive);
    }

    /// <summary>
    /// Obtiene un fee type por su ID
    /// </summary>
    /// <param name="feeTypeId">ID del fee type</param>
    /// <returns>FeeType o null si no existe</returns>
    public async Task<FeeType?> GetFeeTypeByIdAsync(int feeTypeId)
    {
        return await _feeTypeQueryRepository.GetByIdAsync(feeTypeId);
    }

    /// <summary>
    /// Obtiene todos los fee types activos
    /// </summary>
    /// <returns>Lista de fee types activos</returns>
    public async Task<IEnumerable<FeeType>> GetAllActiveFeeTypesAsync()
    {
        return await _feeTypeQueryRepository.GetAllActiveAsync();
    }

    #endregion
}

