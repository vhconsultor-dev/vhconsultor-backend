using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

/// <summary>
/// Validator para UpdateContractRequest
/// </summary>
public class UpdateContractValidator : AbstractValidator<UpdateContractRequest>
{
    public UpdateContractValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("El ID del customer debe ser mayor a 0");

        RuleFor(x => x.FeeTypeId)
            .GreaterThan(0).WithMessage("El ID del tipo de tarifa (FeeTypeId) es requerido y debe ser mayor a 0");

        RuleFor(x => x.ContractNumber)
            .NotEmpty().WithMessage("El número de contrato es requerido")
            .MaximumLength(100).WithMessage("El número de contrato no puede exceder 100 caracteres");

        RuleFor(x => x.ClientEmail)
            .EmailAddress().WithMessage("El email del cliente no es válido")
            .When(x => !string.IsNullOrEmpty(x.ClientEmail));

        RuleFor(x => x.FeeAmount)
            .GreaterThanOrEqualTo(0).WithMessage("El monto de la tarifa debe ser mayor o igual a 0")
            .When(x => x.FeeAmount.HasValue);

        RuleFor(x => x.CurrencyCode)
            .Length(3).WithMessage("El código de moneda debe tener exactamente 3 caracteres")
            .When(x => !string.IsNullOrEmpty(x.CurrencyCode));

        RuleFor(x => x.PaymentDay)
            .InclusiveBetween(1, 31).WithMessage("El día de pago debe estar entre 1 y 31")
            .When(x => x.PaymentDay.HasValue);

        RuleFor(x => x.Status)
            .MaximumLength(50).WithMessage("El estado no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Status));

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate).WithMessage("La fecha de fin debe ser posterior a la fecha de inicio")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

        RuleFor(x => x.EffectiveDate)
            .LessThanOrEqualTo(x => x.StartDate).WithMessage("La fecha efectiva debe ser anterior o igual a la fecha de inicio")
            .When(x => x.EffectiveDate.HasValue && x.StartDate.HasValue);
    }
}

