using FluentValidation;
using BusinessLayer.BrandPartner.Commands;

namespace BusinessLayer.BrandPartner.Validators;

public class ChangePasswordBrandPartnerValidator : AbstractValidator<ChangePasswordBrandPartnerCommand>
{
    public ChangePasswordBrandPartnerValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("UserId must be greater than 0");

        RuleFor(x => x.OldPassword)
            .NotEmpty()
            .WithMessage("Old password is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required")
            .MinimumLength(8)
            .WithMessage("New password must be at least 8 characters")
            .Matches("[A-Z]")
            .WithMessage("New password must contain at least one uppercase letter")
            .Matches("[a-z]")
            .WithMessage("New password must contain at least one lowercase letter")
            .Matches("[0-9]")
            .WithMessage("New password must contain at least one digit")
            .NotEqual(x => x.OldPassword)
            .WithMessage("New password must be different from the old password");
    }
}
