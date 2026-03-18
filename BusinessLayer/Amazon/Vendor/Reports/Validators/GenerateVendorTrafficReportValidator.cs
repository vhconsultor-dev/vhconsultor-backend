using FluentValidation;
using BusinessLayer.Amazon.Vendor.Reports.Models;

namespace BusinessLayer.Amazon.Vendor.Reports.Validators;

/// <summary>
/// Validador para el reporte de tráfico de Vendor
/// </summary>
public class GenerateVendorTrafficReportValidator : AbstractValidator<GenerateVendorTrafficReportRequest>
{
    public GenerateVendorTrafficReportValidator()
    {
        RuleFor(x => x.ReportType)
            .NotEmpty()
            .WithMessage("El tipo de reporte es requerido")
            .Equal("GET_VENDOR_TRAFFIC_REPORT")
            .WithMessage("El tipo de reporte debe ser GET_VENDOR_TRAFFIC_REPORT");

        RuleFor(x => x.MarketplaceIds)
            .NotNull()
            .WithMessage("Los marketplace IDs son requeridos")
            .Must(x => x != null && x.Count > 0)
            .WithMessage("Debe especificar al menos un marketplace ID");

        RuleFor(x => x.DataStartTime)
            .NotEmpty()
            .WithMessage("La fecha de inicio es requerida")
            .Must(BeValidIso8601Date)
            .WithMessage("La fecha de inicio debe estar en formato ISO 8601 (ejemplo: 2026-02-01T00:00:00Z)");

        RuleFor(x => x.DataEndTime)
            .NotEmpty()
            .WithMessage("La fecha de fin es requerida")
            .Must(BeValidIso8601Date)
            .WithMessage("La fecha de fin debe estar en formato ISO 8601 (ejemplo: 2026-02-28T23:59:59Z)");

        RuleFor(x => x)
            .Must(x => BeValidDateRange(x.DataStartTime, x.DataEndTime))
            .WithMessage("La fecha de inicio debe ser anterior a la fecha de fin");

        RuleFor(x => x.ReportOptions)
            .NotNull()
            .WithMessage("Las opciones del reporte son requeridas");

        RuleFor(x => x.ReportOptions!.ReportPeriod)
            .NotEmpty()
            .WithMessage("El periodo del reporte es requerido")
            .Must(x => x == "DAY" || x == "WEEK" || x == "MONTH")
            .WithMessage("El periodo del reporte debe ser DAY, WEEK o MONTH")
            .When(x => x.ReportOptions != null);
    }

    private bool BeValidIso8601Date(string dateString)
    {
        if (string.IsNullOrEmpty(dateString))
            return false;

        return DateTime.TryParse(dateString, out _);
    }

    private bool BeValidDateRange(string startDate, string endDate)
    {
        if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate))
            return true; // Dejar que otras validaciones manejen esto

        if (!DateTime.TryParse(startDate, out var start) || !DateTime.TryParse(endDate, out var end))
            return true; // Dejar que otras validaciones manejen esto

        return start < end;
    }
}
