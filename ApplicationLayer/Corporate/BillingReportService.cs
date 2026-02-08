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
    /// Gets invoices with optional filters (status type, customer tax id, customer name, fee type, etc.).
    /// Supports all scenarios: all invoices, pending, overdue, paid, cancelled, draft, sent.
    /// When multiple filters are provided, they are combined with AND. When none are provided, all invoices are returned.
    /// </summary>
    public async Task<IEnumerable<InvoiceReportDto>> GetInvoicesAsync(InvoiceReportFilter filter)
    {
        return await _billingReportQueryRepository.GetInvoicesAsync(filter);
    }
}
