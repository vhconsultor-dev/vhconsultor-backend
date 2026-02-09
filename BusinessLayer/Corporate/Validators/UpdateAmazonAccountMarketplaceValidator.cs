using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

/// <summary>
/// Validator for UpdateAmazonAccountMarketplaceRequest.
/// </summary>
public class UpdateAmazonAccountMarketplaceValidator : AbstractValidator<UpdateAmazonAccountMarketplaceRequest>
{
    public UpdateAmazonAccountMarketplaceValidator()
    {
        // IsPrimary and IsActive are booleans; no additional rules required for presence.
    }
}
