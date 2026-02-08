using ApplicationLayer.Corporate;
using ApiLayer.Tools;
using BusinessLayer.Corporate.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ApiLayer.Controllers.Reporting;

/// <summary>
/// Billing reporting: all invoices with flexible filtering (all, pending, overdue, paid, etc.).
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
    /// Gets invoices with flexible filtering options for multiple reporting scenarios.
    /// Supports: all invoices, pending (unpaid), overdue (unpaid and past due date), paid, cancelled, draft, sent.
    /// Optional query parameters: invoice status type, customer tax id, customer name, fee type, contract number, invoice number.
    /// All filters use AND logic when combined. When no filters are sent, all invoices are returned.
    /// </summary>
    /// <param name="invoiceStatusType">Optional. Valid values: 'all', 'pending' (unpaid), 'overdue' (unpaid & past due), 'paid', 'cancelled', 'draft', 'sent'. Default: all.</param>
    /// <param name="customerTaxId">Optional. Customer NIT/tax id; partial match (LIKE).</param>
    /// <param name="customerName">Optional. Customer name (CompanyName); partial match (LIKE).</param>
    /// <param name="feeTypeId">Optional. 1 = Fixed amount, 2 = Percentage. Omit to include all.</param>
    /// <param name="contractNumber">Optional. Contract number; partial match (LIKE).</param>
    /// <param name="invoiceNumber">Optional. Invoice number; partial match (LIKE).</param>
    /// <returns>List of invoice rows: customer, contract, invoice details, dates, amounts, status, fee type.</returns>
    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] string? invoiceStatusType = null,
        [FromQuery] string? customerTaxId = null,
        [FromQuery] string? customerName = null,
        [FromQuery] int? feeTypeId = null,
        [FromQuery] string? contractNumber = null,
        [FromQuery] string? invoiceNumber = null)
    {
        try
        {
            var filter = new InvoiceReportFilter
            {
                InvoiceStatusType = invoiceStatusType,
                CustomerTaxId = customerTaxId,
                CustomerName = customerName,
                FeeTypeId = feeTypeId,
                ContractNumber = contractNumber,
                InvoiceNumber = invoiceNumber
            };

            var items = await _billingReportService.GetInvoicesAsync(filter);

            // Build descriptive message
            var statusLabel = !string.IsNullOrWhiteSpace(invoiceStatusType) 
                ? invoiceStatusType.ToLowerInvariant() 
                : "all";
            var message = $"Found {items.Count()} {statusLabel} invoice(s)";
            
            var response = ResponseStructure<IEnumerable<InvoiceReportDto>>.Success(
                items,
                message);

            return Ok(response);
        }
        catch (ArgumentException argEx)
        {
            // Invalid parameter values (e.g. invalid invoiceStatusType or feeTypeId)
            var errorResponse = ResponseStructure<object>.Error(argEx.Message, 400);
            return BadRequest(errorResponse);
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? string.Empty;
            var fullMessage = $"Error retrieving invoices report: {ex.Message}";
            if (!string.IsNullOrEmpty(innerMessage))
                fullMessage += $" | Inner: {innerMessage}";

            var errorResponse = ResponseStructure<object>.Error(fullMessage, 500);
            return StatusCode(500, errorResponse);
        }
    }
}
