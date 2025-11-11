using ApplicationLayer.Corporate;
using ApiLayer.Tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiLayer.Controllers.Corporate;

/// <summary>
/// Controlador para gestionar BusinessTypes
/// </summary>
[ApiController]
[Route("api/corporate/[controller]")]
[Authorize]
public class BusinessTypeController : ControllerBase
{
    private readonly BusinessTypeService _businessTypeService;

    public BusinessTypeController(BusinessTypeService businessTypeService)
    {
        _businessTypeService = businessTypeService;
    }

    #region GET

    /// <summary>
    /// Obtiene business types con búsqueda flexible usando parámetros opcionales
    /// </summary>
    /// <param name="businessTypeId">ID del business type (si se especifica, devuelve solo ese)</param>
    /// <param name="businessTypeName">Nombre del business type para búsqueda parcial</param>
    /// <param name="businessTypeKey">Clave del business type para búsqueda exacta</param>
    /// <param name="isActive">Filtrar por estado activo (por defecto null = todos)</param>
    /// <returns>Lista de business types que coinciden con los criterios</returns>
    [HttpGet]
    public async Task<IActionResult> GetBusinessTypes(
        [FromQuery] int? businessTypeId = null,
        [FromQuery] string? businessTypeName = null,
        [FromQuery] string? businessTypeKey = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var businessTypes = await _businessTypeService.GetBusinessTypesAsync(
                businessTypeId, businessTypeName, businessTypeKey, isActive);

            var businessTypesList = businessTypes.ToList();
            var response = ResponseStructure<object>.Success(
                businessTypesList,
                $"{businessTypesList.Count} business type(s) encontrado(s)");

            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener business types: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Obtiene un business type específico por ID
    /// </summary>
    /// <param name="id">ID del business type</param>
    /// <returns>Business type encontrado</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBusinessTypeById(int id)
    {
        try
        {
            var businessType = await _businessTypeService.GetBusinessTypeByIdAsync(id);

            if (businessType == null)
            {
                var notFoundResponse = ResponseStructure<object>.NotFound(
                    $"BusinessType con ID {id} no encontrado");
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(
                businessType,
                "BusinessType obtenido exitosamente");

            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener business type: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    #endregion
}

