using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

/// <summary>
/// Validator para GeneratePercentageInvoicesRequest
/// </summary>
public class GeneratePercentageInvoicesValidator : AbstractValidator<GeneratePercentageInvoicesRequest>
{
    public GeneratePercentageInvoicesValidator()
    {
        RuleFor(x => x.ContractId)
            .GreaterThan(0).WithMessage("El ID del contrato debe ser mayor a 0");
    }
}