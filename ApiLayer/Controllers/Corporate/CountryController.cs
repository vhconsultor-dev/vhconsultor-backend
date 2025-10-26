using ApplicationLayer.Corporate;
using ApiLayer.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ApiLayer.Controllers.Corporate;

/// <summary>
/// Controlador para gestionar Countries
/// </summary>
[ApiController]
[Route("api/corporate/[controller]")]
[Authorize]
public class CountryController : ControllerBase
{
    private readonly CountryService _countryService;

    public CountryController(CountryService countryService)
    {
        _countryService = countryService;
    }

    #region GET - Get Countries (Flexible Search)

    /// <summary>
    /// Obtiene countries con búsqueda flexible usando parámetros opcionales
    /// </summary>
    /// <param name="id">ID del country (si se especifica, devuelve solo ese country)</param>
    /// <param name="countryCode">Código del país (ej: CRI, USA)</param>
    /// <param name="countryName">Nombre del país para búsqueda parcial</param>
    /// <param name="phoneCode">Código telefónico (ej: +506)</param>
    /// <param name="isActive">Filtrar por estado activo (por defecto true)</param>
    /// <returns>Lista de countries que coinciden con los criterios</returns>
    [HttpGet]
    public async Task<IActionResult> GetCountries(
        [FromQuery] int? id = null,
        [FromQuery] string? countryCode = null,
        [FromQuery] string? countryName = null,
        [FromQuery] string? phoneCode = null,
        [FromQuery] bool? isActive = true)
    {
        try
        {
            // Si se especifica ID, devolver solo ese country
            if (id.HasValue)
            {
                var country = await _countryService.GetCountryByIdAsync(id.Value);
                
                if (country == null)
                {
                    var response = ResponseStructure<object>.Error(
                        $"No se encontró el country con ID {id.Value}", 
                        404);
                    return NotFound(response);
                }

                var successResponse = ResponseStructure<ModelLayer.Corporate.Entities.Country>.Success(
                    country, 
                    "Country obtenido exitosamente");
                
                return Ok(successResponse);
            }

            // Si se especifica CountryCode
            if (!string.IsNullOrEmpty(countryCode))
            {
                var country = await _countryService.GetCountryByCodeAsync(countryCode);
                
                if (country == null)
                {
                    var response = ResponseStructure<object>.Error(
                        $"No se encontró el country con código {countryCode}", 
                        404);
                    return NotFound(response);
                }

                var successResponse = ResponseStructure<ModelLayer.Corporate.Entities.Country>.Success(
                    country, 
                    "Country obtenido exitosamente");
                
                return Ok(successResponse);
            }

            // Obtener countries con los filtros especificados
            var countries = await _countryService.GetCountriesAsync(
                id, countryCode, countryName, phoneCode, isActive);
            
            var allResponse = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.Country>>.Success(
                countries, 
                "Countries obtenidos exitosamente");
            
            return Ok(allResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener los Countries: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion
}

