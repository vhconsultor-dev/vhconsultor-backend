using FluentValidation;
using BusinessLayer.Amazon.Commands;

namespace BusinessLayer.Amazon.Validators;

/// <summary>
/// Validador para el comando de generar access token de Amazon
/// </summary>
public class GenerateAccessTokenValidator : AbstractValidator<GenerateAccessTokenCommand>
{
    public GenerateAccessTokenValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("El Refresh Token es requerido")
            .MinimumLength(50).WithMessage("El Refresh Token debe tener al menos 50 caracteres");

        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("El Client ID es requerido")
            .Must(x => x.StartsWith("amzn1.application-oa2-client."))
            .WithMessage("El Client ID debe tener el formato correcto de Amazon");

        RuleFor(x => x.ClientSecret)
            .NotEmpty().WithMessage("El Client Secret es requerido")
            .Must(x => x.StartsWith("amzn1.oa2-cs."))
            .WithMessage("El Client Secret debe tener el formato correcto de Amazon");
    }
}
