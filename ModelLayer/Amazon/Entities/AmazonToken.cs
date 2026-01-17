namespace ModelLayer.Amazon.Entities;

/// <summary>
/// Entidad para almacenar tokens de acceso de Amazon SP-API
/// </summary>
public class AmazonToken
{
    public int TokenId { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "bearer";
    public int ExpiresIn { get; set; } // En segundos (3600 = 1 hora)
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public string? ClientId { get; set; }
    public string? Notes { get; set; }
}
