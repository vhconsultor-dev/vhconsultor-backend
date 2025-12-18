using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

/// <summary>
/// Validator para CreateContractServiceRequest
/// </summary>
public class CreateContractServiceValidator : AbstractValidator<CreateContractServiceRequest>
{
    public CreateContractServiceValidator()
    {
        RuleFor(x => x.ContractId)
            .GreaterThan(0).WithMessage("El ID del contrato debe ser mayor a 0");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("El precio unitario debe ser mayor o igual a 0")
            .When(x => x.UnitPrice.HasValue);

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0")
            .When(x => x.Quantity.HasValue);

        RuleFor(x => x.DiscountPercentage)
            .InclusiveBetween(0, 100).WithMessage("El porcentaje de descuento debe estar entre 0 y 100")
            .When(x => x.DiscountPercentage.HasValue);

        RuleFor(x => x.FinalPrice)
            .GreaterThanOrEqualTo(0).WithMessage("El precio final debe ser mayor o igual a 0")
            .When(x => x.FinalPrice.HasValue);

        RuleFor(x => x.ServiceOrder)
            .GreaterThan(0).WithMessage("El orden del servicio debe ser mayor a 0")
            .When(x => x.ServiceOrder.HasValue);

        RuleFor(x => x.BillingFrequency)
            .MaximumLength(50).WithMessage("La frecuencia de facturación no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.BillingFrequency));
    }
}

