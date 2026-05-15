namespace BusinessLayer.Corporate;

/// <summary>
/// Valores de frecuencia de pago usados por el frontend en contratos.
/// </summary>
public static class PaymentFrequencyHelper
{
    public const string Monthly = "Monthly";
    public const string Quarterly = "Quarterly";
    public const string Annual = "Annual";
    public const string PerProject = "Per Project";

    public static string AllowedValuesDescription =>
        $"{Monthly}, {Quarterly}, {Annual}, {PerProject}";

    public static bool IsAllowed(string? value) =>
        value is Monthly or Quarterly or Annual or PerProject;

    /// <summary>
    /// Meses entre facturas según la frecuencia (solo aplica si hay más de una factura).
    /// </summary>
    public static int GetMonthsIncrement(string paymentFrequency) =>
        paymentFrequency switch
        {
            Monthly => 1,
            Quarterly => 3,
            Annual => 12,
            PerProject => 1,
            "SemiAnnual" => 6, // legacy en BD
            _ => throw new ArgumentException($"Frecuencia de pago inválida: {paymentFrequency}")
        };

    /// <summary>
    /// Cantidad de facturas esperadas para la duración del contrato.
    /// </summary>
    public static int CalculateInvoiceCount(int contractMonths, string paymentFrequency)
    {
        if (paymentFrequency == PerProject)
            return 1;

        int monthsInFrequency = GetMonthsIncrement(paymentFrequency);

        if (monthsInFrequency > contractMonths)
            return 1;

        int count = paymentFrequency switch
        {
            Monthly => contractMonths,
            Quarterly => (int)Math.Ceiling(contractMonths / 3.0),
            Annual => (int)Math.Ceiling(contractMonths / 12.0),
            "SemiAnnual" => (int)Math.Ceiling(contractMonths / 6.0),
            _ => throw new ArgumentException($"Frecuencia de pago inválida: {paymentFrequency}")
        };

        return Math.Max(1, count);
    }
}
