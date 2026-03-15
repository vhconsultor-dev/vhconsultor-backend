using FluentValidation;
using BusinessLayer.BrandPartner.Commands;

namespace BusinessLayer.BrandPartner.Validators;

public class ResetPasswordBrandPartnerValidator : AbstractValidator<ResetPasswordBrandPartnerRequest>
{
    public ResetPasswordBrandPartnerValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format");
    }
}
