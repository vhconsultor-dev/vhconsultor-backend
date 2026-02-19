using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

/// <summary>
/// Validator for GenerateInvoicePdfRequest.
/// </summary>
public class GenerateInvoicePdfRequestValidator : AbstractValidator<GenerateInvoicePdfRequest>
{
    public GenerateInvoicePdfRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one invoice item is required.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Item description is required.");
            item.RuleFor(x => x.Qty)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Item quantity must be greater than or equal to zero.");
            item.RuleFor(x => x.Unitprice)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Item unit price must be greater than or equal to zero.");
        });

        RuleFor(x => x.InvoiceNo)
            .NotEmpty()
            .WithMessage("Invoice number is required.");
    }
}
