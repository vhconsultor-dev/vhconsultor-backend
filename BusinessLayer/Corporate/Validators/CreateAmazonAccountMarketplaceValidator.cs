using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

/// <summary>
/// Validator for CreateAmazonAccountMarketplaceRequest.
/// </summary>
public class CreateAmazonAccountMarketplaceValidator : AbstractValidator<CreateAmazonAccountMarketplaceRequest>
{
    public CreateAmazonAccountMarketplaceValidator()
    {
        RuleFor(x => x.AmazonAccountId)
            .GreaterThan(0)
            .WithMessage("Amazon account ID is required and must be greater than zero.");

        RuleFor(x => x.AmazonMarketplaceId)
            .GreaterThan(0)
            .WithMessage("Amazon marketplace ID is required and must be greater than zero.");
    }
}
