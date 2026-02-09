using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

/// <summary>
/// Validator for CreateAmazonAccountRequest.
/// </summary>
public class CreateAmazonAccountValidator : AbstractValidator<CreateAmazonAccountRequest>
{
    public CreateAmazonAccountValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0)
            .WithMessage("Customer ID is required and must be greater than zero.");

        RuleFor(x => x.AmazonAccountIdentifier)
            .NotEmpty().WithMessage("Amazon account identifier is required.")
            .MaximumLength(50).WithMessage("Amazon account identifier cannot exceed 50 characters.");

        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.")
            .MaximumLength(500).WithMessage("Refresh token cannot exceed 500 characters.");

        RuleFor(x => x.AmazonRegion)
            .NotEmpty().WithMessage("Amazon region is required.")
            .MaximumLength(20).WithMessage("Amazon region cannot exceed 20 characters.");
    }
}
