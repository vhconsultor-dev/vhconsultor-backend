using ModelLayer;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;
using Microsoft.EntityFrameworkCore;

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
        // Generar ContractNumber automáticamente si no se proporciona
        string contractNumber;
        if (string.IsNullOrWhiteSpace(request.ContractNumber))
        {
            // Generar automáticamente
            contractNumber = await GenerateContractNumberAsync();
        }
        else
        {
            // Usar el proporcionado, pero verificar que sea único
            bool exists = await _context.Contracts
                .AnyAsync(c => c.ContractNumber == request.ContractNumber);
            
            if (exists)
            {
                // Si el número proporcionado ya existe, generar uno nuevo automáticamente
                contractNumber = await GenerateContractNumberAsync();
            }
            else
            {
                contractNumber = request.ContractNumber;
            }
        }

        var contract = new Contract
        {
            // ContractId es auto-generado (IDENTITY), no se asigna
            CustomerId = request.CustomerId,
            ContractNumber = contractNumber,
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

    /// <summary>
    /// Genera un número de contrato único en formato CON-{YEAR}-{CONSECUTIVE}
    /// </summary>
    private async Task<string> GenerateContractNumberAsync()
    {
        var year = DateTime.Now.Year;
        var lastContract = await _context.Contracts
            .Where(c => c.ContractNumber.StartsWith($"CON-{year}-"))
            .OrderByDescending(c => c.ContractNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastContract != null)
        {
            var parts = lastContract.ContractNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"CON-{year}-{nextNumber:D3}";
    }
}

/// <summary>
/// Request para crear un Contract
/// </summary>
public class CreateContractRequest
{
    // ContractId es auto-generado (IDENTITY), no se incluye en el request
    public int CustomerId { get; set; }
    // ContractNumber es opcional: si no se proporciona, se genera automáticamente
    public string? ContractNumber { get; set; }
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

