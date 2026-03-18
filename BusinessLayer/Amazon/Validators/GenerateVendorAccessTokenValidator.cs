using FluentValidation;
using BusinessLayer.Amazon.Commands;

namespace BusinessLayer.Amazon.Validators;

/// <summary>
/// Validador para el comando de generar access token de Amazon Vendor
/// </summary>
public class GenerateVendorAccessTokenValidator : AbstractValidator<GenerateVendorAccessTokenCommand>
{
    public GenerateVendorAccessTokenValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("El Refresh Token es requerido")
            .MinimumLength(20)
            .WithMessage("El Refresh Token debe tener al menos 20 caracteres")
            .Matches(@"^[A-Za-z0-9\-_.]+$")
            .WithMessage("El Refresh Token debe tener el formato correcto de Amazon");
    }
}
