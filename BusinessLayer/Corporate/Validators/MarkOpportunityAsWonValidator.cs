using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

public class MarkOpportunityAsWonValidator : AbstractValidator<MarkOpportunityAsWonRequest>
{
    public MarkOpportunityAsWonValidator()
    {
        RuleFor(x => x.OpportunityId)
            .GreaterThan(0)
            .WithMessage("Opportunity ID must be greater than 0.");

        RuleFor(x => x.CustomerId)
            .GreaterThan(0)
            .WithMessage("Customer ID must be greater than 0.");

        RuleFor(x => x.ContractId)
            .GreaterThan(0)
            .When(x => x.ContractId.HasValue)
            .WithMessage("Contract ID must be greater than 0 when provided.");
    }
}
