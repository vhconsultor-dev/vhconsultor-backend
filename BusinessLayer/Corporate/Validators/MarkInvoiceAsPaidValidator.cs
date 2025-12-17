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
            .LessThanOrEqualTo(DateTime.Now).WithMessage("La fecha de pago no puede ser futura")
            .When(x => x.PaidDate.HasValue);

        RuleFor(x => x.PaymentMethodId)
            .GreaterThan(0).WithMessage("El método de pago debe ser válido")
            .When(x => x.PaymentMethodId.HasValue);

        RuleFor(x => x.PaymentReference)
            .MaximumLength(255).WithMessage("La referencia de pago no puede exceder 255 caracteres")
            .When(x => !string.IsNullOrEmpty(x.PaymentReference));

        RuleFor(x => x.DepositNumber)
            .MaximumLength(255).WithMessage("El número de depósito no puede exceder 255 caracteres")
            .When(x => !string.IsNullOrEmpty(x.DepositNumber));

        RuleFor(x => x.TransferNumber)
            .MaximumLength(255).WithMessage("El número de transferencia no puede exceder 255 caracteres")
            .When(x => !string.IsNullOrEmpty(x.TransferNumber));
    }
}

