using FluentValidation;
using BusinessLayer.Shared.Commands;

namespace BusinessLayer.Shared.Validators;

/// <summary>
/// Validator for AdminResetPasswordRequest.
/// </summary>
public class AdminResetPasswordValidator : AbstractValidator<AdminResetPasswordRequest>
{
    public AdminResetPasswordValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("User ID is required and must be greater than zero.");
    }
}
