using ApplicationLayer.Corporate;
using ApiLayer.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ApiLayer.Controllers.Corporate;

/// <summary>
/// Controlador para gestionar Industry Sectors
/// </summary>
[ApiController]
[Route("api/corporate/[controller]")]
[Authorize]
public class IndustrySectorController : ControllerBase
{
    private readonly IndustrySectorService _industrySectorService;

    public IndustrySectorController(IndustrySectorService industrySectorService)
    {
        _industrySectorService = industrySectorService;
    }

    #region GET - Get Industry Sectors (Flexible Search)

    /// <summary>
    /// Obtiene industry sectors con búsqueda flexible usando parámetros opcionales
    /// </summary>
    /// <param name="id">ID del sector (si se especifica, devuelve solo ese sector)</param>
    /// <param name="sectorName">Nombre del sector para búsqueda parcial</param>
    /// <param name="isActive">Filtrar por estado activo (por defecto true)</param>
    /// <returns>Lista de industry sectors que coinciden con los criterios</returns>
    [HttpGet]
    public async Task<IActionResult> GetIndustrySectors(
        [FromQuery] int? id = null,
        [FromQuery] string? sectorName = null,
        [FromQuery] bool? isActive = true)
    {
        try
        {
            // Si se especifica ID, devolver solo ese sector
            if (id.HasValue)
            {
                var sector = await _industrySectorService.GetIndustrySectorByIdAsync(id.Value);
                
                if (sector == null)
                {
                    var response = ResponseStructure<object>.Error(
                        $"No se encontró el sector con ID {id.Value}", 
                        404);
                    return NotFound(response);
                }

                var successResponse = ResponseStructure<ModelLayer.Corporate.Entities.IndustrySector>.Success(
                    sector, 
                    "Sector obtenido exitosamente");
                
                return Ok(successResponse);
            }

            // Obtener sectors con los filtros especificados
            var sectors = await _industrySectorService.GetIndustrySectorsAsync(
                id, sectorName, isActive);
            
            var allResponse = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.IndustrySector>>.Success(
                sectors, 
                "Sectores obtenidos exitosamente");
            
            return Ok(allResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener los sectores: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion
}

