namespace ModelLayer.Security;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public int BranchId { get; set; }
} 