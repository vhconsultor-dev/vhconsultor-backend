using ApplicationLayer.Corporate;
using ApiLayer.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ApiLayer.Controllers.Corporate;

/// <summary>
/// Controlador para gestionar Fee Types
/// </summary>
[ApiController]
[Route("api/corporate/[controller]")]
[Authorize]
public class FeeTypeController : ControllerBase
{
    private readonly FeeTypeService _feeTypeService;

    public FeeTypeController(FeeTypeService feeTypeService)
    {
        _feeTypeService = feeTypeService;
    }

    #region GET - Get Fee Types

    /// <summary>
    /// Obtiene fee types. Si se proporciona un ID, retorna un solo registro. Si no, retorna todos.
    /// </summary>
    /// <param name="id">ID del fee type (opcional)</param>
    /// <returns>Un fee type si se proporciona ID, o lista de todos los fee types si no se proporciona ID</returns>
    [HttpGet]
    public async Task<IActionResult> GetFeeTypes([FromQuery] int? id = null)
    {
        try
        {
            // Si se especifica ID, devolver solo ese fee type
            if (id.HasValue)
            {
                var feeType = await _feeTypeService.GetFeeTypeByIdAsync(id.Value);
                
                if (feeType == null)
                {
                    var notFoundResponse = ResponseStructure<object>.Error(
                        "Fee type no encontrado", 
                        404);
                    return NotFound(notFoundResponse);
                }

                var singleResponse = ResponseStructure<ModelLayer.Corporate.Entities.FeeType>.Success(
                    feeType, 
                    "Fee type obtenido exitosamente");
                
                return Ok(singleResponse);
            }

            // Devolver todos los fee types (sin filtros)
            var feeTypes = await _feeTypeService.GetFeeTypesAsync(null, null, null);
            
            var response = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.FeeType>>.Success(
                feeTypes, 
                "Fee types obtenidos exitosamente");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener los fee types: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion
}

