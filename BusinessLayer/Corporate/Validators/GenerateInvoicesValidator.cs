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
            .GreaterThan(0).WithMessage("El ID del contrato debe ser mayor a 0");
    }
}

