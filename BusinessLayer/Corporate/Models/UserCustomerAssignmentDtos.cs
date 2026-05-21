namespace BusinessLayer.Corporate.Models;

public class UserCustomerAssignmentAmazonAccountDto
{
    public int AmazonAccountId { get; set; }
    public int CustomerId { get; set; }
    public string AmazonAccountIdentifier { get; set; } = string.Empty;
    public bool IsSeller { get; set; }
    public bool IsVendor { get; set; }
    public string AmazonRegion { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UserCustomerAssignmentCustomerDto
{
    public int CustomerId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string NIT { get; set; } = string.Empty;
    public string? CompanyType { get; set; }
    public string? PrimaryEmail { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? City { get; set; }
    public int? CountryId { get; set; }
    public string? ClientStatus { get; set; }
    public bool IsActive { get; set; }
}

public class UserCustomerAssignmentCorporateUserDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class UserCustomerAssignmentDetailDto
{
    public int UserCustomerAssignmentId { get; set; }
    public int UserId { get; set; }
    public int CustomerId { get; set; }
    public bool IsActive { get; set; }
    public DateTime AssignedAt { get; set; }
    public int? AssignedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public UserCustomerAssignmentCorporateUserDto CorporateUser { get; set; } = new();
    public UserCustomerAssignmentCustomerDto Customer { get; set; } = new();
    public List<UserCustomerAssignmentAmazonAccountDto> AmazonAccounts { get; set; } = new();
}

/// <summary>Fila plana para mapeo Dapper (JOIN con cuentas Amazon).</summary>
public class UserCustomerAssignmentFlatRow
{
    public int UserCustomerAssignmentId { get; set; }
    public int UserId { get; set; }
    public int CustomerId { get; set; }
    public bool AssignmentIsActive { get; set; }
    public DateTime AssignedAt { get; set; }
    public int? AssignedBy { get; set; }
    public DateTime? AssignmentUpdatedAt { get; set; }
    public string CorporateUserFirstName { get; set; } = string.Empty;
    public string CorporateUserLastName { get; set; } = string.Empty;
    public string CorporateUserEmail { get; set; } = string.Empty;
    public bool CorporateUserIsActive { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string NIT { get; set; } = string.Empty;
    public string? CompanyType { get; set; }
    public string? CustomerPrimaryEmail { get; set; }
    public string? CustomerPrimaryPhone { get; set; }
    public string? City { get; set; }
    public int? CountryId { get; set; }
    public string? ClientStatus { get; set; }
    public bool CustomerIsActive { get; set; }
    public int? AmazonAccountId { get; set; }
    public string? AmazonAccountIdentifier { get; set; }
    public bool? AmazonIsSeller { get; set; }
    public bool? AmazonIsVendor { get; set; }
    public string? AmazonRegion { get; set; }
    public bool? AmazonAccountIsActive { get; set; }
    public DateTime? AmazonCreatedAt { get; set; }
    public DateTime? AmazonUpdatedAt { get; set; }
}
