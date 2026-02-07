using BusinessLayer.Corporate.Commands;
using FluentValidation;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace BusinessLayer.Corporate.Validators;

/// <summary>
/// Validator for SendContractEmailRequest
/// </summary>
public class SendContractEmailRequestValidator : AbstractValidator<SendContractEmailRequest>
{
    public SendContractEmailRequestValidator()
    {
        RuleFor(x => x.ToEmail)
            .NotEmpty().WithMessage("El correo del destinatario es requerido")
            .EmailAddress().WithMessage("El correo del destinatario no es válido");

        RuleFor(x => x.CcEmail)
            .Must(BeValidEmailList).WithMessage("El correo CC no es válido. Debe ser un correo válido o múltiples correos separados por comas")
            .When(x => !string.IsNullOrWhiteSpace(x.CcEmail));

        RuleFor(x => x.Data)
            .NotNull().WithMessage("Los datos del correo son requeridos");

        RuleFor(x => x.Data.FullName)
            .NotEmpty().WithMessage("El nombre completo es requerido")
            .When(x => x.Data != null);

        RuleFor(x => x.Data.Identification)
            .NotEmpty().WithMessage("La identificación es requerida")
            .When(x => x.Data != null);

        RuleFor(x => x.Data.ContractNumber)
            .NotEmpty().WithMessage("El número de contrato es requerido")
            .When(x => x.Data != null);

        RuleFor(x => x.Data.CompanyName)
            .NotEmpty().WithMessage("El nombre de la compañía es requerido")
            .When(x => x.Data != null);

        RuleFor(x => x.Data.Day)
            .NotEmpty().WithMessage("El día es requerido")
            .When(x => x.Data != null);

        RuleFor(x => x.Data.Month)
            .NotEmpty().WithMessage("El mes es requerido")
            .When(x => x.Data != null);

        RuleFor(x => x.Data.Year)
            .NotEmpty().WithMessage("El año es requerido")
            .When(x => x.Data != null);
    }

    /// <summary>
    /// Valida que el string sea un correo válido o múltiples correos separados por comas
    /// </summary>
    private bool BeValidEmailList(string? emailList)
    {
        if (string.IsNullOrWhiteSpace(emailList))
            return true; // Opcional, ya se valida con When

        // Dividir por comas y validar cada correo
        var emails = emailList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        
        var emailValidator = new EmailAddressAttribute();
        return emails.All(email => emailValidator.IsValid(email));
    }
}
