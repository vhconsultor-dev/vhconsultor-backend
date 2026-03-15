using FluentValidation;
using BusinessLayer.BrandPartner.Commands;

namespace BusinessLayer.BrandPartner.Validators;

public class VerifyTwoFactorCodeValidator : AbstractValidator<VerifyTwoFactorCodeCommand>
{
    public VerifyTwoFactorCodeValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("2FA code is required")
            .Length(5)
            .WithMessage("2FA code must be exactly 5 characters")
            .Matches("^[A-Z][0-9]{4}$")
            .WithMessage("Invalid 2FA code format. Expected format: A1234 (1 uppercase letter + 4 digits)");
    }
}
