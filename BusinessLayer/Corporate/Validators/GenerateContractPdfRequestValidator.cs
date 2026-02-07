using BusinessLayer.Corporate.Commands;
using FluentValidation;

namespace BusinessLayer.Corporate.Validators;

/// <summary>
/// Validador para la generación de PDF de contratos
/// </summary>
public class GenerateContractPdfRequestValidator : AbstractValidator<GenerateContractPdfRequest>
{
    public GenerateContractPdfRequestValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotEmpty()
            .WithMessage("El ID del template es requerido");

        RuleFor(x => x.Data)
            .NotNull()
            .WithMessage("Los datos del contrato son requeridos")
            .SetValidator(new ContractPdfDataValidator());
    }
}

/// <summary>
/// Validador para los datos del PDF del contrato
/// </summary>
public class ContractPdfDataValidator : AbstractValidator<ContractPdfData>
{
    public ContractPdfDataValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty()
            .WithMessage("El nombre de la compañía es requerido")
            .MaximumLength(200)
            .WithMessage("El nombre de la compañía no puede exceder 200 caracteres");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("El nombre completo es requerido")
            .MaximumLength(200)
            .WithMessage("El nombre completo no puede exceder 200 caracteres");

        RuleFor(x => x.TaxId)
            .NotEmpty()
            .WithMessage("El Tax ID es requerido")
            .MaximumLength(50)
            .WithMessage("El Tax ID no puede exceder 50 caracteres");

        RuleFor(x => x.Nationality)
            .NotEmpty()
            .WithMessage("La nacionalidad es requerida")
            .MaximumLength(100)
            .WithMessage("La nacionalidad no puede exceder 100 caracteres");

        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage("La dirección es requerida")
            .MaximumLength(500)
            .WithMessage("La dirección no puede exceder 500 caracteres");

        RuleFor(x => x.Regions)
            .NotEmpty()
            .WithMessage("Las regiones son requeridas")
            .MaximumLength(200)
            .WithMessage("Las regiones no pueden exceder 200 caracteres");

        RuleFor(x => x.PaymentFrequency)
            .NotEmpty()
            .WithMessage("La frecuencia de pago es requerida")
            .MaximumLength(50)
            .WithMessage("La frecuencia de pago no puede exceder 50 caracteres");

        RuleFor(x => x.Fee)
            .NotEmpty()
            .WithMessage("La tarifa (fee) es requerida")
            .MaximumLength(200)
            .WithMessage("La tarifa no puede exceder 200 caracteres");

        RuleFor(x => x.FeePaymentDay)
            .InclusiveBetween(1, 31)
            .WithMessage("El día de pago de la tarifa debe estar entre 1 y 31");

        RuleFor(x => x.ContractDurations)
            .NotEmpty()
            .WithMessage("La duración del contrato es requerida")
            .MaximumLength(100)
            .WithMessage("La duración del contrato no puede exceder 100 caracteres");

        RuleFor(x => x.NoticePeriod)
            .NotEmpty()
            .WithMessage("El período de aviso es requerido")
            .MaximumLength(100)
            .WithMessage("El período de aviso no puede exceder 100 caracteres");

        RuleFor(x => x.WorkingSince)
            .NotEmpty()
            .WithMessage("La fecha de inicio de trabajo (workingSince) es requerida")
            .MaximumLength(20)
            .WithMessage("La fecha de inicio de trabajo no puede exceder 20 caracteres");

        RuleFor(x => x.Day)
            .InclusiveBetween(1, 31)
            .WithMessage("El día debe estar entre 1 y 31");

        RuleFor(x => x.Month)
            .NotEmpty()
            .WithMessage("El mes es requerido")
            .MaximumLength(20)
            .WithMessage("El mes no puede exceder 20 caracteres");

        RuleFor(x => x.Year)
            .GreaterThan(2000)
            .WithMessage("El año debe ser mayor a 2000")
            .LessThanOrEqualTo(2100)
            .WithMessage("El año debe ser menor o igual a 2100");
    }
}
