using BusinessLayer.Corporate.Commands;
using FluentValidation;

namespace BusinessLayer.Corporate.Validators;

public class UpdateServiceBudgetRangeCommandValidator : AbstractValidator<UpdateServiceBudgetRangeCommand>
{
    public UpdateServiceBudgetRangeCommandValidator()
    {
        RuleFor(x => x.ServiceId)
            .GreaterThan(0).When(x => x.ServiceId.HasValue)
            .WithMessage("ServiceId debe ser mayor que 0");

        RuleFor(x => x.BusinessTypeId)
            .GreaterThan(0).When(x => x.BusinessTypeId.HasValue)
            .WithMessage("BusinessTypeId debe ser mayor que 0");

        RuleFor(x => x.PlatformId)
            .GreaterThan(0).When(x => x.PlatformId.HasValue)
            .WithMessage("PlatformId debe ser mayor que 0 si se proporciona");

        RuleFor(x => x.MinBudgetValue)
            .GreaterThan(0).When(x => x.MinBudgetValue.HasValue)
            .WithMessage("MinBudgetValue debe ser mayor que 0");

        RuleFor(x => x.Percentage)
            .InclusiveBetween(0, 100).When(x => x.Percentage.HasValue)
            .WithMessage("Percentage debe estar entre 0 y 100");
    }
}

