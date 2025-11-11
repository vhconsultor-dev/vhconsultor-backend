using ApplicationLayer.Corporate;
using ApiLayer.Tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiLayer.Controllers.Corporate;

/// <summary>
/// Controlador para gestionar Platforms
/// </summary>
[ApiController]
[Route("api/corporate/[controller]")]
[Authorize]
public class PlatformController : ControllerBase
{
    private readonly PlatformService _platformService;

    public PlatformController(PlatformService platformService)
    {
        _platformService = platformService;
    }

    #region GET

    /// <summary>
    /// Obtiene platforms con búsqueda flexible usando parámetros opcionales
    /// </summary>
    /// <param name="platformId">ID de la platform (si se especifica, devuelve solo esa)</param>
    /// <param name="businessTypeId">ID del business type para filtrar platforms</param>
    /// <param name="platformName">Nombre de la platform para búsqueda parcial</param>
    /// <param name="platformKey">Clave de la platform para búsqueda exacta</param>
    /// <param name="isActive">Filtrar por estado activo (por defecto null = todos)</param>
    /// <returns>Lista de platforms que coinciden con los criterios</returns>
    [HttpGet]
    public async Task<IActionResult> GetPlatforms(
        [FromQuery] int? platformId = null,
        [FromQuery] int? businessTypeId = null,
        [FromQuery] string? platformName = null,
        [FromQuery] string? platformKey = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var platforms = await _platformService.GetPlatformsAsync(
                platformId, businessTypeId, platformName, platformKey, isActive);

            var platformsList = platforms.ToList();
            var response = ResponseStructure<object>.Success(
                platformsList,
                $"{platformsList.Count} platform(s) encontrado(s)");

            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener platforms: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Obtiene una platform específica por ID
    /// </summary>
    /// <param name="id">ID de la platform</param>
    /// <returns>Platform encontrada</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPlatformById(int id)
    {
        try
        {
            var platform = await _platformService.GetPlatformByIdAsync(id);

            if (platform == null)
            {
                var notFoundResponse = ResponseStructure<object>.NotFound(
                    $"Platform con ID {id} no encontrada");
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(
                platform,
                "Platform obtenida exitosamente");

            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener platform: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    #endregion
}

