namespace BusinessLayer.Corporate.Commands;

public class CreateServiceAdBudgetRangeCommand
{
    public int ServiceId { get; set; }
    public int BusinessTypeId { get; set; }
    public int? PlatformId { get; set; }
    public decimal MinAdBudgetValue { get; set; }
    public decimal? MaxAdBudgetValue { get; set; }
    public decimal FixedQuote { get; set; }
    public bool IsActive { get; set; } = true;
}

