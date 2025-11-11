using BusinessLayer.Corporate.Commands;
using FluentValidation;

namespace BusinessLayer.Corporate.Validators;

public class CreateServiceAdBudgetRangeCommandValidator : AbstractValidator<CreateServiceAdBudgetRangeCommand>
{
    public CreateServiceAdBudgetRangeCommandValidator()
    {
        RuleFor(x => x.ServiceId)
            .GreaterThan(0).WithMessage("ServiceId debe ser mayor que 0");

        RuleFor(x => x.BusinessTypeId)
            .GreaterThan(0).WithMessage("BusinessTypeId debe ser mayor que 0");

        RuleFor(x => x.PlatformId)
            .GreaterThan(0).When(x => x.PlatformId.HasValue)
            .WithMessage("PlatformId debe ser mayor que 0 si se proporciona");

        RuleFor(x => x.MinAdBudgetValue)
            .GreaterThan(0).WithMessage("MinAdBudgetValue debe ser mayor que 0");

        RuleFor(x => x.MaxAdBudgetValue)
            .GreaterThan(x => x.MinAdBudgetValue)
            .When(x => x.MaxAdBudgetValue.HasValue)
            .WithMessage("MaxAdBudgetValue debe ser mayor que MinAdBudgetValue");

        RuleFor(x => x.FixedQuote)
            .GreaterThan(0).WithMessage("FixedQuote debe ser mayor que 0");
    }
}

