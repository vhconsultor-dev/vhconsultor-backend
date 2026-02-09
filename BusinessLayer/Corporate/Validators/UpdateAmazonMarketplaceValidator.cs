using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

/// <summary>
/// Validator for UpdateAmazonMarketplaceRequest.
/// </summary>
public class UpdateAmazonMarketplaceValidator : AbstractValidator<UpdateAmazonMarketplaceRequest>
{
    public UpdateAmazonMarketplaceValidator()
    {
        RuleFor(x => x.AmazonMarketplaceCode)
            .NotEmpty().WithMessage("Amazon marketplace code is required.")
            .MaximumLength(20).WithMessage("Amazon marketplace code cannot exceed 20 characters.");

        RuleFor(x => x.CountryCode)
            .NotEmpty().WithMessage("Country code is required.")
            .MaximumLength(10).WithMessage("Country code cannot exceed 10 characters.");

        RuleFor(x => x.CountryName)
            .NotEmpty().WithMessage("Country name is required.")
            .MaximumLength(100).WithMessage("Country name cannot exceed 100 characters.");

        RuleFor(x => x.AmazonRegion)
            .NotEmpty().WithMessage("Amazon region is required.")
            .MaximumLength(20).WithMessage("Amazon region cannot exceed 20 characters.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Currency code is required.")
            .MaximumLength(10).WithMessage("Currency code cannot exceed 10 characters.");
    }
}
