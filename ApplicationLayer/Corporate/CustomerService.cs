using BusinessLayer.Corporate.Commands;
using BusinessLayer.Corporate.Queries;
using ModelLayer.Corporate.Entities;

namespace ApplicationLayer.Corporate;

/// <summary>
/// Servicio de aplicación para gestionar Customers
/// </summary>
public class CustomerService
{
    private readonly CreateCustomerCommand _createCustomerCommand;
    private readonly UpdateCustomerCommand _updateCustomerCommand;
    private readonly DeleteCustomerCommand _deleteCustomerCommand;
    private readonly UpdateLastContactDateCommand _updateLastContactDateCommand;
    private readonly CustomerQueryRepository _customerQueryRepository;

    public CustomerService(
        CreateCustomerCommand createCustomerCommand,
        UpdateCustomerCommand updateCustomerCommand,
        DeleteCustomerCommand deleteCustomerCommand,
        UpdateLastContactDateCommand updateLastContactDateCommand,
        CustomerQueryRepository customerQueryRepository)
    {
        _createCustomerCommand = createCustomerCommand;
        _updateCustomerCommand = updateCustomerCommand;
        _deleteCustomerCommand = deleteCustomerCommand;
        _updateLastContactDateCommand = updateLastContactDateCommand;
        _customerQueryRepository = customerQueryRepository;
    }

    #region Commands

    /// <summary>
    /// Crea un nuevo customer
    /// </summary>
    /// <param name="request">Datos del customer a crear</param>
    /// <returns>ID del customer creado</returns>
    public async Task<int> CreateCustomerAsync(CreateCustomerRequest request)
    {
        return await _createCustomerCommand.ExecuteAsync(request);
    }

    /// <summary>
    /// Actualiza un customer existente
    /// </summary>
    /// <param name="customerId">ID del customer a actualizar</param>
    /// <param name="request">Datos actualizados del customer</param>
    /// <returns>True si se actualizó correctamente, False si no se encontró</returns>
    public async Task<bool> UpdateCustomerAsync(int customerId, UpdateCustomerRequest request)
    {
        return await _updateCustomerCommand.ExecuteAsync(customerId, request);
    }

    /// <summary>
    /// Elimina (soft delete) un customer
    /// </summary>
    /// <param name="customerId">ID del customer a eliminar</param>
    /// <returns>True si se eliminó correctamente, False si no se encontró</returns>
    public async Task<bool> DeleteCustomerAsync(int customerId)
    {
        return await _deleteCustomerCommand.ExecuteAsync(customerId);
    }

    /// <summary>
    /// Actualiza la fecha de último contacto de un customer
    /// </summary>
    /// <param name="customerId">ID del customer</param>
    /// <returns>True si se actualizó correctamente, False si no se encontró</returns>
    public async Task<bool> UpdateLastContactDateAsync(int customerId)
    {
        return await _updateLastContactDateCommand.ExecuteAsync(customerId);
    }

    #endregion

    #region Queries

    /// <summary>
    /// Obtiene todos los customers activos
    /// </summary>
    /// <returns>Lista de customers</returns>
    public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
    {
        return await _customerQueryRepository.GetAllAsync();
    }

    /// <summary>
    /// Obtiene un customer por su ID
    /// </summary>
    /// <param name="customerId">ID del customer</param>
    /// <returns>Customer o null si no existe</returns>
    public async Task<Customer?> GetCustomerByIdAsync(int customerId)
    {
        return await _customerQueryRepository.GetByIdAsync(customerId);
    }

    /// <summary>
    /// Obtiene un customer por su NIT
    /// </summary>
    /// <param name="nit">NIT del customer</param>
    /// <returns>Customer o null si no existe</returns>
    public async Task<Customer?> GetCustomerByNITAsync(string nit)
    {
        return await _customerQueryRepository.GetByNITAsync(nit);
    }

    /// <summary>
    /// Busca customers por nombre de compañía
    /// </summary>
    /// <param name="companyName">Nombre o parte del nombre de la compañía</param>
    /// <returns>Lista de customers que coinciden</returns>
    public async Task<IEnumerable<Customer>> SearchByCompanyNameAsync(string companyName)
    {
        return await _customerQueryRepository.SearchByCompanyNameAsync(companyName);
    }

    /// <summary>
    /// Obtiene customers filtrados por múltiples criterios
    /// </summary>
    /// <param name="filter">Filtros de búsqueda</param>
    /// <returns>Lista de customers que coinciden con los filtros</returns>
    public async Task<IEnumerable<Customer>> GetFilteredCustomersAsync(CustomerFilter filter)
    {
        return await _customerQueryRepository.GetFilteredAsync(filter);
    }

    /// <summary>
    /// Obtiene customers por estado de cliente
    /// </summary>
    /// <param name="clientStatus">Estado del cliente</param>
    /// <returns>Lista de customers con ese estado</returns>
    public async Task<IEnumerable<Customer>> GetCustomersByClientStatusAsync(string clientStatus)
    {
        return await _customerQueryRepository.GetByClientStatusAsync(clientStatus);
    }

    /// <summary>
    /// Obtiene customers por prioridad
    /// </summary>
    /// <param name="priority">Prioridad</param>
    /// <returns>Lista de customers con esa prioridad</returns>
    public async Task<IEnumerable<Customer>> GetCustomersByPriorityAsync(string priority)
    {
        return await _customerQueryRepository.GetByPriorityAsync(priority);
    }

    #endregion
}

