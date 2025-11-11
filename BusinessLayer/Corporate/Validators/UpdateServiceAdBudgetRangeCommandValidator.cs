using BusinessLayer.Corporate.Commands;
using FluentValidation;

namespace BusinessLayer.Corporate.Validators;

public class UpdateServiceAdBudgetRangeCommandValidator : AbstractValidator<UpdateServiceAdBudgetRangeCommand>
{
    public UpdateServiceAdBudgetRangeCommandValidator()
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

        RuleFor(x => x.MinAdBudgetValue)
            .GreaterThan(0).When(x => x.MinAdBudgetValue.HasValue)
            .WithMessage("MinAdBudgetValue debe ser mayor que 0");

        RuleFor(x => x.FixedQuote)
            .GreaterThan(0).When(x => x.FixedQuote.HasValue)
            .WithMessage("FixedQuote debe ser mayor que 0");
    }
}

