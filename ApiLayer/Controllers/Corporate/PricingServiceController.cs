using ApplicationLayer.Corporate;
using ApiLayer.Tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiLayer.Controllers.Corporate;

/// <summary>
/// Controlador para gestionar PricingServices (servicios del módulo de pricing)
/// </summary>
[ApiController]
[Route("api/corporate/[controller]")]
[Authorize]
public class PricingServiceController : ControllerBase
{
    private readonly PricingServiceService _pricingServiceService;

    public PricingServiceController(PricingServiceService pricingServiceService)
    {
        _pricingServiceService = pricingServiceService;
    }

    #region GET

    /// <summary>
    /// Obtiene pricing services con búsqueda flexible usando parámetros opcionales
    /// </summary>
    /// <param name="serviceId">ID del service (si se especifica, devuelve solo ese)</param>
    /// <param name="serviceName">Nombre del servicio para búsqueda parcial</param>
    /// <param name="serviceKey">Clave del servicio para búsqueda exacta</param>
    /// <param name="serviceCategory">Categoría del servicio</param>
    /// <param name="isActive">Filtrar por estado activo (por defecto null = todos)</param>
    /// <returns>Lista de pricing services que coinciden con los criterios</returns>
    [HttpGet]
    public async Task<IActionResult> GetPricingServices(
        [FromQuery] int? serviceId = null,
        [FromQuery] string? serviceName = null,
        [FromQuery] string? serviceKey = null,
        [FromQuery] string? serviceCategory = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var services = await _pricingServiceService.GetPricingServicesAsync(
                serviceId, serviceName, serviceKey, serviceCategory, isActive);

            var servicesList = services.ToList();
            var response = ResponseStructure<object>.Success(
                servicesList,
                $"{servicesList.Count} pricing service(s) encontrado(s)");

            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener pricing services: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Obtiene un pricing service específico por ID
    /// </summary>
    /// <param name="id">ID del pricing service</param>
    /// <returns>Pricing service encontrado</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPricingServiceById(int id)
    {
        try
        {
            var service = await _pricingServiceService.GetPricingServiceByIdAsync(id);

            if (service == null)
            {
                var notFoundResponse = ResponseStructure<object>.NotFound(
                    $"PricingService con ID {id} no encontrado");
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(
                service,
                "PricingService obtenido exitosamente");

            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener pricing service: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    #endregion
}
