namespace BusinessLayer.Corporate.Commands;

public class CalculatePercentageCommand
{
    public int ServiceId { get; set; }
    public int BusinessTypeId { get; set; }
    public int? PlatformId { get; set; }
    public decimal AnnualBudget { get; set; }
}

