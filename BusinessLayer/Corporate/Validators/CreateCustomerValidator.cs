using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

/// <summary>
/// Validador para la creación de Customers
/// </summary>
public class CreateCustomerValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty()
            .WithMessage("El nombre de la compañía es requerido")
            .MaximumLength(255)
            .WithMessage("El nombre de la compañía no puede exceder 255 caracteres");

        RuleFor(x => x.NIT)
            .NotEmpty()
            .WithMessage("El NIT es requerido")
            .MaximumLength(50)
            .WithMessage("El NIT no puede exceder 50 caracteres");

        RuleFor(x => x.CompanyType)
            .MaximumLength(50)
            .WithMessage("El tipo de compañía no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.CompanyType));

        RuleFor(x => x.PrimaryEmail)
            .EmailAddress()
            .WithMessage("El email primario debe ser una dirección válida")
            .MaximumLength(255)
            .WithMessage("El email primario no puede exceder 255 caracteres")
            .When(x => !string.IsNullOrEmpty(x.PrimaryEmail));

        RuleFor(x => x.SecondaryEmail)
            .EmailAddress()
            .WithMessage("El email secundario debe ser una dirección válida")
            .MaximumLength(255)
            .WithMessage("El email secundario no puede exceder 255 caracteres")
            .When(x => !string.IsNullOrEmpty(x.SecondaryEmail));

        RuleFor(x => x.BillingEmail)
            .EmailAddress()
            .WithMessage("El email de facturación debe ser una dirección válida")
            .MaximumLength(255)
            .WithMessage("El email de facturación no puede exceder 255 caracteres")
            .When(x => !string.IsNullOrEmpty(x.BillingEmail));

        RuleFor(x => x.PrimaryPhone)
            .MaximumLength(50)
            .WithMessage("El teléfono primario no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.PrimaryPhone));

        RuleFor(x => x.SecondaryPhone)
            .MaximumLength(50)
            .WithMessage("El teléfono secundario no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.SecondaryPhone));

        RuleFor(x => x.EmergencyPhone)
            .MaximumLength(50)
            .WithMessage("El teléfono de emergencia no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.EmergencyPhone));

        RuleFor(x => x.Contact1Name)
            .MaximumLength(100)
            .WithMessage("El nombre del contacto 1 no puede exceder 100 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Contact1Name));

        RuleFor(x => x.Contact1Phone)
            .MaximumLength(50)
            .WithMessage("El teléfono del contacto 1 no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Contact1Phone));

        RuleFor(x => x.Contact2Name)
            .MaximumLength(100)
            .WithMessage("El nombre del contacto 2 no puede exceder 100 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Contact2Name));

        RuleFor(x => x.Contact2Phone)
            .MaximumLength(50)
            .WithMessage("El teléfono del contacto 2 no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Contact2Phone));

        RuleFor(x => x.Contact3Name)
            .MaximumLength(100)
            .WithMessage("El nombre del contacto 3 no puede exceder 100 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Contact3Name));

        RuleFor(x => x.Contact3Phone)
            .MaximumLength(50)
            .WithMessage("El teléfono del contacto 3 no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Contact3Phone));

        RuleFor(x => x.State)
            .MaximumLength(100)
            .WithMessage("El estado no puede exceder 100 caracteres")
            .When(x => !string.IsNullOrEmpty(x.State));

        RuleFor(x => x.City)
            .MaximumLength(100)
            .WithMessage("La ciudad no puede exceder 100 caracteres")
            .When(x => !string.IsNullOrEmpty(x.City));

        RuleFor(x => x.Address)
            .MaximumLength(500)
            .WithMessage("La dirección no puede exceder 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.PostalCode)
            .MaximumLength(20)
            .WithMessage("El código postal no puede exceder 20 caracteres")
            .When(x => !string.IsNullOrEmpty(x.PostalCode));

        RuleFor(x => x.CompanySize)
            .MaximumLength(50)
            .WithMessage("El tamaño de la compañía no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.CompanySize));

        RuleFor(x => x.AnnualRevenue)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Los ingresos anuales deben ser mayores o iguales a 0")
            .When(x => x.AnnualRevenue.HasValue);

        RuleFor(x => x.Website)
            .MaximumLength(255)
            .WithMessage("El sitio web no puede exceder 255 caracteres")
            .Must(BeAValidUrlOrEmpty)
            .WithMessage("El sitio web debe ser una URL válida")
            .When(x => !string.IsNullOrEmpty(x.Website));

        RuleFor(x => x.ClientStatus)
            .MaximumLength(50)
            .WithMessage("El estado del cliente no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.ClientStatus));

        RuleFor(x => x.Priority)
            .MaximumLength(20)
            .WithMessage("La prioridad no puede exceder 20 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Priority));

        RuleFor(x => x.Source)
            .MaximumLength(100)
            .WithMessage("La fuente no puede exceder 100 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Source));
    }

    private bool BeAValidUrlOrEmpty(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}

