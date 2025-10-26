using BusinessLayer.Corporate.Queries;
using ModelLayer.Corporate.Entities;

namespace ApplicationLayer.Corporate;

/// <summary>
/// Servicio de aplicación para gestionar Industry Sectors
/// </summary>
public class IndustrySectorService
{
    private readonly IndustrySectorQueryRepository _industrySectorQueryRepository;

    public IndustrySectorService(IndustrySectorQueryRepository industrySectorQueryRepository)
    {
        _industrySectorQueryRepository = industrySectorQueryRepository;
    }

    #region Queries

    /// <summary>
    /// Obtiene industry sectors con filtros opcionales
    /// </summary>
    /// <param name="id">ID del sector (opcional)</param>
    /// <param name="sectorName">Nombre del sector para búsqueda parcial (opcional)</param>
    /// <param name="isActive">Filtrar por estado activo (opcional)</param>
    /// <returns>Lista de industry sectors</returns>
    public async Task<IEnumerable<IndustrySector>> GetIndustrySectorsAsync(
        int? id = null,
        string? sectorName = null,
        bool? isActive = true)
    {
        return await _industrySectorQueryRepository.GetIndustrySectorsAsync(id, sectorName, isActive);
    }

    /// <summary>
    /// Obtiene un industry sector por su ID
    /// </summary>
    /// <param name="sectorId">ID del sector</param>
    /// <returns>IndustrySector o null si no existe</returns>
    public async Task<IndustrySector?> GetIndustrySectorByIdAsync(int sectorId)
    {
        return await _industrySectorQueryRepository.GetByIdAsync(sectorId);
    }

    /// <summary>
    /// Obtiene todos los industry sectors activos
    /// </summary>
    /// <returns>Lista de industry sectors activos</returns>
    public async Task<IEnumerable<IndustrySector>> GetAllActiveIndustrySectorsAsync()
    {
        return await _industrySectorQueryRepository.GetAllActiveAsync();
    }

    #endregion
}

