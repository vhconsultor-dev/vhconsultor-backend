using BusinessLayer.BrandPartner.Commands;
using FluentValidation;

namespace BusinessLayer.BrandPartner.Validators;

public class UpdateInventoryItemValidator : AbstractValidator<UpdateInventoryItemRequest>
{
    public UpdateInventoryItemValidator()
    {
        RuleFor(x => x.Asin)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.Asin))
            .WithMessage("ASIN cannot exceed 20 characters.");

        RuleFor(x => x.ProductName)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.ProductName))
            .WithMessage("Product Name cannot exceed 500 characters.");

        RuleFor(x => x.PrepOwner)
            .MaximumLength(30)
            .When(x => !string.IsNullOrEmpty(x.PrepOwner))
            .WithMessage("Prep Owner cannot exceed 30 characters.");

        RuleFor(x => x.LabelingOwner)
            .MaximumLength(30)
            .When(x => !string.IsNullOrEmpty(x.LabelingOwner))
            .WithMessage("Labeling Owner cannot exceed 30 characters.");

        RuleFor(x => x.UnitsPerBox)
            .GreaterThanOrEqualTo(0)
            .When(x => x.UnitsPerBox.HasValue)
            .WithMessage("Units Per Box must be non-negative.");

        RuleFor(x => x.NumberOfBoxes)
            .GreaterThanOrEqualTo(0)
            .When(x => x.NumberOfBoxes.HasValue)
            .WithMessage("Number Of Boxes must be non-negative.");

        RuleFor(x => x.BoxLengthIn)
            .GreaterThanOrEqualTo(0)
            .When(x => x.BoxLengthIn.HasValue)
            .WithMessage("Box Length must be non-negative.");

        RuleFor(x => x.BoxWidthIn)
            .GreaterThanOrEqualTo(0)
            .When(x => x.BoxWidthIn.HasValue)
            .WithMessage("Box Width must be non-negative.");

        RuleFor(x => x.BoxHeightIn)
            .GreaterThanOrEqualTo(0)
            .When(x => x.BoxHeightIn.HasValue)
            .WithMessage("Box Height must be non-negative.");

        RuleFor(x => x.BoxWeightLb)
            .GreaterThanOrEqualTo(0)
            .When(x => x.BoxWeightLb.HasValue)
            .WithMessage("Box Weight must be non-negative.");
    }
}
