using ApplicationLayer.Corporate;
using ApiLayer.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ApiLayer.Controllers.Corporate;

/// <summary>
/// Controlador para gestionar Currencies
/// </summary>
[ApiController]
[Route("api/corporate/[controller]")]
[Authorize]
public class CurrencyController : ControllerBase
{
    private readonly CurrencyService _currencyService;

    public CurrencyController(CurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    #region GET - Get Currencies (Flexible Search)

    /// <summary>
    /// Obtiene currencies con búsqueda flexible usando parámetros opcionales
    /// </summary>
    /// <param name="currencyCode">Código de la moneda (si se especifica, devuelve solo esa currency)</param>
    /// <param name="currencyName">Nombre de la moneda para búsqueda parcial</param>
    /// <param name="currencySymbol">Símbolo de la moneda</param>
    /// <param name="isActive">Filtrar por estado activo (por defecto true)</param>
    /// <returns>Lista de currencies que coinciden con los criterios</returns>
    [HttpGet]
    public async Task<IActionResult> GetCurrencies(
        [FromQuery] string? currencyCode = null,
        [FromQuery] string? currencyName = null,
        [FromQuery] string? currencySymbol = null,
        [FromQuery] bool? isActive = true)
    {
        try
        {
            // Si se especifica código de moneda, buscar por código
            if (!string.IsNullOrEmpty(currencyCode))
            {
                var currency = await _currencyService.GetCurrencyByCodeAsync(currencyCode);
                
                if (currency == null)
                {
                    var notFoundResponse = ResponseStructure<object>.Error(
                        "Currency no encontrada", 
                        404);
                    return NotFound(notFoundResponse);
                }

                var singleResponse = ResponseStructure<ModelLayer.Corporate.Entities.Currency>.Success(
                    currency, 
                    "Currency obtenida exitosamente");
                
                return Ok(singleResponse);
            }

            // Devolver todas las currencies con los filtros aplicados
            var currencies = await _currencyService.GetCurrenciesAsync(currencyCode, currencyName, currencySymbol, isActive);
            
            var response = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.Currency>>.Success(
                currencies, 
                "Currencies obtenidas exitosamente");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener las currencies: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion
}

