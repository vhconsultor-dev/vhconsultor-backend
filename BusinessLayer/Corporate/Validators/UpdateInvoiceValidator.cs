using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

/// <summary>
/// Validator para UpdateInvoiceRequest
/// </summary>
public class UpdateInvoiceValidator : AbstractValidator<UpdateInvoiceRequest>
{
    public UpdateInvoiceValidator()
    {
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

        RuleFor(x => x.Status)
            .Must(status => string.IsNullOrEmpty(status) || new[] { "Draft", "Sent", "Paid", "Overdue", "Cancelled" }.Contains(status))
            .WithMessage("Status must be one of: Draft, Sent, Paid, Overdue, Cancelled")
            .When(x => !string.IsNullOrEmpty(x.Status));

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
