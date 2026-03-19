using FluentValidation;
using BusinessLayer.Amazon.Commands;

namespace BusinessLayer.Amazon.Validators;

/// <summary>
/// Validador para el comando de generar access token de Amazon Vendor por credenciales de Brand Partner
/// </summary>
public class GenerateVendorAccessTokenByCustomerValidator : AbstractValidator<GenerateVendorAccessTokenByCustomerCommand>
{
    public GenerateVendorAccessTokenByCustomerValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("El email es requerido")
            .EmailAddress()
            .WithMessage("El email debe ser válido");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("La contraseña es requerida")
            .MinimumLength(8)
            .WithMessage("La contraseña debe tener al menos 8 caracteres");
    }
}
