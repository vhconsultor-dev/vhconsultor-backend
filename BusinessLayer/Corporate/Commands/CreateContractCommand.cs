using ModelLayer;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command para crear un nuevo Contract usando Entity Framework
/// </summary>
public class CreateContractCommand
{
    private readonly DBcontext _context;

    public CreateContractCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ejecuta la creación de un nuevo Contract
    /// </summary>
    /// <param name="request">Datos del contrato</param>
    /// <returns>ID del contrato creado</returns>
    public async Task<int> ExecuteAsync(CreateContractRequest request)
    {
        var contract = new Contract
        {
            // ContractId es auto-generado (IDENTITY), no se asigna
            CustomerId = request.CustomerId,
            ContractNumber = request.ContractNumber,
            ClientLegalName = request.ClientLegalName,
            ClientTaxId = request.ClientTaxId,
            ClientNationality = request.ClientNationality,
            ClientAddress = request.ClientAddress,
            ClientPrimaryContact = request.ClientPrimaryContact,
            ClientEmail = request.ClientEmail,
            ClientPhone = request.ClientPhone,
            ContractTypeId = request.ContractTypeId,
            ServiceDescription = request.ServiceDescription,
            FeeTypeId = request.FeeTypeId,
            FeeAmount = request.FeeAmount,
            FeeDescription = request.FeeDescription,
            CurrencyCode = request.CurrencyCode,
            ContractTerm = request.ContractTerm,
            PaymentFrequency = request.PaymentFrequency,
            PaymentDay = request.PaymentDay,
            PaymentMethodId = request.PaymentMethodId,
            SignedDate = request.SignedDate,
            EffectiveDate = request.EffectiveDate,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            AutoRenewal = request.AutoRenewal,
            RenewalTerm = request.RenewalTerm,
            RenewalNoticeDays = request.RenewalNoticeDays,
            NoticePeriodDays = request.NoticePeriodDays,
            Status = request.Status,
            GoverningLaw = request.GoverningLaw,
            DisputeResolution = request.DisputeResolution,
            ContractualDomicile = request.ContractualDomicile,
            Jurisdiction = request.Jurisdiction,
            Notes = request.Notes,
            DocumentUrl = request.DocumentUrl,
            SignedDocumentUrl = request.SignedDocumentUrl,
            LastModifiedBy = request.LastModifiedBy,
            CreatedAt = DateTimeService.GetCostaRicaNow()
        };

        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();
        
        return contract.ContractId; // Retorna el ID auto-generado
    }
}

/// <summary>
/// Request para crear un Contract
/// </summary>
public class CreateContractRequest
{
    // ContractId es auto-generado (IDENTITY), no se incluye en el request
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
    public int? FeeTypeId { get; set; }
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

