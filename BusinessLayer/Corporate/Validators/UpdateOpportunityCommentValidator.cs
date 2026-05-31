using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

public class UpdateOpportunityCommentValidator : AbstractValidator<UpdateOpportunityCommentRequest>
{
    public UpdateOpportunityCommentValidator()
    {
        RuleFor(x => x.CommentId)
            .GreaterThan(0)
            .WithMessage("Comment ID must be greater than 0.");

        RuleFor(x => x.Body)
            .NotEmpty()
            .WithMessage("Comment body is required.")
            .MaximumLength(2000)
            .WithMessage("Comment body cannot exceed 2000 characters.");
    }
}
