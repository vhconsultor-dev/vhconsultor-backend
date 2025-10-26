using BusinessLayer.Corporate.Queries;
using ModelLayer.Corporate.Entities;

namespace ApplicationLayer.Corporate;

/// <summary>
/// Servicio de aplicación para gestionar Services
/// </summary>
public class ServiceService
{
    private readonly ServiceQueryRepository _serviceQueryRepository;

    public ServiceService(ServiceQueryRepository serviceQueryRepository)
    {
        _serviceQueryRepository = serviceQueryRepository;
    }

    #region Queries

    /// <summary>
    /// Obtiene services con filtros opcionales
    /// </summary>
    /// <param name="id">ID del service (opcional)</param>
    /// <param name="serviceCode">Código del servicio (opcional)</param>
    /// <param name="serviceName">Nombre del servicio para búsqueda parcial (opcional)</param>
    /// <param name="billingUnit">Unidad de facturación (opcional)</param>
    /// <param name="isActive">Filtrar por estado activo (opcional)</param>
    /// <returns>Lista de services</returns>
    public async Task<IEnumerable<Service>> GetServicesAsync(
        int? id = null,
        string? serviceCode = null,
        string? serviceName = null,
        string? billingUnit = null,
        bool? isActive = true)
    {
        return await _serviceQueryRepository.GetServicesAsync(id, serviceCode, serviceName, billingUnit, isActive);
    }

    /// <summary>
    /// Obtiene un service por su ID
    /// </summary>
    /// <param name="serviceId">ID del service</param>
    /// <returns>Service o null si no existe</returns>
    public async Task<Service?> GetServiceByIdAsync(int serviceId)
    {
        return await _serviceQueryRepository.GetByIdAsync(serviceId);
    }

    /// <summary>
    /// Obtiene un service por su código
    /// </summary>
    /// <param name="serviceCode">Código del service</param>
    /// <returns>Service o null si no existe</returns>
    public async Task<Service?> GetServiceByCodeAsync(string serviceCode)
    {
        return await _serviceQueryRepository.GetByCodeAsync(serviceCode);
    }

    /// <summary>
    /// Obtiene todos los services activos
    /// </summary>
    /// <returns>Lista de services activos</returns>
    public async Task<IEnumerable<Service>> GetAllActiveServicesAsync()
    {
        return await _serviceQueryRepository.GetAllActiveAsync();
    }

    #endregion
}

