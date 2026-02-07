using ModelLayer;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command para actualizar un Contract usando Entity Framework
/// </summary>
public class UpdateContractCommand
{
    private readonly DBcontext _context;

    public UpdateContractCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ejecuta la actualización de un Contract
    /// </summary>
    /// <param name="contractId">ID del contrato a actualizar</param>
    /// <param name="request">Datos actualizados del contrato</param>
    /// <returns>True si se actualizó correctamente, False si no se encontró</returns>
    public async Task<bool> ExecuteAsync(int contractId, UpdateContractRequest request)
    {
        var contract = await _context.Contracts.FindAsync(contractId);
        
        if (contract == null)
            return false;

        // Actualizar propiedades
        contract.CustomerId = request.CustomerId;
        contract.ContractNumber = request.ContractNumber;
        contract.ClientLegalName = request.ClientLegalName;
        contract.ClientTaxId = request.ClientTaxId;
        contract.ClientNationality = request.ClientNationality;
        contract.ClientAddress = request.ClientAddress;
        contract.ClientPrimaryContact = request.ClientPrimaryContact;
        contract.ClientEmail = request.ClientEmail;
        contract.ClientPhone = request.ClientPhone;
        contract.ContractTypeId = request.ContractTypeId;
        contract.ServiceDescription = request.ServiceDescription;
        contract.FeeTypeId = request.FeeTypeId;
        contract.FeeAmount = request.FeeAmount;
        contract.FeeDescription = request.FeeDescription;
        contract.CurrencyCode = request.CurrencyCode;
        contract.ContractTerm = request.ContractTerm;
        contract.PaymentFrequency = request.PaymentFrequency;
        contract.PaymentDay = request.PaymentDay;
        contract.PaymentMethodId = request.PaymentMethodId;
        contract.SignedDate = request.SignedDate;
        contract.EffectiveDate = request.EffectiveDate;
        contract.StartDate = request.StartDate;
        contract.EndDate = request.EndDate;
        contract.AutoRenewal = request.AutoRenewal;
        contract.RenewalTerm = request.RenewalTerm;
        contract.RenewalNoticeDays = request.RenewalNoticeDays;
        contract.NoticePeriodDays = request.NoticePeriodDays;
        contract.Status = request.Status;
        contract.GoverningLaw = request.GoverningLaw;
        contract.DisputeResolution = request.DisputeResolution;
        contract.ContractualDomicile = request.ContractualDomicile;
        contract.Jurisdiction = request.Jurisdiction;
        contract.Notes = request.Notes;
        contract.DocumentUrl = request.DocumentUrl;
        contract.SignedDocumentUrl = request.SignedDocumentUrl;
        contract.LastModifiedBy = request.LastModifiedBy;
        contract.UpdatedAt = DateTimeService.GetCostaRicaNow();

        await _context.SaveChangesAsync();
        
        return true;
    }
}

/// <summary>
/// Request para actualizar un Contract
/// </summary>
public class UpdateContractRequest
{
    public int CustomerId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string? ClientLegalName { get; set; }
    public string? ClientTaxId { get; set; }
    public string? ClientNationality { get; set; }
    public string? ClientAddress { get; set; }
    public string? ClientPrimaryContact { get; set; }
    public string? ClientEmail { get; set; }
    public string? ClientPhone { get; set; }
    public int? ContractTypeId { get; set; }
    public string? ServiceDescription { get; set; }
    public int FeeTypeId { get; set; }
    public decimal? FeeAmount { get; set; }
    public string? FeeDescription { get; set; }
    public string? CurrencyCode { get; set; }
    public string? ContractTerm { get; set; }
    public string? PaymentFrequency { get; set; }
    public int? PaymentDay { get; set; }
    public int? PaymentMethodId { get; set; }
    public DateTime? SignedDate { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? AutoRenewal { get; set; }
    public string? RenewalTerm { get; set; }
    public int? RenewalNoticeDays { get; set; }
    public int? NoticePeriodDays { get; set; }
    public string? Status { get; set; }
    public string? GoverningLaw { get; set; }
    public string? DisputeResolution { get; set; }
    public string? ContractualDomicile { get; set; }
    public string? Jurisdiction { get; set; }
    public string? Notes { get; set; }
    public string? DocumentUrl { get; set; }
    public string? SignedDocumentUrl { get; set; }
    public string? LastModifiedBy { get; set; }
}

