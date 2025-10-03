namespace ModelLayer.Security.Entities;

public class User
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? RoleId { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public string? SippUser { get; set; }
} 