namespace ModelLayer.Security;

public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; }
    public JwtClaims Claims { get; set; } = new JwtClaims();
}

public class JwtClaims
{
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public string BranchId { get; set; } = string.Empty;
} 