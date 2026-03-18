using FluentValidation;
using BusinessLayer.Amazon.Commands;

namespace BusinessLayer.Amazon.Validators;

/// <summary>
/// Validador para el comando de generar access token de Amazon Vendor por CustomerId
/// </summary>
public class GenerateVendorAccessTokenByCustomerValidator : AbstractValidator<GenerateVendorAccessTokenByCustomerCommand>
{
    public GenerateVendorAccessTokenByCustomerValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0)
            .WithMessage("El Customer ID debe ser mayor a 0");
    }
}
