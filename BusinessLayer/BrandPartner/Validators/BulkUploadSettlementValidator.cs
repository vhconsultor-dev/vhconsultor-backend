using BusinessLayer.BrandPartner.Commands;
using FluentValidation;

namespace BusinessLayer.BrandPartner.Validators;

public class BulkUploadSettlementValidator : AbstractValidator<BulkUploadSettlementRequest>
{
    public BulkUploadSettlementValidator()
    {
        RuleFor(x => x.AmazonAccountId)
            .GreaterThan(0)
            .WithMessage("Amazon Account ID is required and must be greater than zero.");

        RuleFor(x => x.SettlementTransactions)
            .NotNull()
            .WithMessage("Settlement transactions list is required.");

        RuleFor(x => x.SettlementTransactions)
            .NotEmpty()
            .When(x => x.SettlementTransactions != null)
            .WithMessage("At least one settlement transaction is required.");

        RuleFor(x => x.SettlementTransactions)
            .Must(transactions => transactions == null || transactions.Count <= 5000)
            .WithMessage("Maximum 5000 settlement transactions allowed per request.");

        RuleForEach(x => x.SettlementTransactions)
            .SetValidator(new SettlementTransactionRequestValidator())
            .When(x => x.SettlementTransactions != null);

        RuleFor(x => x.SettlementTransactions)
            .Must(HaveConsistentSettlementId)
            .When(x => x.SettlementTransactions != null && x.SettlementTransactions.Any())
            .WithMessage("All transactions must have the same settlement-id.");
    }

    private bool HaveConsistentSettlementId(List<SettlementTransactionRequest>? transactions)
    {
        if (transactions == null || !transactions.Any()) return true;

        var settlementIds = transactions
            .Where(t => !string.IsNullOrWhiteSpace(t.SettlementId))
            .Select(t => t.SettlementId.Trim())
            .Distinct()
            .ToList();

        return settlementIds.Count <= 1;
    }
}

public class SettlementTransactionRequestValidator : AbstractValidator<SettlementTransactionRequest>
{
    public SettlementTransactionRequestValidator()
    {
        RuleFor(x => x.SettlementId)
            .NotEmpty()
            .WithMessage("Settlement ID is required for each transaction.");

        RuleFor(x => x.SettlementId)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.SettlementId))
            .WithMessage("Settlement ID must not exceed 100 characters.");

        RuleFor(x => x.Currency)
            .MaximumLength(10)
            .When(x => !string.IsNullOrEmpty(x.Currency))
            .WithMessage("Currency must not exceed 10 characters.");

        RuleFor(x => x.TransactionType)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.TransactionType))
            .WithMessage("Transaction type must not exceed 50 characters.");

        RuleFor(x => x.OrderId)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.OrderId))
            .WithMessage("Order ID must not exceed 100 characters.");

        RuleFor(x => x.MerchantOrderId)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.MerchantOrderId))
            .WithMessage("Merchant order ID must not exceed 100 characters.");

        RuleFor(x => x.Sku)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Sku))
            .WithMessage("SKU must not exceed 100 characters.");

        RuleFor(x => x.MarketplaceName)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.MarketplaceName))
            .WithMessage("Marketplace name must not exceed 100 characters.");

        RuleFor(x => x.AdjustmentId)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.AdjustmentId))
            .WithMessage("Adjustment ID must not exceed 100 characters.");

        RuleFor(x => x.ShipmentId)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.ShipmentId))
            .WithMessage("Shipment ID must not exceed 100 characters.");

        RuleFor(x => x.FulfillmentId)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.FulfillmentId))
            .WithMessage("Fulfillment ID must not exceed 100 characters.");

        RuleFor(x => x.OrderItemCode)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.OrderItemCode))
            .WithMessage("Order item code must not exceed 100 characters.");

        RuleFor(x => x.MerchantOrderItemId)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.MerchantOrderItemId))
            .WithMessage("Merchant order item ID must not exceed 100 characters.");

        RuleFor(x => x.MerchantAdjustmentItemId)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.MerchantAdjustmentItemId))
            .WithMessage("Merchant adjustment item ID must not exceed 100 characters.");

        RuleFor(x => x.PriceType)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.PriceType))
            .WithMessage("Price type must not exceed 50 characters.");

        RuleFor(x => x.ShipmentFeeType)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.ShipmentFeeType))
            .WithMessage("Shipment fee type must not exceed 50 characters.");

        RuleFor(x => x.OrderFeeType)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.OrderFeeType))
            .WithMessage("Order fee type must not exceed 50 characters.");

        RuleFor(x => x.ItemRelatedFeeType)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.ItemRelatedFeeType))
            .WithMessage("Item related fee type must not exceed 50 characters.");

        RuleFor(x => x.OtherFeeReasonDescription)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.OtherFeeReasonDescription))
            .WithMessage("Other fee reason description must not exceed 500 characters.");

        RuleFor(x => x.PromotionId)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.PromotionId))
            .WithMessage("Promotion ID must not exceed 100 characters.");

        RuleFor(x => x.PromotionType)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.PromotionType))
            .WithMessage("Promotion type must not exceed 50 characters.");

        RuleFor(x => x.DirectPaymentType)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.DirectPaymentType))
            .WithMessage("Direct payment type must not exceed 50 characters.");

        // Validaciones de fechas
        RuleFor(x => x.SettlementStartDate)
            .LessThanOrEqualTo(x => x.SettlementEndDate)
            .When(x => x.SettlementStartDate.HasValue && x.SettlementEndDate.HasValue)
            .WithMessage("Settlement start date must be less than or equal to settlement end date.");

        RuleFor(x => x.SettlementEndDate)
            .LessThanOrEqualTo(x => x.DepositDate)
            .When(x => x.SettlementEndDate.HasValue && x.DepositDate.HasValue)
            .WithMessage("Settlement end date must be less than or equal to deposit date.");

        // Validaciones lógicas de montos
        RuleFor(x => x.QuantityPurchased)
            .GreaterThan(0)
            .When(x => x.QuantityPurchased.HasValue)
            .WithMessage("Quantity purchased must be greater than zero when specified.");
    }
}
