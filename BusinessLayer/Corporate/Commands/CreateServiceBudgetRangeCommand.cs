namespace BusinessLayer.Corporate.Commands;

public class CreateServiceBudgetRangeCommand
{
    public int ServiceId { get; set; }
    public int BusinessTypeId { get; set; }
    public int? PlatformId { get; set; }
    public decimal MinBudgetValue { get; set; }
    public decimal? MaxBudgetValue { get; set; }
    public decimal Percentage { get; set; }
    public bool IsActive { get; set; } = true;
}

