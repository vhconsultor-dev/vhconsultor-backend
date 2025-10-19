using ApplicationLayer.Corporate;
using ApplicationLayer.Shared;
using ApiLayer.Tools;
using BusinessLayer.Corporate.Commands;
using BusinessLayer.Corporate.Queries;
using Microsoft.AspNetCore.Mvc;

namespace ApiLayer.Controllers.Corporate;

/// <summary>
/// Controlador para gestionar Customers
/// </summary>
[ApiController]
[Route("api/corporate/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly CustomerService _customerService;
    private readonly ValidationService _validationService;

    public CustomerController(
        CustomerService customerService,
        ValidationService validationService)
    {
        _customerService = customerService;
        _validationService = validationService;
    }

    #region POST - Create Customer

    /// <summary>
    /// Crea un nuevo Customer
    /// </summary>
    /// <param name="request">Datos del customer</param>
    /// <returns>ID del customer creado</returns>
    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request)
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
            // Verificar si ya existe un customer con el mismo NIT
            var existingCustomer = await _customerService.GetCustomerByNITAsync(request.NIT);
            if (existingCustomer != null)
            {
                var response = ResponseStructure<object>.ValidationError(
                    $"Ya existe un customer con el NIT {request.NIT}");
                return BadRequest(response);
            }

            var customerId = await _customerService.CreateCustomerAsync(request);
            
            var successResponse = ResponseStructure<int>.Success(
                customerId, 
                "Customer creado exitosamente");
            
            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al crear el Customer: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region PUT - Update Customer

    /// <summary>
    /// Actualiza un Customer existente
    /// </summary>
    /// <param name="customerId">ID del customer a actualizar</param>
    /// <param name="request">Datos actualizados del customer</param>
    /// <returns>Resultado de la operación</returns>
    [HttpPut("{customerId}")]
    public async Task<IActionResult> UpdateCustomer(int customerId, [FromBody] UpdateCustomerRequest request)
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
            // Verificar si el customer existe
            var existingCustomer = await _customerService.GetCustomerByIdAsync(customerId);
            if (existingCustomer == null)
            {
                var response = ResponseStructure<object>.Error(
                    $"No se encontró el customer con ID {customerId}", 
                    404);
                return NotFound(response);
            }

            // Verificar si el NIT ya está siendo usado por otro customer
            var customerWithSameNIT = await _customerService.GetCustomerByNITAsync(request.NIT);
            if (customerWithSameNIT != null && customerWithSameNIT.CustomerId != customerId)
            {
                var response = ResponseStructure<object>.ValidationError(
                    $"El NIT {request.NIT} ya está siendo usado por otro customer");
                return BadRequest(response);
            }

            var result = await _customerService.UpdateCustomerAsync(customerId, request);
            
            if (result)
            {
                var successResponse = ResponseStructure<bool>.Success(
                    true, 
                    "Customer actualizado exitosamente");
                return Ok(successResponse);
            }
            else
            {
                var errorResponse = ResponseStructure<object>.Error(
                    "No se pudo actualizar el customer", 
                    500);
                return StatusCode(500, errorResponse);
            }
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al actualizar el Customer: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region DELETE - Delete Customer

    /// <summary>
    /// Elimina (soft delete) un Customer
    /// </summary>
    /// <param name="customerId">ID del customer a eliminar</param>
    /// <returns>Resultado de la operación</returns>
    [HttpDelete("{customerId}")]
    public async Task<IActionResult> DeleteCustomer(int customerId)
    {
        try
        {
            // Verificar si el customer existe
            var existingCustomer = await _customerService.GetCustomerByIdAsync(customerId);
            if (existingCustomer == null)
            {
                var response = ResponseStructure<object>.Error(
                    $"No se encontró el customer con ID {customerId}", 
                    404);
                return NotFound(response);
            }

            var result = await _customerService.DeleteCustomerAsync(customerId);
            
            if (result)
            {
                var successResponse = ResponseStructure<bool>.Success(
                    true, 
                    "Customer eliminado exitosamente");
                return Ok(successResponse);
            }
            else
            {
                var errorResponse = ResponseStructure<object>.Error(
                    "No se pudo eliminar el customer", 
                    500);
                return StatusCode(500, errorResponse);
            }
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al eliminar el Customer: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region GET - Get All Customers

    /// <summary>
    /// Obtiene todos los customers activos
    /// </summary>
    /// <returns>Lista de customers</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllCustomers()
    {
        try
        {
            var customers = await _customerService.GetAllCustomersAsync();
            
            var response = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.Customer>>.Success(
                customers, 
                "Customers obtenidos exitosamente");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener los Customers: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region GET - Get Customer By ID

    /// <summary>
    /// Obtiene un customer por su ID
    /// </summary>
    /// <param name="customerId">ID del customer</param>
    /// <returns>Customer encontrado</returns>
    [HttpGet("{customerId}")]
    public async Task<IActionResult> GetCustomerById(int customerId)
    {
        try
        {
            var customer = await _customerService.GetCustomerByIdAsync(customerId);
            
            if (customer == null)
            {
                var response = ResponseStructure<object>.Error(
                    $"No se encontró el customer con ID {customerId}", 
                    404);
                return NotFound(response);
            }

            var successResponse = ResponseStructure<ModelLayer.Corporate.Entities.Customer>.Success(
                customer, 
                "Customer obtenido exitosamente");
            
            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener el Customer: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region GET - Get Customer By NIT

    /// <summary>
    /// Obtiene un customer por su NIT
    /// </summary>
    /// <param name="nit">NIT del customer</param>
    /// <returns>Customer encontrado</returns>
    [HttpGet("by-nit/{nit}")]
    public async Task<IActionResult> GetCustomerByNIT(string nit)
    {
        try
        {
            var customer = await _customerService.GetCustomerByNITAsync(nit);
            
            if (customer == null)
            {
                var response = ResponseStructure<object>.Error(
                    $"No se encontró el customer con NIT {nit}", 
                    404);
                return NotFound(response);
            }

            var successResponse = ResponseStructure<ModelLayer.Corporate.Entities.Customer>.Success(
                customer, 
                "Customer obtenido exitosamente");
            
            return Ok(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener el Customer: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region GET - Search Customers By Company Name

    /// <summary>
    /// Busca customers por nombre de compañía
    /// </summary>
    /// <param name="companyName">Nombre o parte del nombre de la compañía</param>
    /// <returns>Lista de customers que coinciden</returns>
    [HttpGet("search/{companyName}")]
    public async Task<IActionResult> SearchByCompanyName(string companyName)
    {
        try
        {
            var customers = await _customerService.SearchByCompanyNameAsync(companyName);
            
            var response = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.Customer>>.Success(
                customers, 
                "Búsqueda completada exitosamente");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al buscar Customers: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region POST - Get Filtered Customers

    /// <summary>
    /// Obtiene customers filtrados por múltiples criterios
    /// </summary>
    /// <param name="filter">Filtros de búsqueda</param>
    /// <returns>Lista de customers que coinciden con los filtros</returns>
    [HttpPost("filter")]
    public async Task<IActionResult> GetFilteredCustomers([FromBody] CustomerFilter filter)
    {
        try
        {
            var customers = await _customerService.GetFilteredCustomersAsync(filter);
            
            var response = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.Customer>>.Success(
                customers, 
                "Customers filtrados obtenidos exitosamente");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al filtrar Customers: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region GET - Get Customers By Client Status

    /// <summary>
    /// Obtiene customers por estado de cliente
    /// </summary>
    /// <param name="clientStatus">Estado del cliente</param>
    /// <returns>Lista de customers con ese estado</returns>
    [HttpGet("by-status/{clientStatus}")]
    public async Task<IActionResult> GetCustomersByClientStatus(string clientStatus)
    {
        try
        {
            var customers = await _customerService.GetCustomersByClientStatusAsync(clientStatus);
            
            var response = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.Customer>>.Success(
                customers, 
                "Customers obtenidos exitosamente");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener Customers: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region GET - Get Customers By Priority

    /// <summary>
    /// Obtiene customers por prioridad
    /// </summary>
    /// <param name="priority">Prioridad</param>
    /// <returns>Lista de customers con esa prioridad</returns>
    [HttpGet("by-priority/{priority}")]
    public async Task<IActionResult> GetCustomersByPriority(string priority)
    {
        try
        {
            var customers = await _customerService.GetCustomersByPriorityAsync(priority);
            
            var response = ResponseStructure<IEnumerable<ModelLayer.Corporate.Entities.Customer>>.Success(
                customers, 
                "Customers obtenidos exitosamente");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al obtener Customers: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion

    #region PATCH - Update Last Contact Date

    /// <summary>
    /// Actualiza la fecha de último contacto de un customer
    /// </summary>
    /// <param name="customerId">ID del customer</param>
    /// <returns>Resultado de la operación</returns>
    [HttpPatch("{customerId}/last-contact")]
    public async Task<IActionResult> UpdateLastContactDate(int customerId)
    {
        try
        {
            // Verificar si el customer existe
            var existingCustomer = await _customerService.GetCustomerByIdAsync(customerId);
            if (existingCustomer == null)
            {
                var response = ResponseStructure<object>.Error(
                    $"No se encontró el customer con ID {customerId}", 
                    404);
                return NotFound(response);
            }

            var result = await _customerService.UpdateLastContactDateAsync(customerId);
            
            if (result)
            {
                var successResponse = ResponseStructure<bool>.Success(
                    true, 
                    "Fecha de último contacto actualizada exitosamente");
                return Ok(successResponse);
            }
            else
            {
                var errorResponse = ResponseStructure<object>.Error(
                    "No se pudo actualizar la fecha de último contacto", 
                    500);
                return StatusCode(500, errorResponse);
            }
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseStructure<object>.Error(
                $"Error al actualizar la fecha de último contacto: {ex.Message}", 
                500);
            return StatusCode(500, errorResponse);
        }
    }

    #endregion
}

