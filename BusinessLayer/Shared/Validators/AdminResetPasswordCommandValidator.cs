using FluentValidation;
using BusinessLayer.Shared.Commands;

namespace BusinessLayer.Shared.Validators;

/// <summary>
/// Validador para AdminResetPasswordCommand
/// </summary>
public class AdminResetPasswordCommandValidator : AbstractValidator<AdminResetPasswordCommand>
{
    public AdminResetPasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("El ID de usuario debe ser mayor a 0");

        RuleFor(x => x.ResetByUserId)
            .GreaterThan(0)
            .WithMessage("El ID del administrador debe ser mayor a 0")
            .When(x => x.ResetByUserId.HasValue);
    }
}

