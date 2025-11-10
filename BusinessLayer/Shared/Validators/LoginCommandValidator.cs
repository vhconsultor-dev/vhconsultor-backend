using FluentValidation;
using BusinessLayer.Shared.Commands;

namespace BusinessLayer.Shared.Validators;

/// <summary>
/// Validador para LoginCommand
/// </summary>
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.UsernameOrEmail)
            .NotEmpty()
            .WithMessage("El nombre de usuario o email es requerido")
            .MaximumLength(255)
            .WithMessage("El nombre de usuario o email no puede exceder 255 caracteres");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("La contraseña es requerida");

        RuleFor(x => x.IPAddress)
            .MaximumLength(45)
            .WithMessage("La dirección IP no puede exceder 45 caracteres")
            .When(x => !string.IsNullOrEmpty(x.IPAddress));

        RuleFor(x => x.UserAgent)
            .MaximumLength(500)
            .WithMessage("El User Agent no puede exceder 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.UserAgent));
    }
}

