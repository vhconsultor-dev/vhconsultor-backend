using FluentValidation;
using BusinessLayer.Shared.Commands;

namespace BusinessLayer.Shared.Validators;

/// <summary>
/// Validador para LockAccountCommand
/// </summary>
public class LockAccountCommandValidator : AbstractValidator<LockAccountCommand>
{
    public LockAccountCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("El ID de usuario debe ser mayor a 0");

        RuleFor(x => x.LockDurationMinutes)
            .GreaterThan(0)
            .WithMessage("La duración del bloqueo debe ser mayor a 0 minutos")
            .LessThanOrEqualTo(10080) // 7 días en minutos
            .WithMessage("La duración del bloqueo no puede exceder 7 días (10080 minutos)");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("La razón no puede exceder 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}

