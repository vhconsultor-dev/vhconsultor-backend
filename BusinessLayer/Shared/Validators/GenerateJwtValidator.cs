using BusinessLayer.Shared.Commands;
using FluentValidation;

namespace BusinessLayer.Shared.Validators;

public class GenerateJwtValidator : AbstractValidator<GenerateJwtCommand>
{
    public GenerateJwtValidator()
    {
        RuleFor(x => x.EncryptedPayload)
            .NotEmpty()
            .WithMessage("El payload encriptado es requerido")
            .MinimumLength(10)
            .WithMessage("El payload encriptado no tiene el formato correcto");
    }
}

