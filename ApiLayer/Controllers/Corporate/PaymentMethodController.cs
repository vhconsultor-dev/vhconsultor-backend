using ApplicationLayer.Corporate;
using ApiLayer.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ApiLayer.Controllers.Corporate;

/// <summary>
/// Controlador para gestionar Payment Methods
/// </summary>
[ApiController]
[Route("api/corporate/[controller]")]
[Authorize]
public class PaymentMethodController : ControllerBase
{
    private readonly PaymentMethodService _paymentMethodService;

    public PaymentMethodController(PaymentMethodService paymentMethodService)
    {
        _paymentMethodService = paymentMethodService;
    }

    #region GET - Get Payment Methods (Flexible Search)

    /// <summary>
    /// Obtiene payment methods con búsqueda flexible usando parámetros opcionales
    /// </summary>
    /// <param name="id">ID del payment method (si se especifica, devuelve solo ese payment method)</param>
    /// <param name="methodName">Nombre del método para búsqueda parcial</param>
    /// <param name="isActive">Filtrar por estado activo (por defecto true)</param>
    /// <returns>Lista de payment methods que coinciden con los criterios</returns>
    [HttpGet]
    public async Task<IActionResult> GetPaymentMethods(
        [FromQuery] int? id = null,
        [FromQuery] string? methodName = null,
        [FromQuery] bool? isActive = true)
    {
        try
        {
            // Si se especifica ID, devolver solo ese payment method
            if (id.HasValue)
            {
                var paymentMethod = await _paymentMethodService.GetPaymentMethodByIdAsync(id.Value);
                
                if (paymentMethod == null)
                {
                    var notFoundResponse = ResponseStructure<object>.Error(
                        "Payment method no encontrado", 
                        404);
                    return NotFound(notFoundResponse);
                }

                var singleResponse = ResponseStructure<ModelLayer.Corporate.Entities.PaymentMethod>.Success(
                    paymentMethod, 
                    "Payment method obtenido exitosamente");
                
                return Ok(singleResponse);
            }

            // Si no hay filtros específicos, devolver todos los payment methods activos
            var paymentMethods = await _paymentMethodService.GetPaymentMethodsAsync(id, methodName, isActive);
            
            var response = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.PaymentMethod>>.Success(
                paymentMethods, 
                "Payment methods obtenidos exitosamente");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener los payment methods: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion
}
