using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

public class ConvertLeadToOpportunityValidator : AbstractValidator<ConvertLeadToOpportunityRequest>
{
    public ConvertLeadToOpportunityValidator()
    {
        RuleFor(x => x.SubmissionId)
            .GreaterThan(0)
            .WithMessage("Submission ID must be greater than 0.");

        RuleFor(x => x.AssignedToUserId)
            .GreaterThan(0)
            .WithMessage("Assigned user ID must be greater than 0.");

        RuleFor(x => x.ViewerUserId)
            .GreaterThan(0)
            .When(x => x.ViewerUserId.HasValue)
            .WithMessage("Viewer user ID must be greater than 0 when provided.");

        RuleFor(x => x.ConvertedByUserId)
            .GreaterThan(0)
            .When(x => x.ConvertedByUserId.HasValue)
            .WithMessage("Converted by user ID must be greater than 0 when provided.");
    }
}
