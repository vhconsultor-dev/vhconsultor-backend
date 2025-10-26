using BusinessLayer.Corporate.Queries;
using ModelLayer.Corporate.Entities;

namespace ApplicationLayer.Corporate;

/// <summary>
/// Servicio de aplicación para gestionar Payment Methods
/// </summary>
public class PaymentMethodService
{
    private readonly PaymentMethodQueryRepository _paymentMethodQueryRepository;

    public PaymentMethodService(PaymentMethodQueryRepository paymentMethodQueryRepository)
    {
        _paymentMethodQueryRepository = paymentMethodQueryRepository;
    }

    #region Queries

    /// <summary>
    /// Obtiene payment methods con filtros opcionales
    /// </summary>
    /// <param name="id">ID del payment method (opcional)</param>
    /// <param name="methodName">Nombre del método para búsqueda parcial (opcional)</param>
    /// <param name="isActive">Filtrar por estado activo (opcional)</param>
    /// <returns>Lista de payment methods</returns>
    public async Task<IEnumerable<PaymentMethod>> GetPaymentMethodsAsync(
        int? id = null,
        string? methodName = null,
        bool? isActive = true)
    {
        return await _paymentMethodQueryRepository.GetPaymentMethodsAsync(id, methodName, isActive);
    }

    /// <summary>
    /// Obtiene un payment method por su ID
    /// </summary>
    /// <param name="paymentMethodId">ID del payment method</param>
    /// <returns>PaymentMethod o null si no existe</returns>
    public async Task<PaymentMethod?> GetPaymentMethodByIdAsync(int paymentMethodId)
    {
        return await _paymentMethodQueryRepository.GetByIdAsync(paymentMethodId);
    }

    /// <summary>
    /// Obtiene todos los payment methods activos
    /// </summary>
    /// <returns>Lista de payment methods activos</returns>
    public async Task<IEnumerable<PaymentMethod>> GetAllActivePaymentMethodsAsync()
    {
        return await _paymentMethodQueryRepository.GetAllActiveAsync();
    }

    #endregion
}
