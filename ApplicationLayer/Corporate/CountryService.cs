using BusinessLayer.Corporate.Queries;
using ModelLayer.Corporate.Entities;

namespace ApplicationLayer.Corporate;

/// <summary>
/// Servicio de aplicación para gestionar Countries
/// </summary>
public class CountryService
{
    private readonly CountryQueryRepository _countryQueryRepository;

    public CountryService(CountryQueryRepository countryQueryRepository)
    {
        _countryQueryRepository = countryQueryRepository;
    }

    #region Queries

    /// <summary>
    /// Obtiene countries con filtros opcionales
    /// </summary>
    /// <param name="id">ID del country (opcional)</param>
    /// <param name="countryCode">Código del país (opcional)</param>
    /// <param name="countryName">Nombre del país para búsqueda parcial (opcional)</param>
    /// <param name="phoneCode">Código telefónico (opcional)</param>
    /// <param name="isActive">Filtrar por estado activo (opcional)</param>
    /// <returns>Lista de countries</returns>
    public async Task<IEnumerable<Country>> GetCountriesAsync(
        int? id = null,
        string? countryCode = null,
        string? countryName = null,
        string? phoneCode = null,
        bool? isActive = true)
    {
        return await _countryQueryRepository.GetCountriesAsync(id, countryCode, countryName, phoneCode, isActive);
    }

    /// <summary>
    /// Obtiene un country por su ID
    /// </summary>
    /// <param name="countryId">ID del country</param>
    /// <returns>Country o null si no existe</returns>
    public async Task<Country?> GetCountryByIdAsync(int countryId)
    {
        return await _countryQueryRepository.GetByIdAsync(countryId);
    }

    /// <summary>
    /// Obtiene un country por su código
    /// </summary>
    /// <param name="countryCode">Código del país</param>
    /// <returns>Country o null si no existe</returns>
    public async Task<Country?> GetCountryByCodeAsync(string countryCode)
    {
        return await _countryQueryRepository.GetByCodeAsync(countryCode);
    }

    /// <summary>
    /// Obtiene todos los countries activos
    /// </summary>
    /// <returns>Lista de countries activos</returns>
    public async Task<IEnumerable<Country>> GetAllActiveCountriesAsync()
    {
        return await _countryQueryRepository.GetAllActiveAsync();
    }

    #endregion
}

