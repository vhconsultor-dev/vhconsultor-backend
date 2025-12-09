using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
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
    public async Task<bool> ExecuteAsync(int contractId, UpdateContractRequest request) // Ahora es int
    {
        try
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
        catch (DbUpdateException ex)
        {
            var innerException = ex.InnerException as SqlException;
            
            if (innerException != null)
            {
                // Error 547: Violación de constraint FOREIGN KEY
                if (innerException.Number == 547)
                {
                    var errorMessage = innerException.Message.ToLower();
                    if (errorMessage.Contains("customerid") || errorMessage.Contains("customer"))
                    {
                        throw new ArgumentException($"El cliente con ID {request.CustomerId} no existe en la base de datos");
                    }
                    if (errorMessage.Contains("contracttypeid") || errorMessage.Contains("contracttype"))
                    {
                        throw new ArgumentException($"El tipo de contrato con ID {request.ContractTypeId} no existe en la base de datos");
                    }
                    if (errorMessage.Contains("feetypeid") || errorMessage.Contains("feetype"))
                    {
                        throw new ArgumentException($"El tipo de tarifa con ID {request.FeeTypeId} no existe en la base de datos");
                    }
                    if (errorMessage.Contains("paymentmethodid") || errorMessage.Contains("paymentmethod"))
                    {
                        throw new ArgumentException($"El método de pago con ID {request.PaymentMethodId} no existe en la base de datos");
                    }
                    
                    throw new ArgumentException($"Error de integridad referencial: Uno de los IDs proporcionados no existe en la base de datos. {innerException.Message}");
                }
                
                // Error 2627: Violación de constraint UNIQUE
                if (innerException.Number == 2627)
                {
                    var errorMessage = innerException.Message.ToLower();
                    if (errorMessage.Contains("contractnumber"))
                    {
                        throw new InvalidOperationException($"El número de contrato '{request.ContractNumber}' ya existe en la base de datos");
                    }
                    
                    throw new InvalidOperationException($"Error de duplicación: El valor ya existe en la base de datos. {innerException.Message}");
                }
                
                // Error 515: Cannot insert NULL en columna que no permite NULL
                if (innerException.Number == 515)
                {
                    throw new InvalidOperationException($"Error de validación: Campo requerido faltante. {innerException.Message}");
                }
                
                // Error 8152: String o datos binarios se truncarían
                if (innerException.Number == 8152 || innerException.Number == 2628)
                {
                    throw new ArgumentException($"Error de longitud: Uno o más campos exceden la longitud máxima permitida. {innerException.Message}");
                }
                
                // Otros errores de SQL Server
                throw new InvalidOperationException(
                    $"Error de base de datos al actualizar el contrato: {innerException.Message}", ex);
            }
            
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error inesperado al actualizar el contrato: {ex.Message}", ex);
        }
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

