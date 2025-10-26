namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Entidad que representa un método de pago
/// </summary>
public class PaymentMethod
{
    public int PaymentMethodId { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public string? MethodDescription { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
