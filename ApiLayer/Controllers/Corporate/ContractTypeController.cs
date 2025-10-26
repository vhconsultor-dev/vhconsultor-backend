using ApplicationLayer.Corporate;
using ApiLayer.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ApiLayer.Controllers.Corporate;

/// <summary>
/// Controlador para gestionar Contract Types
/// </summary>
[ApiController]
[Route("api/corporate/[controller]")]
[Authorize]
public class ContractTypeController : ControllerBase
{
    private readonly ContractTypeService _contractTypeService;

    public ContractTypeController(ContractTypeService contractTypeService)
    {
        _contractTypeService = contractTypeService;
    }

    #region GET - Get Contract Types (Flexible Search)

    /// <summary>
    /// Obtiene contract types con búsqueda flexible usando parámetros opcionales
    /// </summary>
    /// <param name="id">ID del contract type (si se especifica, devuelve solo ese contract type)</param>
    /// <param name="typeName">Nombre del tipo para búsqueda parcial</param>
    /// <param name="isActive">Filtrar por estado activo (por defecto true)</param>
    /// <returns>Lista de contract types que coinciden con los criterios</returns>
    [HttpGet]
    public async Task<IActionResult> GetContractTypes(
        [FromQuery] int? id = null,
        [FromQuery] string? typeName = null,
        [FromQuery] bool? isActive = true)
    {
        try
        {
            // Si se especifica ID, devolver solo ese contract type
            if (id.HasValue)
            {
                var contractType = await _contractTypeService.GetContractTypeByIdAsync(id.Value);
                
                if (contractType == null)
                {
                    var notFoundResponse = ResponseStructure<object>.Error(
                        "Contract type no encontrado", 
                        404);
                    return NotFound(notFoundResponse);
                }

                var singleResponse = ResponseStructure<ModelLayer.Corporate.Entities.ContractType>.Success(
                    contractType, 
                    "Contract type obtenido exitosamente");
                
                return Ok(singleResponse);
            }

            // Devolver todos los contract types con los filtros aplicados
            var contractTypes = await _contractTypeService.GetContractTypesAsync(id, typeName, isActive);
            
            var response = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.ContractType>>.Success(
                contractTypes, 
                "Contract types obtenidos exitosamente");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener los contract types: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion
}

