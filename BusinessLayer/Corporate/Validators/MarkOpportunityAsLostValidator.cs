using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

public class MarkOpportunityAsLostValidator : AbstractValidator<MarkOpportunityAsLostRequest>
{
    public MarkOpportunityAsLostValidator()
    {
        RuleFor(x => x.OpportunityId)
            .GreaterThan(0)
            .WithMessage("Opportunity ID must be greater than 0.");

        RuleFor(x => x.LostReasonKey)
            .NotEmpty()
            .WithMessage("Lost reason key is required.")
            .MaximumLength(50)
            .WithMessage("Lost reason key cannot exceed 50 characters.");

        RuleFor(x => x.LostByUserId)
            .GreaterThan(0)
            .WithMessage("Lost by user ID is required and must be greater than 0.");

        RuleFor(x => x.LostReasonNotes)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.LostReasonNotes))
            .WithMessage("Lost reason notes cannot exceed 500 characters.");
    }
}
