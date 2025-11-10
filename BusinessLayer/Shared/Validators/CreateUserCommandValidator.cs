using FluentValidation;
using BusinessLayer.Shared.Commands;

namespace BusinessLayer.Shared.Validators;

/// <summary>
/// Validador para CreateUserCommand
/// </summary>
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("El nombre es requerido")
            .MaximumLength(100)
            .WithMessage("El nombre no puede exceder 100 caracteres");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("El apellido es requerido")
            .MaximumLength(100)
            .WithMessage("El apellido no puede exceder 100 caracteres");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("El email es requerido")
            .EmailAddress()
            .WithMessage("El formato del email es inválido")
            .MaximumLength(255)
            .WithMessage("El email no puede exceder 255 caracteres");

        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("El nombre de usuario es requerido")
            .MinimumLength(3)
            .WithMessage("El nombre de usuario debe tener al menos 3 caracteres")
            .MaximumLength(100)
            .WithMessage("El nombre de usuario no puede exceder 100 caracteres")
            .Matches(@"^[a-zA-Z0-9_]+$")
            .WithMessage("El nombre de usuario solo puede contener letras, números y guiones bajos");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("La contraseña es requerida")
            .MinimumLength(8)
            .WithMessage("La contraseña debe tener al menos 8 caracteres")
            .Matches(@"[A-Z]")
            .WithMessage("La contraseña debe contener al menos una letra mayúscula")
            .Matches(@"[a-z]")
            .WithMessage("La contraseña debe contener al menos una letra minúscula")
            .Matches(@"[0-9]")
            .WithMessage("La contraseña debe contener al menos un número")
            .Matches(@"[\W_]")
            .WithMessage("La contraseña debe contener al menos un carácter especial");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("La confirmación de contraseña es requerida")
            .Equal(x => x.Password)
            .WithMessage("Las contraseñas no coinciden");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(50)
            .WithMessage("El número de teléfono no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.ProfilePictureUrl)
            .MaximumLength(500)
            .WithMessage("La URL de la imagen de perfil no puede exceder 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.ProfilePictureUrl));
    }
}

