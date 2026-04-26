using BusinessLayer.BrandPartner.Commands;
using FluentValidation;

namespace BusinessLayer.BrandPartner.Validators;

public class AdjustInventoryManualValidator : AbstractValidator<AdjustInventoryManualRequest>
{
    public AdjustInventoryManualValidator()
    {
        RuleFor(x => x.InventoryItemId)
            .GreaterThan(0)
            .WithMessage("Inventory Item ID is required and must be greater than zero.");

        RuleFor(x => x.QuantityDelta)
            .NotEqual(0)
            .WithMessage("Quantity Delta cannot be zero. Use a positive value to add inventory or a negative value to subtract.");

        RuleFor(x => x.ReasonCode)
            .NotEmpty()
            .WithMessage("Reason Code is required.");

        RuleFor(x => x.ReasonCode)
            .MaximumLength(50)
            .WithMessage("Reason Code cannot exceed 50 characters.");

        RuleFor(x => x.ReasonCode)
            .Must(BeAValidReasonCode)
            .WithMessage("Reason Code must be one of: COUNT_CORRECTION, DAMAGE, RETURN_TO_STOCK, LOSS, THEFT, ADJUSTMENT.");

        RuleFor(x => x.Comments)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.Comments))
            .WithMessage("Comments cannot exceed 1000 characters.");

        RuleFor(x => x.CreatedBy)
            .NotEmpty()
            .WithMessage("Created By is required.");

        RuleFor(x => x.CreatedBy)
            .MaximumLength(255)
            .WithMessage("Created By cannot exceed 255 characters.");
    }

    private bool BeAValidReasonCode(string reasonCode)
    {
        var validReasonCodes = new[]
        {
            "COUNT_CORRECTION",
            "DAMAGE",
            "RETURN_TO_STOCK",
            "LOSS",
            "THEFT",
            "ADJUSTMENT"
        };

        return validReasonCodes.Contains(reasonCode.ToUpper());
    }
}
