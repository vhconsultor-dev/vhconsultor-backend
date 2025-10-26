using BusinessLayer.Corporate.Queries;
using ModelLayer.Corporate.Entities;

namespace ApplicationLayer.Corporate;

/// <summary>
/// Servicio de aplicación para gestionar Currencies
/// </summary>
public class CurrencyService
{
    private readonly CurrencyQueryRepository _currencyQueryRepository;

    public CurrencyService(CurrencyQueryRepository currencyQueryRepository)
    {
        _currencyQueryRepository = currencyQueryRepository;
    }

    #region Queries

    /// <summary>
    /// Obtiene currencies con filtros opcionales
    /// </summary>
    /// <param name="currencyCode">Código de la moneda (opcional)</param>
    /// <param name="currencyName">Nombre de la moneda para búsqueda parcial (opcional)</param>
    /// <param name="currencySymbol">Símbolo de la moneda (opcional)</param>
    /// <param name="isActive">Filtrar por estado activo (opcional)</param>
    /// <returns>Lista de currencies</returns>
    public async Task<IEnumerable<Currency>> GetCurrenciesAsync(
        string? currencyCode = null,
        string? currencyName = null,
        string? currencySymbol = null,
        bool? isActive = true)
    {
        return await _currencyQueryRepository.GetCurrenciesAsync(currencyCode, currencyName, currencySymbol, isActive);
    }

    /// <summary>
    /// Obtiene una currency por su código
    /// </summary>
    /// <param name="currencyCode">Código de la moneda</param>
    /// <returns>Currency o null si no existe</returns>
    public async Task<Currency?> GetCurrencyByCodeAsync(string currencyCode)
    {
        return await _currencyQueryRepository.GetByCodeAsync(currencyCode);
    }

    /// <summary>
    /// Obtiene todas las currencies activas
    /// </summary>
    /// <returns>Lista de currencies activas</returns>
    public async Task<IEnumerable<Currency>> GetAllActiveCurrenciesAsync()
    {
        return await _currencyQueryRepository.GetAllActiveAsync();
    }

    #endregion
}

