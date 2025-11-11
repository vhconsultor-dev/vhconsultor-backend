using ApiLayer.Tools;
using ApplicationLayer.Corporate;
using ApplicationLayer.Shared;
using BusinessLayer.Corporate.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiLayer.Controllers.Corporate;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PricingController : ControllerBase
{
    private readonly PricingService _pricingService;
    private readonly ValidationService _validationService;

    public PricingController(
        PricingService pricingService,
        ValidationService validationService)
    {
        _pricingService = pricingService;
        _validationService = validationService;
    }

    #region GET

    /// <summary>
    /// ENDPOINT 1: Obtener rangos de pricing por porcentaje
    /// </summary>
    [HttpGet("budget-ranges")]
    public async Task<IActionResult> GetBudgetRanges(
        [FromQuery] int? serviceId = null,
        [FromQuery] int? businessTypeId = null,
        [FromQuery] int? platformId = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var ranges = await _pricingService.GetServiceBudgetRangesAsync(
                serviceId, businessTypeId, platformId, isActive);

            var rangesList = ranges.ToList();
            var response = ResponseStructure<object>.Success(
                rangesList,
                $"{rangesList.Count} rango(s) de presupuesto encontrado(s)");

            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener rangos de presupuesto: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// ENDPOINT 2: Obtener un rango de pricing por porcentaje específico
    /// </summary>
    [HttpGet("budget-ranges/{id}")]
    public async Task<IActionResult> GetBudgetRangeById(int id)
    {
        try
        {
            var range = await _pricingService.GetServiceBudgetRangeByIdAsync(id);

            if (range == null)
            {
                var notFoundResponse = ResponseStructure<object>.NotFound(
                    $"Rango de presupuesto con ID {id} no encontrado");
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(
                range,
                "Rango de presupuesto obtenido exitosamente");

            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener rango de presupuesto: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// ENDPOINT 6: Obtener rangos de pricing por valor fijo
    /// </summary>
    [HttpGet("ad-budget-ranges")]
    public async Task<IActionResult> GetAdBudgetRanges(
        [FromQuery] int? serviceId = null,
        [FromQuery] int? businessTypeId = null,
        [FromQuery] int? platformId = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var ranges = await _pricingService.GetServiceAdBudgetRangesAsync(
                serviceId, businessTypeId, platformId, isActive);

            var rangesList = ranges.ToList();
            var response = ResponseStructure<object>.Success(
                rangesList,
                $"{rangesList.Count} rango(s) de presupuesto de publicidad encontrado(s)");

            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener rangos de presupuesto de publicidad: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// ENDPOINT 7: Obtener un rango de pricing por valor fijo específico
    /// </summary>
    [HttpGet("ad-budget-ranges/{id}")]
    public async Task<IActionResult> GetAdBudgetRangeById(int id)
    {
        try
        {
            var range = await _pricingService.GetServiceAdBudgetRangeByIdAsync(id);

            if (range == null)
            {
                var notFoundResponse = ResponseStructure<object>.NotFound(
                    $"Rango de presupuesto de publicidad con ID {id} no encontrado");
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(
                range,
                "Rango de presupuesto de publicidad obtenido exitosamente");

            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener rango de presupuesto de publicidad: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region POST

    /// <summary>
    /// ENDPOINT 3: Crear nuevo rango de pricing por porcentaje
    /// </summary>
    [HttpPost("budget-ranges")]
    public async Task<IActionResult> CreateBudgetRange([FromBody] CreateServiceBudgetRangeCommand command)
    {
        // Validación usando FluentValidation
        var validationResult = await _validationService.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var errorResponse = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(errorResponse);
        }

        try
        {
            var result = await _pricingService.CreateServiceBudgetRangeAsync(command);

            if (!result.Success)
            {
                var badRequestResponse = ResponseStructure<object>.BadRequest(result.Message);
                return BadRequest(badRequestResponse);
            }

            var response = ResponseStructure<object>.Success(
                new { rangeId = result.RangeId },
                result.Message);

            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al crear rango de presupuesto: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// ENDPOINT 8: Crear nuevo rango de pricing por valor fijo
    /// </summary>
    [HttpPost("ad-budget-ranges")]
    public async Task<IActionResult> CreateAdBudgetRange([FromBody] CreateServiceAdBudgetRangeCommand command)
    {
        // Validación usando FluentValidation
        var validationResult = await _validationService.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var errorResponse = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(errorResponse);
        }

        try
        {
            var result = await _pricingService.CreateServiceAdBudgetRangeAsync(command);

            if (!result.Success)
            {
                var badRequestResponse = ResponseStructure<object>.BadRequest(result.Message);
                return BadRequest(badRequestResponse);
            }

            var response = ResponseStructure<object>.Success(
                new { rangeId = result.RangeId },
                result.Message);

            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al crear rango de presupuesto de publicidad: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// ENDPOINT 11: Calcular comisión basada en porcentaje
    /// </summary>
    [HttpPost("calculate-percentage")]
    public async Task<IActionResult> CalculatePercentage([FromBody] CalculatePercentageCommand command)
    {
        // Validación usando FluentValidation
        var validationResult = await _validationService.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var errorResponse = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(errorResponse);
        }

        try
        {
            var result = await _pricingService.CalculatePercentageAsync(command);

            if (!result.Success)
            {
                var notFoundResponse = ResponseStructure<object>.NotFound(result.Message);
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(
                new
                {
                    rangeId = result.RangeId,
                    minBudgetValue = result.MinBudgetValue,
                    maxBudgetValue = result.MaxBudgetValue,
                    percentage = result.Percentage,
                    annualBudget = result.AnnualBudget,
                    calculatedCommission = result.CalculatedCommission
                },
                result.Message);

            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al calcular comisión: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// ENDPOINT CALCULADORA: Calcular pricing basado en businessType, platform, service y budget
    /// </summary>
    [HttpPost("calculate")]
    public async Task<IActionResult> CalculatePricing([FromBody] CalculatePricingCommand command)
    {
        // Validación usando FluentValidation
        var validationResult = await _validationService.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var errorResponse = new
            {
                success = false,
                error = new
                {
                    code = "VALIDATION_ERROR",
                    message = "Datos de entrada inválidos",
                    details = validationResult.Errors
                }
            };
            return BadRequest(errorResponse);
        }

        try
        {
            var result = await _pricingService.CalculatePricingAsync(command);

            if (!result.Success)
            {
                if (result.ErrorCode == "RANGE_NOT_FOUND")
                {
                    var notFoundResponse = new
                    {
                        success = false,
                        error = new
                        {
                            code = result.ErrorCode,
                            message = result.ErrorMessage,
                            details = result.ErrorDetails
                        }
                    };
                    return NotFound(notFoundResponse);
                }
                else
                {
                    var badRequestResponse = new
                    {
                        success = false,
                        error = new
                        {
                            code = result.ErrorCode,
                            message = result.ErrorMessage,
                            details = result.ErrorDetails
                        }
                    };
                    return BadRequest(badRequestResponse);
                }
            }

            var response = new
            {
                success = true,
                calculationType = result.CalculationType,
                businessType = result.BusinessType,
                platform = result.Platform,
                service = result.Service,
                inputBudget = result.InputBudget,
                range = result.Range,
                pricing = result.Pricing,
                result = result.Result,
                metadata = result.Metadata
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = new
            {
                success = false,
                error = new
                {
                    code = "INTERNAL_ERROR",
                    message = $"Error al calcular pricing: {ex.Message}"
                }
            };
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// ENDPOINT 12: Obtener valor fijo basado en presupuesto de publicidad
    /// </summary>
    [HttpPost("calculate-fixed")]
    public async Task<IActionResult> CalculateFixed([FromBody] CalculateFixedCommand command)
    {
        // Validación usando FluentValidation
        var validationResult = await _validationService.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var errorResponse = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(errorResponse);
        }

        try
        {
            var result = await _pricingService.CalculateFixedAsync(command);

            if (!result.Success)
            {
                var notFoundResponse = ResponseStructure<object>.NotFound(result.Message);
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(
                new
                {
                    rangeId = result.RangeId,
                    minAdBudgetValue = result.MinAdBudgetValue,
                    maxAdBudgetValue = result.MaxAdBudgetValue,
                    annualAdBudget = result.AnnualAdBudget,
                    fixedQuote = result.FixedQuote
                },
                result.Message);

            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener valor fijo: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region PUT

    /// <summary>
    /// ENDPOINT 4: Actualizar rango de pricing por porcentaje
    /// </summary>
    [HttpPut("budget-ranges/{id}")]
    public async Task<IActionResult> UpdateBudgetRange(int id, [FromBody] UpdateServiceBudgetRangeCommand command)
    {
        // Validación usando FluentValidation
        var validationResult = await _validationService.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var errorResponse = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(errorResponse);
        }

        try
        {
            var result = await _pricingService.UpdateServiceBudgetRangeAsync(id, command);

            if (!result.Success)
            {
                var badRequestResponse = ResponseStructure<object>.BadRequest(result.Message);
                return BadRequest(badRequestResponse);
            }

            var response = ResponseStructure<object>.Success(null, result.Message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al actualizar rango de presupuesto: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// ENDPOINT 9: Actualizar rango de pricing por valor fijo
    /// </summary>
    [HttpPut("ad-budget-ranges/{id}")]
    public async Task<IActionResult> UpdateAdBudgetRange(int id, [FromBody] UpdateServiceAdBudgetRangeCommand command)
    {
        // Validación usando FluentValidation
        var validationResult = await _validationService.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var errorResponse = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(errorResponse);
        }

        try
        {
            var result = await _pricingService.UpdateServiceAdBudgetRangeAsync(id, command);

            if (!result.Success)
            {
                var badRequestResponse = ResponseStructure<object>.BadRequest(result.Message);
                return BadRequest(badRequestResponse);
            }

            var response = ResponseStructure<object>.Success(null, result.Message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al actualizar rango de presupuesto de publicidad: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region DELETE

    /// <summary>
    /// ENDPOINT 5: Eliminar/Desactivar rango de pricing por porcentaje
    /// </summary>
    [HttpDelete("budget-ranges/{id}")]
    public async Task<IActionResult> DeleteBudgetRange(int id)
    {
        try
        {
            var result = await _pricingService.DeleteServiceBudgetRangeAsync(id);

            if (!result.Success)
            {
                var notFoundResponse = ResponseStructure<object>.NotFound(result.Message);
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(null, result.Message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al eliminar rango de presupuesto: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// ENDPOINT 10: Eliminar/Desactivar rango de pricing por valor fijo
    /// </summary>
    [HttpDelete("ad-budget-ranges/{id}")]
    public async Task<IActionResult> DeleteAdBudgetRange(int id)
    {
        try
        {
            var result = await _pricingService.DeleteServiceAdBudgetRangeAsync(id);

            if (!result.Success)
            {
                var notFoundResponse = ResponseStructure<object>.NotFound(result.Message);
                return NotFound(notFoundResponse);
            }

            var response = ResponseStructure<object>.Success(null, result.Message);
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al eliminar rango de presupuesto de publicidad: {ex.Message}");
            return StatusCode(500, errorResponse);
        }
    }

    #endregion
}

