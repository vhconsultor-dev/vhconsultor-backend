using BusinessLayer.Corporate.Commands;
using FluentValidation;

namespace BusinessLayer.Corporate.Validators;

public class CalculatePercentageCommandValidator : AbstractValidator<CalculatePercentageCommand>
{
    public CalculatePercentageCommandValidator()
    {
        RuleFor(x => x.ServiceId)
            .GreaterThan(0).WithMessage("ServiceId debe ser mayor que 0");

        RuleFor(x => x.BusinessTypeId)
            .GreaterThan(0).WithMessage("BusinessTypeId debe ser mayor que 0");

        RuleFor(x => x.PlatformId)
            .GreaterThan(0).When(x => x.PlatformId.HasValue)
            .WithMessage("PlatformId debe ser mayor que 0 si se proporciona");

        RuleFor(x => x.AnnualBudget)
            .GreaterThan(0).WithMessage("AnnualBudget debe ser mayor que 0");
    }
}

