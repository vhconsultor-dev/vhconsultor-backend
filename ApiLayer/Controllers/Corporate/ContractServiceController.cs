using ApplicationLayer.Corporate;
using ApplicationLayer.Shared;
using ApiLayer.Tools;
using BusinessLayer.Corporate.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ApiLayer.Controllers.Corporate;

/// <summary>
/// Controlador para gestionar Contract Services (CRUD Completo)
/// </summary>
[ApiController]
[Route("api/corporate/[controller]")]
[Authorize]
public class ContractServiceController : ControllerBase
{
    private readonly ContractServiceService _contractServiceService;
    private readonly ValidationService _validationService;

    public ContractServiceController(
        ContractServiceService contractServiceService,
        ValidationService validationService)
    {
        _contractServiceService = contractServiceService;
        _validationService = validationService;
    }

    #region POST - Create ContractService

    /// <summary>
    /// Crea un nuevo Contract Service
    /// </summary>
    /// <param name="request">Datos del servicio del contrato</param>
    /// <returns>ID del servicio del contrato creado</returns>
    [HttpPost]
    public async Task<IActionResult> CreateContractService([FromBody] CreateContractServiceRequest request)
    {
        // Validación usando FluentValidation
        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var response = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(response);
        }

        try
        {
            var contractServiceId = await _contractServiceService.CreateContractServiceAsync(request);
            
            var successResponse = ResponseStructure<int>.Success(
                contractServiceId, 
                "Servicio del contrato creado exitosamente");
            
            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al crear el servicio del contrato: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region PUT - Update ContractService

    /// <summary>
    /// Actualiza un Contract Service existente
    /// </summary>
    /// <param name="contractServiceId">ID del servicio del contrato a actualizar</param>
    /// <param name="request">Datos actualizados del servicio del contrato</param>
    /// <returns>Resultado de la operación</returns>
    [HttpPut("{contractServiceId}")]
    public async Task<IActionResult> UpdateContractService(int contractServiceId, [FromBody] UpdateContractServiceRequest request)
    {
        // Validación usando FluentValidation
        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var response = ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors));
            return BadRequest(response);
        }

        try
        {
            // Verificar si el servicio del contrato existe
            var existingContractService = await _contractServiceService.GetContractServiceByIdAsync(contractServiceId);
            if (existingContractService == null)
            {
                var response = ResponseStructure<object>.Error(
                    $"No se encontró el servicio del contrato con ID {contractServiceId}", 
                    404);
                return NotFound(response);
            }

            var result = await _contractServiceService.UpdateContractServiceAsync(contractServiceId, request);
            
            if (result)
            {
                var successResponse = ResponseStructure<bool>.Success(
                    true, 
                    "Servicio del contrato actualizado exitosamente");
                return Ok(successResponse);
            }
            else
            {
                var errorResponse = ResponseStructure<object>.Error(
                    "No se pudo actualizar el servicio del contrato", 
                    500);
                return StatusCode(500, errorResponse);
            }
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al actualizar el servicio del contrato: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region DELETE - Delete ContractService

    /// <summary>
    /// Elimina (desactiva) un Contract Service
    /// </summary>
    /// <param name="contractServiceId">ID del servicio del contrato a eliminar</param>
    /// <returns>Resultado de la operación</returns>
    [HttpDelete("{contractServiceId}")]
    public async Task<IActionResult> DeleteContractService(int contractServiceId)
    {
        try
        {
            // Verificar si el servicio del contrato existe
            var existingContractService = await _contractServiceService.GetContractServiceByIdAsync(contractServiceId);
            if (existingContractService == null)
            {
                var response = ResponseStructure<object>.Error(
                    $"No se encontró el servicio del contrato con ID {contractServiceId}", 
                    404);
                return NotFound(response);
            }

            var result = await _contractServiceService.DeleteContractServiceAsync(contractServiceId);
            
            if (result)
            {
                var successResponse = ResponseStructure<bool>.Success(
                    true, 
                    "Servicio del contrato desactivado exitosamente");
                return Ok(successResponse);
            }
            else
            {
                var errorResponse = ResponseStructure<object>.Error(
                    "No se pudo desactivar el servicio del contrato", 
                    500);
                return StatusCode(500, errorResponse);
            }
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al desactivar el servicio del contrato: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region GET - Get ContractServices (Flexible Search)

    /// <summary>
    /// Obtiene contract services con búsqueda flexible usando parámetros opcionales
    /// </summary>
    /// <param name="contractServiceId">ID del servicio del contrato</param>
    /// <param name="contractId">ID del contrato</param>
    /// <param name="serviceId">ID del servicio</param>
    /// <param name="isActive">Filtrar por estado activo (por defecto true)</param>
    /// <returns>Lista de contract services que coinciden con los criterios</returns>
    [HttpGet]
    public async Task<IActionResult> GetContractServices(
        [FromQuery] int? contractServiceId = null,
        [FromQuery] string? contractId = null,
        [FromQuery] int? serviceId = null,
        [FromQuery] bool? isActive = true)
    {
        try
        {
            // Si se especifica contractServiceId, devolver solo ese servicio
            if (contractServiceId.HasValue)
            {
                var contractService = await _contractServiceService.GetContractServiceByIdAsync(contractServiceId.Value);
                
                if (contractService == null)
                {
                    var notFoundResponse = ResponseStructure<object>.Error(
                        "Servicio del contrato no encontrado", 
                        404);
                    return NotFound(notFoundResponse);
                }

                var singleResponse = ResponseStructure<ModelLayer.Corporate.Entities.ContractService>.Success(
                    contractService, 
                    "Servicio del contrato obtenido exitosamente");
                
                return Ok(singleResponse);
            }

            // Si se especifica contractId, obtener servicios del contrato
            if (!string.IsNullOrEmpty(contractId))
            {
                var contractServices = await _contractServiceService.GetContractServicesByContractIdAsync(contractId, isActive);
                
                var contractResponse = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.ContractService>>.Success(
                    contractServices, 
                    "Servicios del contrato obtenidos exitosamente");
                
                return Ok(contractResponse);
            }

            // Devolver todos los contract services con los filtros aplicados
            var services = await _contractServiceService.GetContractServicesAsync(
                contractServiceId, contractId, serviceId, isActive);
            
            var response = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.ContractService>>.Success(
                services, 
                "Servicios de contratos obtenidos exitosamente");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener los servicios de contratos: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region GET - Get Contract Total

    /// <summary>
    /// Obtiene el total de servicios de un contrato
    /// </summary>
    /// <param name="contractId">ID del contrato</param>
    /// <returns>Total de servicios del contrato</returns>
    [HttpGet("total/{contractId}")]
    public async Task<IActionResult> GetContractTotal(string contractId)
    {
        try
        {
            var total = await _contractServiceService.GetContractTotalAsync(contractId);
            
            var response = ResponseStructure<decimal>.Success(
                total, 
                "Total del contrato calculado exitosamente");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al calcular el total del contrato: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion
}

