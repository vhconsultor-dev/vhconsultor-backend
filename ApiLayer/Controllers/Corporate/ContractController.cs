using ApplicationLayer.Corporate;
using ApplicationLayer.Shared;
using ApiLayer.Tools;
using BusinessLayer.Corporate.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ApiLayer.Controllers.Corporate;

/// <summary>
/// Controlador para gestionar Contracts (CRUD Completo)
/// </summary>
[ApiController]
[Route("api/corporate/[controller]")]
[Authorize]
public class ContractController : ControllerBase
{
    private readonly ContractService _contractService;
    private readonly ValidationService _validationService;

    public ContractController(
        ContractService contractService,
        ValidationService validationService)
    {
        _contractService = contractService;
        _validationService = validationService;
    }

    #region POST - Create Contract

    /// <summary>
    /// Crea un nuevo Contract
    /// </summary>
    /// <param name="request">Datos del contrato</param>
    /// <returns>ID del contrato creado</returns>
    [HttpPost]
    public async Task<IActionResult> CreateContract([FromBody] CreateContractRequest request)
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
            // ContractId es auto-generado (IDENTITY), no se valida existencia previa
            var contractId = await _contractService.CreateContractAsync(request);
            
            var successResponse = ResponseStructure<int>.Success(
                contractId, 
                "Contrato creado exitosamente");
            
            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al crear el contrato: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region PUT - Update Contract

    /// <summary>
    /// Actualiza un Contract existente
    /// </summary>
    /// <param name="contractId">ID del contrato a actualizar</param>
    /// <param name="request">Datos actualizados del contrato</param>
    /// <returns>Resultado de la operación</returns>
    [HttpPut("{contractId}")]
    public async Task<IActionResult> UpdateContract(int contractId, [FromBody] UpdateContractRequest request)
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
            // Verificar si el contrato existe
            var existingContract = await _contractService.GetContractByIdAsync(contractId);
            if (existingContract == null)
            {
                var response = ResponseStructure<object>.Error(
                    $"No se encontró el contrato con ID {contractId}", 
                    404);
                return NotFound(response);
            }

            var result = await _contractService.UpdateContractAsync(contractId, request);
            
            if (result)
            {
                var successResponse = ResponseStructure<bool>.Success(
                    true, 
                    "Contrato actualizado exitosamente");
                return Ok(successResponse);
            }
            else
            {
                var errorResponse = ResponseStructure<object>.Error(
                    "No se pudo actualizar el contrato", 
                    500);
                return StatusCode(500, errorResponse);
            }
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al actualizar el contrato: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region DELETE - Delete Contract

    /// <summary>
    /// Elimina (cancela) un Contract
    /// </summary>
    /// <param name="contractId">ID del contrato a eliminar</param>
    /// <param name="deletedBy">Usuario que elimina (opcional)</param>
    /// <returns>Resultado de la operación</returns>
    [HttpDelete("{contractId}")]
    public async Task<IActionResult> DeleteContract(int contractId, [FromQuery] string? deletedBy = null)
    {
        try
        {
            // Verificar si el contrato existe
            var existingContract = await _contractService.GetContractByIdAsync(contractId);
            if (existingContract == null)
            {
                var response = ResponseStructure<object>.Error(
                    $"Contract with ID {contractId} not found", 
                    404);
                return NotFound(response);
            }

            var result = await _contractService.DeleteContractAsync(contractId, deletedBy);
            
            if (result)
            {
                var successResponse = ResponseStructure<bool>.Success(
                    true, 
                    $"Contract '{existingContract.ContractNumber}' has been successfully cancelled");
                return Ok(successResponse);
            }
            else
            {
                var errorResponse = ResponseStructure<object>.Error(
                    "Unable to cancel the contract", 
                    500);
                return StatusCode(500, errorResponse);
            }
        }
        catch (InvalidOperationException ex)
        {
            // Error de validación de negocio (ej: tiene facturas asociadas)
            var errorResponse = ResponseStructure<object>.ValidationError(
                ex.Message);
            return BadRequest(errorResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"An unexpected error occurred while cancelling the contract: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region GET - Get Contracts (Flexible Search)

    /// <summary>
    /// Obtiene contracts con búsqueda flexible usando parámetros opcionales
    /// </summary>
    /// <param name="contractId">ID del contrato</param>
    /// <param name="customerId">ID del customer</param>
    /// <param name="contractNumber">Número de contrato para búsqueda parcial</param>
    /// <param name="status">Estado del contrato</param>
    /// <param name="contractTypeId">ID del tipo de contrato</param>
    /// <param name="currencyCode">Código de moneda</param>
    /// <param name="startDateFrom">Fecha de inicio desde</param>
    /// <param name="startDateTo">Fecha de inicio hasta</param>
    /// <param name="endDateFrom">Fecha de fin desde</param>
    /// <param name="endDateTo">Fecha de fin hasta</param>
    /// <returns>Lista de contracts que coinciden con los criterios</returns>
    [HttpGet]
    public async Task<IActionResult> GetContracts(
        [FromQuery] int? contractId = null,
        [FromQuery] int? customerId = null,
        [FromQuery] string? contractNumber = null,
        [FromQuery] string? status = null,
        [FromQuery] int? contractTypeId = null,
        [FromQuery] string? currencyCode = null,
        [FromQuery] DateTime? startDateFrom = null,
        [FromQuery] DateTime? startDateTo = null,
        [FromQuery] DateTime? endDateFrom = null,
        [FromQuery] DateTime? endDateTo = null)
    {
        try
        {
            // Si se especifica ID, devolver solo ese contrato
            if (contractId.HasValue)
            {
                var contract = await _contractService.GetContractByIdAsync(contractId.Value);
                
                if (contract == null)
                {
                    var notFoundResponse = ResponseStructure<object>.Error(
                        "Contrato no encontrado", 
                        404);
                    return NotFound(notFoundResponse);
                }

                var singleResponse = ResponseStructure<ModelLayer.Corporate.Entities.Contract>.Success(
                    contract, 
                    "Contrato obtenido exitosamente");
                
                return Ok(singleResponse);
            }

            // Si se especifica customerId, obtener contratos del customer
            if (customerId.HasValue)
            {
                var customerContracts = await _contractService.GetContractsByCustomerIdAsync(customerId.Value);
                
                var customerResponse = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.Contract>>.Success(
                    customerContracts, 
                    "Contratos del customer obtenidos exitosamente");
                
                return Ok(customerResponse);
            }

            // Devolver todos los contratos con los filtros aplicados
            var contracts = await _contractService.GetContractsAsync(
                contractId, customerId, contractNumber, status, contractTypeId,
                currencyCode, startDateFrom, startDateTo, endDateFrom, endDateTo);
            
            var response = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.Contract>>.Success(
                contracts, 
                "Contratos obtenidos exitosamente");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener los contratos: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region GET - Get Active Contracts

    /// <summary>
    /// Obtiene todos los contratos activos
    /// </summary>
    /// <returns>Lista de contratos activos</returns>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveContracts()
    {
        try
        {
            var contracts = await _contractService.GetActiveContractsAsync();
            
            var response = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.Contract>>.Success(
                contracts, 
                "Contratos activos obtenidos exitosamente");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener los contratos activos: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion
}

