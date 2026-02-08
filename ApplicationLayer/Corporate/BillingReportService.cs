using BusinessLayer.Corporate.Queries;

namespace ApplicationLayer.Corporate;

/// <summary>
/// Application service for billing reports.
/// </summary>
public class BillingReportService
{
    private readonly BillingReportQueryRepository _billingReportQueryRepository;

    public BillingReportService(BillingReportQueryRepository billingReportQueryRepository)
    {
        _billingReportQueryRepository = billingReportQueryRepository;
    }

    /// <summary>
    /// Gets all invoices pending payment, with optional filters (customer tax id, customer name, fee type).
    /// When multiple filters are provided, they are combined with AND. When none are provided, all pending invoices are returned.
    /// </summary>
    public async Task<IEnumerable<PendingInvoiceReportDto>> GetPendingInvoicesAsync(PendingInvoicesReportFilter filter)
    {
        return await _billingReportQueryRepository.GetPendingInvoicesAsync(filter);
    }
}
