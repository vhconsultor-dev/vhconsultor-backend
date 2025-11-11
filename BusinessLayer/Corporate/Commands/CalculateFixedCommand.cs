namespace BusinessLayer.Corporate.Commands;

public class CalculateFixedCommand
{
    public int ServiceId { get; set; }
    public int BusinessTypeId { get; set; }
    public int? PlatformId { get; set; }
    public decimal AnnualAdBudget { get; set; }
}

