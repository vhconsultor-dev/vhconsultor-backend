using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

public class CreateFollowUpValidator : AbstractValidator<CreateFollowUpRequest>
{
    public CreateFollowUpValidator()
    {
        RuleFor(x => x.OpportunityId)
            .GreaterThan(0)
            .WithMessage("Opportunity ID must be greater than 0.");

        RuleFor(x => x.StageKey)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.StageKey))
            .WithMessage("Stage key cannot exceed 50 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Notes))
            .WithMessage("Notes cannot exceed 500 characters.");
    }
}
