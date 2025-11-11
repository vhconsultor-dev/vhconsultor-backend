using BusinessLayer.Corporate.Commands;
using FluentValidation;

namespace BusinessLayer.Corporate.Validators;

public class CalculatePricingCommandValidator : AbstractValidator<CalculatePricingCommand>
{
    public CalculatePricingCommandValidator()
    {
        RuleFor(x => x.BusinessTypeId)
            .GreaterThan(0).WithMessage("BusinessTypeId debe ser mayor que 0");

        RuleFor(x => x.ServiceId)
            .GreaterThan(0).WithMessage("ServiceId debe ser mayor que 0");

        RuleFor(x => x.PlatformId)
            .GreaterThan(0).When(x => x.PlatformId.HasValue)
            .WithMessage("PlatformId debe ser mayor que 0 si se proporciona");

        RuleFor(x => x.BudgetAmount)
            .GreaterThan(0).WithMessage("BudgetAmount debe ser mayor que 0");
    }
}

