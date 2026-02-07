using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

/// <summary>
/// Validator para CreateManualInvoiceRequest
/// </summary>
public class CreateManualInvoiceValidator : AbstractValidator<CreateManualInvoiceRequest>
{
    public CreateManualInvoiceValidator()
    {
        RuleFor(x => x.ContractId)
            .GreaterThan(0).WithMessage("Contract ID must be greater than 0");

        RuleFor(x => x.InvoiceDate)
            .NotEmpty().WithMessage("Invoice date is required");

        RuleFor(x => x.DueDate)
            .NotEmpty().WithMessage("Due date is required")
            .GreaterThanOrEqualTo(x => x.InvoiceDate).WithMessage("Due date must be equal to or after invoice date");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Amount must be greater than or equal to 0");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
