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

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]")
            .WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]")
            .WithMessage("Password must contain at least one digit");

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
