using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

public class CreateOpportunityCommentValidator : AbstractValidator<CreateOpportunityCommentRequest>
{
    public CreateOpportunityCommentValidator()
    {
        RuleFor(x => x.OpportunityId)
            .GreaterThan(0)
            .WithMessage("Opportunity ID must be greater than 0.");

        RuleFor(x => x.AuthorUserId)
            .GreaterThan(0)
            .WithMessage("Author user ID is required and must be greater than 0.");

        RuleFor(x => x.Body)
            .NotEmpty()
            .WithMessage("Comment body is required.")
            .MaximumLength(2000)
            .WithMessage("Comment body cannot exceed 2000 characters.");
    }
}
