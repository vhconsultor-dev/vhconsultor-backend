using FluentValidation;
using BusinessLayer.Shared.Commands;

namespace BusinessLayer.Shared.Validators;

/// <summary>
/// Validador para UnlockAccountCommand
/// </summary>
public class UnlockAccountCommandValidator : AbstractValidator<UnlockAccountCommand>
{
    public UnlockAccountCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("El ID de usuario debe ser mayor a 0");
    }
}

