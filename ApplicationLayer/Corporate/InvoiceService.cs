using BusinessLayer.Corporate.Commands;
using BusinessLayer.Corporate.Queries;
using ModelLayer.Corporate.Entities;
using Microsoft.AspNetCore.Http;

namespace ApplicationLayer.Corporate;

/// <summary>
/// Servicio de aplicación para gestionar Invoices
/// </summary>
public class InvoiceService
{
    private readonly GenerateInvoicesCommand _generateInvoicesCommand;
    private readonly MarkInvoiceAsPaidCommand _markInvoiceAsPaidCommand;
    private readonly DeleteInvoicesCommand _deleteInvoicesCommand;
    private readonly UploadInvoiceAttachmentCommand _uploadInvoiceAttachmentCommand;
    private readonly DeleteInvoiceAttachmentCommand _deleteInvoiceAttachmentCommand;
    private readonly InvoiceQueryRepository _invoiceQueryRepository;

    public InvoiceService(
        GenerateInvoicesCommand generateInvoicesCommand,
        MarkInvoiceAsPaidCommand markInvoiceAsPaidCommand,
        DeleteInvoicesCommand deleteInvoicesCommand,
        UploadInvoiceAttachmentCommand uploadInvoiceAttachmentCommand,
        DeleteInvoiceAttachmentCommand deleteInvoiceAttachmentCommand,
        InvoiceQueryRepository invoiceQueryRepository)
    {
        _generateInvoicesCommand = generateInvoicesCommand;
        _markInvoiceAsPaidCommand = markInvoiceAsPaidCommand;
        _deleteInvoicesCommand = deleteInvoicesCommand;
        _uploadInvoiceAttachmentCommand = uploadInvoiceAttachmentCommand;
        _deleteInvoiceAttachmentCommand = deleteInvoiceAttachmentCommand;
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

    /// <summary>
    /// Sube un archivo adjunto a una factura
    /// </summary>
    public async Task<UploadInvoiceAttachmentResponse> UploadAttachmentAsync(int invoiceId, IFormFile file, int uploadedBy)
    {
        return await _uploadInvoiceAttachmentCommand.ExecuteAsync(invoiceId, file, uploadedBy);
    }

    /// <summary>
    /// Elimina un adjunto de una factura
    /// </summary>
    public async Task<DeleteInvoiceAttachmentResponse> DeleteAttachmentAsync(int attachmentId)
    {
        return await _deleteInvoiceAttachmentCommand.ExecuteAsync(attachmentId);
    }

    /// <summary>
    /// Elimina todos los adjuntos de una factura
    /// </summary>
    public async Task<DeleteInvoiceAttachmentResponse> DeleteAllAttachmentsForInvoiceAsync(int invoiceId)
    {
        return await _deleteInvoiceAttachmentCommand.DeleteAllAttachmentsForInvoiceAsync(invoiceId);
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

