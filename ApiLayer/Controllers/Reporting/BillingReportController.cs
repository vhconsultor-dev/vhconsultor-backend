using ApplicationLayer.Corporate;
using ApiLayer.Tools;
using BusinessLayer.Corporate.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ApiLayer.Controllers.Reporting;

/// <summary>
/// Billing reporting: pending invoices and related reports.
/// </summary>
[ApiController]
[Route("api/corporate/reporting/billing")]
[Authorize]
public class BillingReportController : ControllerBase
{
    private readonly BillingReportService _billingReportService;

    public BillingReportController(BillingReportService billingReportService)
    {
        _billingReportService = billingReportService;
    }

    /// <summary>
    /// Gets all invoices pending payment (Unpaid), ordered by due date ascending.
    /// Optional query parameters: filter by customer tax id (partial), customer name (partial), or fee type (1=Fixed, 2=Percentage).
    /// When multiple parameters are sent, filters are combined with AND. When none are sent, all pending invoices are returned.
    /// </summary>
    /// <param name="customerTaxId">Optional. Customer NIT/tax id; partial match (LIKE).</param>
    /// <param name="customerName">Optional. Customer name (CompanyName); partial match (LIKE).</param>
    /// <param name="feeTypeId">Optional. 1 = Fixed amount, 2 = Percentage. Omit to include all.</param>
    /// <returns>List of pending invoice rows: customer id, tax id, customer name, contract number, invoice number, due date, amount, currency, fee type.</returns>
    [HttpGet("pending-invoices")]
    public async Task<IActionResult> GetPendingInvoices(
        [FromQuery] string? customerTaxId = null,
        [FromQuery] string? customerName = null,
        [FromQuery] int? feeTypeId = null)
    {
        try
        {
            var filter = new PendingInvoicesReportFilter
            {
                CustomerTaxId = customerTaxId,
                CustomerName = customerName,
                FeeTypeId = feeTypeId
            };

            var items = await _billingReportService.GetPendingInvoicesAsync(filter);

            var message = $"Found {items.Count()} pending invoice(s)";
            var response = ResponseStructure<IEnumerable<PendingInvoiceReportDto>>.Success(
                items,
                message);

            return Ok(response);
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? string.Empty;
            var fullMessage = $"Error retrieving pending invoices report: {ex.Message}";
            if (!string.IsNullOrEmpty(innerMessage))
                fullMessage += $" | Inner: {innerMessage}";

            var errorResponse = ResponseStructure<object>.Error(fullMessage, 500);
            return StatusCode(500, errorResponse);
        }
    }
}
