using FluentValidation;
using BusinessLayer.BrandPartner.Commands;

namespace BusinessLayer.BrandPartner.Validators;

public class CreateBrandPartnerUserValidator : AbstractValidator<CreateBrandPartnerUserRequest>
{
    public CreateBrandPartnerUserValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0)
            .WithMessage("CustomerId must be greater than 0");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format")
            .MaximumLength(255)
            .WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("FirstName is required")
            .MaximumLength(100)
            .WithMessage("FirstName must not exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("LastName is required")
            .MaximumLength(100)
            .WithMessage("LastName must not exceed 100 characters");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(50)
            .WithMessage("PhoneNumber must not exceed 50 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}
