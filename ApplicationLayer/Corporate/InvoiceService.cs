using BusinessLayer.Corporate.Commands;
using BusinessLayer.Corporate.Queries;
using ModelLayer.Corporate.Entities;

namespace ApplicationLayer.Corporate;

/// <summary>
/// Servicio de aplicación para gestionar Invoices
/// </summary>
public class InvoiceService
{
    private readonly GenerateInvoicesCommand _generateInvoicesCommand;
    private readonly MarkInvoiceAsPaidCommand _markInvoiceAsPaidCommand;
    private readonly DeleteInvoicesCommand _deleteInvoicesCommand;
    private readonly InvoiceQueryRepository _invoiceQueryRepository;

    public InvoiceService(
        GenerateInvoicesCommand generateInvoicesCommand,
        MarkInvoiceAsPaidCommand markInvoiceAsPaidCommand,
        DeleteInvoicesCommand deleteInvoicesCommand,
        InvoiceQueryRepository invoiceQueryRepository)
    {
        _generateInvoicesCommand = generateInvoicesCommand;
        _markInvoiceAsPaidCommand = markInvoiceAsPaidCommand;
        _deleteInvoicesCommand = deleteInvoicesCommand;
        _invoiceQueryRepository = invoiceQueryRepository;
    }

    #region Commands

    /// <summary>
    /// Genera facturas automáticamente para un contrato
    /// </summary>
    public async Task<GenerateInvoicesResponse> GenerateInvoicesAsync(GenerateInvoicesRequest request)
    {
        return await _generateInvoicesCommand.ExecuteAsync(request);
    }

    /// <summary>
    /// Marca una factura como pagada
    /// </summary>
    public async Task<MarkInvoiceAsPaidResponse> MarkInvoiceAsPaidAsync(int invoiceId, MarkInvoiceAsPaidRequest request, int userId)
    {
        return await _markInvoiceAsPaidCommand.ExecuteAsync(invoiceId, request, userId);
    }

    /// <summary>
    /// Elimina una factura específica
    /// </summary>
    public async Task<DeleteInvoicesResponse> DeleteSingleInvoiceAsync(int invoiceId)
    {
        return await _deleteInvoicesCommand.DeleteSingleInvoiceAsync(invoiceId);
    }

    /// <summary>
    /// Elimina todas las facturas de un contrato
    /// </summary>
    public async Task<DeleteInvoicesResponse> DeleteAllContractInvoicesAsync(int contractId)
    {
        return await _deleteInvoicesCommand.DeleteAllContractInvoicesAsync(contractId);
    }

    #endregion

    #region Queries

    /// <summary>
    /// Obtiene facturas con filtros opcionales
    /// </summary>
    public async Task<InvoiceQueryResult> GetInvoicesAsync(InvoiceQueryFilter filter)
    {
        return await _invoiceQueryRepository.GetInvoicesAsync(filter);
    }

    /// <summary>
    /// Obtiene una factura por ID con sus items y adjuntos
    /// </summary>
    public async Task<InvoiceDetailDto?> GetInvoiceByIdAsync(int invoiceId)
    {
        return await _invoiceQueryRepository.GetInvoiceByIdAsync(invoiceId);
    }

    /// <summary>
    /// Obtiene el resumen de facturas de un contrato
    /// </summary>
    public async Task<InvoiceSummaryDto> GetInvoiceSummaryByContractAsync(int contractId)
    {
        return await _invoiceQueryRepository.GetInvoiceSummaryByContractAsync(contractId);
    }

    #endregion
}

