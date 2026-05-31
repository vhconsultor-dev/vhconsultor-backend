using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

public class ChangeOpportunityStageValidator : AbstractValidator<ChangeOpportunityStageRequest>
{
    public ChangeOpportunityStageValidator()
    {
        RuleFor(x => x.OpportunityId)
            .GreaterThan(0)
            .WithMessage("Opportunity ID must be greater than 0.");

        RuleFor(x => x.StageKey)
            .NotEmpty()
            .WithMessage("Stage key is required.")
            .MaximumLength(50)
            .WithMessage("Stage key cannot exceed 50 characters.");
    }
}
