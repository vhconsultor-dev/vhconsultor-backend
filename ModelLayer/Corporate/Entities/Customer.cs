namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Entidad que representa un cliente corporativo
/// </summary>
public class Customer
{
    public int CustomerId { get; set; }
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
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastContactDate { get; set; }
}

