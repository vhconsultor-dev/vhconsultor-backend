using FluentValidation;
using BusinessLayer.BrandPartner.Commands;

namespace BusinessLayer.BrandPartner.Validators;

public class BrandPartnerLoginValidator : AbstractValidator<BrandPartnerLoginCommand>
{
    public BrandPartnerLoginValidator()
    {
        RuleFor(x => x.EmailOrUsername)
            .NotEmpty()
            .WithMessage("Email or username is required")
            .MaximumLength(255)
            .WithMessage("Email or username must be at most 255 characters");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required");
    }
}
