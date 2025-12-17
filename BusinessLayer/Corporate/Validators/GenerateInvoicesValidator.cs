using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

/// <summary>
/// Validator para GenerateInvoicesRequest
/// </summary>
public class GenerateInvoicesValidator : AbstractValidator<GenerateInvoicesRequest>
{
    public GenerateInvoicesValidator()
    {
        RuleFor(x => x.ContractId)
            .NotEmpty().WithMessage("El ID del contrato es requerido")
            .MaximumLength(50).WithMessage("El ID del contrato no puede exceder 50 caracteres");
    }
}

