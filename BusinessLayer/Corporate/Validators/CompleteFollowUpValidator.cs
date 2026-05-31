using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

public class CompleteFollowUpValidator : AbstractValidator<CompleteFollowUpRequest>
{
    public CompleteFollowUpValidator()
    {
        RuleFor(x => x.FollowUpId)
            .GreaterThan(0)
            .WithMessage("Follow-up ID must be greater than 0.");

        RuleFor(x => x.CompletedByUserId)
            .GreaterThan(0)
            .WithMessage("Completed by user ID is required and must be greater than 0.");
    }
}
