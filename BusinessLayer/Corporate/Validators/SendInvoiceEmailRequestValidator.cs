using System.ComponentModel.DataAnnotations;
using System.Linq;
using BusinessLayer.Corporate.Commands;
using FluentValidation;

namespace BusinessLayer.Corporate.Validators;

/// <summary>
/// Validator for SendInvoiceEmailRequest.
/// </summary>
public class SendInvoiceEmailRequestValidator : AbstractValidator<SendInvoiceEmailRequest>
{
    public SendInvoiceEmailRequestValidator()
    {
        RuleFor(x => x.ToEmail)
            .NotEmpty().WithMessage("Recipient email (ToEmail) is required.")
            .EmailAddress().WithMessage("Recipient email is not valid.");

        RuleFor(x => x.CcEmail)
            .Must(BeValidEmailList).WithMessage("CC must be a valid email or multiple emails separated by commas.")
            .When(x => !string.IsNullOrWhiteSpace(x.CcEmail));

        RuleFor(x => x.Data)
            .NotNull().WithMessage("Invoice email data is required.");

        When(x => x.Data != null, () =>
        {
            RuleFor(x => x.Data!.ClientName).NotEmpty().WithMessage("Client name is required.");
            RuleFor(x => x.Data!.CompanyName).NotEmpty().WithMessage("Company name is required.");
            RuleFor(x => x.Data!.InvoiceNumber).NotEmpty().WithMessage("Invoice number is required.");
            RuleFor(x => x.Data!.InvoiceMonth).NotEmpty().WithMessage("Invoice month is required.");
            RuleFor(x => x.Data!.InvoiceYear).NotEmpty().WithMessage("Invoice year is required.");
            RuleFor(x => x.Data!.IssueDate).NotEmpty().WithMessage("Issue date is required.");
            RuleFor(x => x.Data!.Currency).NotEmpty().WithMessage("Currency is required.");
            RuleFor(x => x.Data!.TotalAmount).NotEmpty().WithMessage("Total amount is required.");
        });
    }

    private static bool BeValidEmailList(string? emailList)
    {
        if (string.IsNullOrWhiteSpace(emailList))
            return true;
        var emails = emailList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var validator = new EmailAddressAttribute();
        return emails.All(email => validator.IsValid(email));
    }
}
