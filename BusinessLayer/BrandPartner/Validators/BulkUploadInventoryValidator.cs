using BusinessLayer.BrandPartner.Commands;
using FluentValidation;

namespace BusinessLayer.BrandPartner.Validators;

public class BulkUploadInventoryValidator : AbstractValidator<BulkUploadInventoryRequest>
{
    public BulkUploadInventoryValidator()
    {
        RuleFor(x => x.AmazonAccountId)
            .GreaterThan(0)
            .WithMessage("Amazon Account ID is required and must be greater than zero.");

        RuleFor(x => x.InventoryItems)
            .NotNull()
            .WithMessage("Inventory items list is required.");

        RuleFor(x => x.InventoryItems)
            .NotEmpty()
            .When(x => x.InventoryItems != null)
            .WithMessage("At least one inventory item is required.");

        RuleFor(x => x.InventoryItems)
            .Must(items => items == null || items.Count <= 1000)
            .WithMessage("Maximum 1000 inventory items allowed per request.");

        RuleForEach(x => x.InventoryItems)
            .SetValidator(new InventoryItemRequestValidator())
            .When(x => x.InventoryItems != null);
    }
}

public class InventoryItemRequestValidator : AbstractValidator<InventoryItemRequest>
{
    public InventoryItemRequestValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty()
            .WithMessage("SKU is required for each inventory item.");

        RuleFor(x => x.Sku)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Sku))
            .WithMessage("SKU must not exceed 100 characters.");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Quantity must be zero or positive.");

        RuleFor(x => x.ProductName)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.ProductName))
            .WithMessage("Product name must not exceed 500 characters.");

        RuleFor(x => x.PrepOwner)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.PrepOwner))
            .WithMessage("Prep owner must not exceed 100 characters.");

        RuleFor(x => x.LabelingOwner)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.LabelingOwner))
            .WithMessage("Labeling owner must not exceed 100 characters.");

        RuleFor(x => x.UnitsPerBox)
            .GreaterThan(0)
            .When(x => x.UnitsPerBox.HasValue)
            .WithMessage("Units per box must be greater than zero when specified.");

        RuleFor(x => x.NumberOfBoxes)
            .GreaterThan(0)
            .When(x => x.NumberOfBoxes.HasValue)
            .WithMessage("Number of boxes must be greater than zero when specified.");

        RuleFor(x => x.BoxLengthIn)
            .GreaterThan(0)
            .When(x => x.BoxLengthIn.HasValue)
            .WithMessage("Box length must be greater than zero when specified.");

        RuleFor(x => x.BoxWidthIn)
            .GreaterThan(0)
            .When(x => x.BoxWidthIn.HasValue)
            .WithMessage("Box width must be greater than zero when specified.");

        RuleFor(x => x.BoxHeightIn)
            .GreaterThan(0)
            .When(x => x.BoxHeightIn.HasValue)
            .WithMessage("Box height must be greater than zero when specified.");

        RuleFor(x => x.BoxWeightLb)
            .GreaterThan(0)
            .When(x => x.BoxWeightLb.HasValue)
            .WithMessage("Box weight must be greater than zero when specified.");
    }
}
