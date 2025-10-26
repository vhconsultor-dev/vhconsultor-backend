using ModelLayer;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command para crear un nuevo Customer usando Entity Framework
/// </summary>
public class CreateCustomerCommand
{
    private readonly DBcontext _context;

    public CreateCustomerCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ejecuta la creación de un nuevo Customer
    /// </summary>
    /// <param name="request">Datos del customer</param>
    /// <returns>ID del customer creado</returns>
    public async Task<int> ExecuteAsync(CreateCustomerRequest request)
    {
        var customer = new Customer
        {
            CompanyName = request.CompanyName,
            NIT = request.NIT,
            CompanyType = request.CompanyType,
            PrimaryEmail = request.PrimaryEmail,
            SecondaryEmail = request.SecondaryEmail,
            BillingEmail = request.BillingEmail,
            PrimaryPhone = request.PrimaryPhone,
            SecondaryPhone = request.SecondaryPhone,
            EmergencyPhone = request.EmergencyPhone,
            Contact1Name = request.Contact1Name,
            Contact1Phone = request.Contact1Phone,
            Contact2Name = request.Contact2Name,
            Contact2Phone = request.Contact2Phone,
            Contact3Name = request.Contact3Name,
            Contact3Phone = request.Contact3Phone,
            CountryId = request.CountryId,
            State = request.State,
            City = request.City,
            Address = request.Address,
            PostalCode = request.PostalCode,
            SectorId = request.SectorId,
            CompanySize = request.CompanySize,
            AnnualRevenue = request.AnnualRevenue,
            Website = request.Website,
            ClientStatus = request.ClientStatus,
            Priority = request.Priority,
            Source = request.Source,
            Notes = request.Notes,
            IsActive = true,
            CreatedAt = DateTimeService.GetCostaRicaNow()
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        
        return customer.CustomerId;
    }
}

/// <summary>
/// Request para crear un Customer
/// </summary>
public class CreateCustomerRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string NIT { get; set; } = string.Empty;
    public string? CompanyType { get; set; }
    public string? PrimaryEmail { get; set; }
    public string? SecondaryEmail { get; set; }
    public string? BillingEmail { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? SecondaryPhone { get; set; }
    public string? EmergencyPhone { get; set; }
    public string? Contact1Name { get; set; }
    public string? Contact1Phone { get; set; }
    public string? Contact2Name { get; set; }
    public string? Contact2Phone { get; set; }
    public string? Contact3Name { get; set; }
    public string? Contact3Phone { get; set; }
    public int? CountryId { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public int? SectorId { get; set; }
    public string? CompanySize { get; set; }
    public decimal? AnnualRevenue { get; set; }
    public string? Website { get; set; }
    public string? ClientStatus { get; set; }
    public string? Priority { get; set; }
    public string? Source { get; set; }
    public string? Notes { get; set; }
}

