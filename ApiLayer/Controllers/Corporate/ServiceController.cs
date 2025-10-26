using ApplicationLayer.Corporate;
using ApiLayer.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ApiLayer.Controllers.Corporate;

/// <summary>
/// Controlador para gestionar Services
/// </summary>
[ApiController]
[Route("api/corporate/[controller]")]
[Authorize]
public class ServiceController : ControllerBase
{
    private readonly ServiceService _serviceService;

    public ServiceController(ServiceService serviceService)
    {
        _serviceService = serviceService;
    }

    #region GET - Get Services (Flexible Search)

    /// <summary>
    /// Obtiene services con búsqueda flexible usando parámetros opcionales
    /// </summary>
    /// <param name="id">ID del service (si se especifica, devuelve solo ese service)</param>
    /// <param name="serviceCode">Código del servicio</param>
    /// <param name="serviceName">Nombre del servicio para búsqueda parcial</param>
    /// <param name="billingUnit">Unidad de facturación</param>
    /// <param name="isActive">Filtrar por estado activo (por defecto true)</param>
    /// <returns>Lista de services que coinciden con los criterios</returns>
    [HttpGet]
    public async Task<IActionResult> GetServices(
        [FromQuery] int? id = null,
        [FromQuery] string? serviceCode = null,
        [FromQuery] string? serviceName = null,
        [FromQuery] string? billingUnit = null,
        [FromQuery] bool? isActive = true)
    {
        try
        {
            // Si se especifica ID, devolver solo ese service
            if (id.HasValue)
            {
                var service = await _serviceService.GetServiceByIdAsync(id.Value);
                
                if (service == null)
                {
                    var notFoundResponse = ResponseStructure<object>.Error(
                        "Service no encontrado", 
                        404);
                    return NotFound(notFoundResponse);
                }

                var singleResponse = ResponseStructure<ModelLayer.Corporate.Entities.Service>.Success(
                    service, 
                    "Service obtenido exitosamente");
                
                return Ok(singleResponse);
            }

            // Si se especifica código de servicio, buscar por código
            if (!string.IsNullOrEmpty(serviceCode))
            {
                var service = await _serviceService.GetServiceByCodeAsync(serviceCode);
                
                if (service == null)
                {
                    var notFoundResponse = ResponseStructure<object>.Error(
                        "Service no encontrado", 
                        404);
                    return NotFound(notFoundResponse);
                }

                var singleResponse = ResponseStructure<ModelLayer.Corporate.Entities.Service>.Success(
                    service, 
                    "Service obtenido exitosamente");
                
                return Ok(singleResponse);
            }

            // Devolver todos los services con los filtros aplicados
            var services = await _serviceService.GetServicesAsync(id, serviceCode, serviceName, billingUnit, isActive);
            
            var response = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.Service>>.Success(
                services, 
                "Services obtenidos exitosamente");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener los services: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion
}

