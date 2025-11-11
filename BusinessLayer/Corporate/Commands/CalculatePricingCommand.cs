namespace BusinessLayer.Corporate.Commands;

public class CalculatePricingCommand
{
    public int BusinessTypeId { get; set; }
    public int? PlatformId { get; set; }
    public int ServiceId { get; set; }
    public decimal BudgetAmount { get; set; }
}

