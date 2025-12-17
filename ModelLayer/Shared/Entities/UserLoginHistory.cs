namespace ModelLayer.Shared.Entities;

/// <summary>
/// Entidad UserLoginHistory - Tabla [Global].[UserLoginHistory]
/// </summary>
public class UserLoginHistory
{
    public int LoginHistoryId { get; set; }
    public int UserId { get; set; }
    public DateTime LoginDate { get; set; }
    public string IPAddress { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceType { get; set; }
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
    public bool LoginSuccessful { get; set; }
    public string? FailureReason { get; set; }
    public string? SessionId { get; set; }
}

