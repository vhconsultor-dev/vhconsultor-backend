using BusinessLayer.Corporate.Commands;
using FluentValidation;

namespace BusinessLayer.Corporate.Validators;

public class CreateManualInvoiceHeaderValidator : AbstractValidator<CreateManualInvoiceHeaderRequest>
{
    public CreateManualInvoiceHeaderValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("CustomerId is required.");

        RuleFor(x => x.InvoiceDate)
            .NotEmpty().WithMessage("InvoiceDate is required.");

        RuleFor(x => x.DueDate)
            .NotEmpty().WithMessage("DueDate is required.")
            .GreaterThanOrEqualTo(x => x.InvoiceDate).WithMessage("DueDate must be on or after InvoiceDate.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("CurrencyCode is required.")
            .MaximumLength(10);

        RuleFor(x => x.Details)
            .NotEmpty().WithMessage("At least one detail line is required.");

        RuleForEach(x => x.Details).ChildRules(detail =>
        {
            detail.RuleFor(d => d.ServiceDescription)
                .NotEmpty().WithMessage("ServiceDescription is required for each line.");

            detail.RuleFor(d => d.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0.");

            detail.RuleFor(d => d.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("UnitPrice cannot be negative.");

            detail.RuleFor(d => d.DiscountPercent)
                .InclusiveBetween(0, 100).When(d => d.DiscountPercent.HasValue)
                .WithMessage("DiscountPercent must be between 0 and 100.");
        });

        RuleFor(x => x.TaxRate)
            .InclusiveBetween(0, 100).When(x => x.TaxRate.HasValue)
            .WithMessage("TaxRate must be between 0 and 100.");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).When(x => x.DiscountAmount.HasValue)
            .WithMessage("DiscountAmount cannot be negative.");

        RuleFor(x => x.Lang)
            .Must(l => l == null || l == "en" || l == "es")
            .WithMessage("Lang must be 'en' or 'es'.");
    }
}
