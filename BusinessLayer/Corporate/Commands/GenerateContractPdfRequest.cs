namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Request para generar el PDF de un contrato
/// </summary>
public class GenerateContractPdfRequest
{
    /// <summary>
    /// ID del template de CraftMyPDF a utilizar
    /// </summary>
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>
    /// Datos para llenar el template del contrato
    /// </summary>
    public ContractPdfData Data { get; set; } = new();
}

/// <summary>
/// Datos necesarios para generar el PDF del contrato
/// </summary>
public class ContractPdfData
{
    /// <summary>
    /// Nombre de la compañía
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Nombre completo del cliente
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Tax ID del cliente
    /// </summary>
    public string TaxId { get; set; } = string.Empty;

    /// <summary>
    /// Nacionalidad del cliente
    /// </summary>
    public string Nationality { get; set; } = string.Empty;

    /// <summary>
    /// Dirección del cliente
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Regiones de cobertura del contrato
    /// </summary>
    public string Regions { get; set; } = string.Empty;

    /// <summary>
    /// Frecuencia de pago (monthly, quarterly, etc.)
    /// </summary>
    public string PaymentFrequency { get; set; } = string.Empty;

    /// <summary>
    /// Fee o tarifa del contrato
    /// </summary>
    public string Fee { get; set; } = string.Empty;

    /// <summary>
    /// Día del mes para el pago de la tarifa
    /// </summary>
    public int FeePaymentDay { get; set; }

    /// <summary>
    /// Duración del contrato (ej: "1 year", "2 years")
    /// </summary>
    public string ContractDurations { get; set; } = string.Empty;

    /// <summary>
    /// Período de aviso para cancelación (ej: "2 months")
    /// </summary>
    public string NoticePeriod { get; set; } = string.Empty;

    /// <summary>
    /// Fecha desde cuando está trabajando (formato: "MM/DD/YYYY")
    /// </summary>
    public string WorkingSince { get; set; } = string.Empty;

    /// <summary>
    /// Día de la firma del contrato
    /// </summary>
    public int Day { get; set; }

    /// <summary>
    /// Mes de la firma del contrato
    /// </summary>
    public string Month { get; set; } = string.Empty;

    /// <summary>
    /// Año de la firma del contrato
    /// </summary>
    public int Year { get; set; }
}
