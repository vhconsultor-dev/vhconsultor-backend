using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

/// <summary>
/// Validator para MarkInvoiceAsPaidRequest
/// </summary>
public class MarkInvoiceAsPaidValidator : AbstractValidator<MarkInvoiceAsPaidRequest>
{
    public MarkInvoiceAsPaidValidator()
    {
        RuleFor(x => x.PaidDate)
            .LessThanOrEqualTo(DateTime.Now).WithMessage("Payment date cannot be in the future")
            .When(x => x.PaidDate.HasValue);

        RuleFor(x => x.PaymentMethodId)
            .GreaterThan(0).WithMessage("Payment method must be valid")
            .When(x => x.PaymentMethodId.HasValue);

        RuleFor(x => x.PaymentReference)
            .MaximumLength(255).WithMessage("Payment reference cannot exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.PaymentReference));

        RuleFor(x => x.DepositNumber)
            .MaximumLength(255).WithMessage("Deposit number cannot exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.DepositNumber));

        RuleFor(x => x.TransferNumber)
            .MaximumLength(255).WithMessage("Transfer number cannot exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.TransferNumber));
    }
}




