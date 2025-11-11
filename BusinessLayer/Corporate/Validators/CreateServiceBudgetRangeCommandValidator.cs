using BusinessLayer.Corporate.Commands;
using FluentValidation;

namespace BusinessLayer.Corporate.Validators;

public class CreateServiceBudgetRangeCommandValidator : AbstractValidator<CreateServiceBudgetRangeCommand>
{
    public CreateServiceBudgetRangeCommandValidator()
    {
        RuleFor(x => x.ServiceId)
            .GreaterThan(0).WithMessage("ServiceId debe ser mayor que 0");

        RuleFor(x => x.BusinessTypeId)
            .GreaterThan(0).WithMessage("BusinessTypeId debe ser mayor que 0");

        RuleFor(x => x.PlatformId)
            .GreaterThan(0).When(x => x.PlatformId.HasValue)
            .WithMessage("PlatformId debe ser mayor que 0 si se proporciona");

        RuleFor(x => x.MinBudgetValue)
            .GreaterThan(0).WithMessage("MinBudgetValue debe ser mayor que 0");

        RuleFor(x => x.MaxBudgetValue)
            .GreaterThan(x => x.MinBudgetValue)
            .When(x => x.MaxBudgetValue.HasValue)
            .WithMessage("MaxBudgetValue debe ser mayor que MinBudgetValue");

        RuleFor(x => x.Percentage)
            .InclusiveBetween(0, 100).WithMessage("Percentage debe estar entre 0 y 100");
    }
}

