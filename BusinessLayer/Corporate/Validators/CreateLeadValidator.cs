using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

public class CreateLeadValidator : AbstractValidator<CreateLeadRequest>
{
    public CreateLeadValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es requerido")
            .MaximumLength(100).WithMessage("El apellido no puede exceder 100 caracteres");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es requerido")
            .EmailAddress().WithMessage("El formato del email es inválido")
            .MaximumLength(255).WithMessage("El email no puede exceder 255 caracteres");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("El teléfono es requerido")
            .MaximumLength(50).WithMessage("El teléfono no puede exceder 50 caracteres");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("El país es requerido")
            .MaximumLength(100).WithMessage("El país no puede exceder 100 caracteres");

        RuleFor(x => x.BrandName)
            .NotEmpty().WithMessage("El nombre de marca es requerido")
            .MaximumLength(255).WithMessage("El nombre de marca no puede exceder 255 caracteres");

        RuleFor(x => x.NumberOfListings)
            .GreaterThan(0).WithMessage("El número de listados debe ser mayor a 0");

        RuleFor(x => x.ProductPageLink)
            .NotEmpty().WithMessage("El enlace del producto es requerido")
            .Must(BeAValidUrl).WithMessage("El enlace del producto debe ser una URL válida");

        RuleFor(x => x.StoreLink)
            .Must(BeAValidUrlOrEmpty).WithMessage("El enlace de la tienda debe ser una URL válida");

        RuleFor(x => x.SelectedPlatform)
            .NotEmpty().WithMessage("La plataforma es requerida")
            .MaximumLength(50).WithMessage("La plataforma no puede exceder 50 caracteres");
    }

    private bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private bool BeAValidUrlOrEmpty(string? url)
    {
        if (string.IsNullOrEmpty(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}

public class UpdateLeadValidator : AbstractValidator<UpdateLeadRequest>
{
    public UpdateLeadValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es requerido")
            .MaximumLength(100).WithMessage("El apellido no puede exceder 100 caracteres");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es requerido")
            .EmailAddress().WithMessage("El formato del email es inválido")
            .MaximumLength(255).WithMessage("El email no puede exceder 255 caracteres");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("El teléfono es requerido")
            .MaximumLength(50).WithMessage("El teléfono no puede exceder 50 caracteres");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("El país es requerido")
            .MaximumLength(100).WithMessage("El país no puede exceder 100 caracteres");

        RuleFor(x => x.BrandName)
            .NotEmpty().WithMessage("El nombre de marca es requerido")
            .MaximumLength(255).WithMessage("El nombre de marca no puede exceder 255 caracteres");

        RuleFor(x => x.NumberOfListings)
            .GreaterThan(0).WithMessage("El número de listados debe ser mayor a 0");

        RuleFor(x => x.ProductPageLink)
            .NotEmpty().WithMessage("El enlace del producto es requerido")
            .Must(BeAValidUrl).WithMessage("El enlace del producto debe ser una URL válida");

        RuleFor(x => x.StoreLink)
            .Must(BeAValidUrlOrEmpty).WithMessage("El enlace de la tienda debe ser una URL válida");

        RuleFor(x => x.SelectedPlatform)
            .NotEmpty().WithMessage("La plataforma es requerida")
            .MaximumLength(50).WithMessage("La plataforma no puede exceder 50 caracteres");
    }

    private bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private bool BeAValidUrlOrEmpty(string? url)
    {
        if (string.IsNullOrEmpty(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
