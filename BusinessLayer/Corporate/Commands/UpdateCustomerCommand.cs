using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Corporate.Entities;
using ModelLayer.Shared;

namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Command para actualizar un Customer usando Entity Framework
/// </summary>
public class UpdateCustomerCommand
{
    private readonly DBcontext _context;

    public UpdateCustomerCommand(DBcontext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ejecuta la actualización de un Customer
    /// </summary>
    /// <param name="customerId">ID del customer a actualizar</param>
    /// <param name="request">Datos actualizados del customer</param>
    /// <returns>True si se actualizó correctamente, False si no se encontró</returns>
    public async Task<bool> ExecuteAsync(int customerId, UpdateCustomerRequest request)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        
        if (customer == null)
            return false;

        // Actualizar propiedades
        customer.CompanyName = request.CompanyName;
        customer.NIT = request.NIT;
        customer.CompanyType = request.CompanyType;
        customer.PrimaryEmail = request.PrimaryEmail;
        customer.SecondaryEmail = request.SecondaryEmail;
        customer.BillingEmail = request.BillingEmail;
        customer.PrimaryPhone = request.PrimaryPhone;
        customer.SecondaryPhone = request.SecondaryPhone;
        customer.EmergencyPhone = request.EmergencyPhone;
        customer.Contact1Name = request.Contact1Name;
        customer.Contact1Phone = request.Contact1Phone;
        customer.Contact2Name = request.Contact2Name;
        customer.Contact2Phone = request.Contact2Phone;
        customer.Contact3Name = request.Contact3Name;
        customer.Contact3Phone = request.Contact3Phone;
        customer.CountryId = request.CountryId;
        customer.State = request.State;
        customer.City = request.City;
        customer.Address = request.Address;
        customer.PostalCode = request.PostalCode;
        customer.SectorId = request.SectorId;
        customer.CompanySize = request.CompanySize;
        customer.AnnualRevenue = request.AnnualRevenue;
        customer.Website = request.Website;
        customer.ClientStatus = request.ClientStatus;
        customer.Priority = request.Priority;
        customer.Source = request.Source;
        customer.Notes = request.Notes;
        customer.UpdatedAt = DateTimeService.GetCostaRicaNow();

        await _context.SaveChangesAsync();
        
        return true;
    }
}

/// <summary>
/// Request para actualizar un Customer
/// </summary>
public class UpdateCustomerRequest
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

