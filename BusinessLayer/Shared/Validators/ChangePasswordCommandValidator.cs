using FluentValidation;
using BusinessLayer.Shared.Commands;

namespace BusinessLayer.Shared.Validators;

/// <summary>
/// Validador para ChangePasswordCommand
/// </summary>
public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("El ID de usuario debe ser mayor a 0");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("La contraseña actual es requerida");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("La nueva contraseña es requerida")
            .MinimumLength(8)
            .WithMessage("La nueva contraseña debe tener al menos 8 caracteres")
            .Matches(@"[A-Z]")
            .WithMessage("La nueva contraseña debe contener al menos una letra mayúscula")
            .Matches(@"[a-z]")
            .WithMessage("La nueva contraseña debe contener al menos una letra minúscula")
            .Matches(@"[0-9]")
            .WithMessage("La nueva contraseña debe contener al menos un número")
            .Matches(@"[\W_]")
            .WithMessage("La nueva contraseña debe contener al menos un carácter especial");

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty()
            .WithMessage("La confirmación de contraseña es requerida")
            .Equal(x => x.NewPassword)
            .WithMessage("Las contraseñas no coinciden");

        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("La nueva contraseña debe ser diferente a la contraseña actual")
            .When(x => !string.IsNullOrEmpty(x.CurrentPassword) && !string.IsNullOrEmpty(x.NewPassword));
    }
}

