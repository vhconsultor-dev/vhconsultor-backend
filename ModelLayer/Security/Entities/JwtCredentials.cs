namespace ModelLayer.Security.Entities;

public class JwtCredentials
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Application { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? SecureKey { get; set; }
    public string Profile { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
} 