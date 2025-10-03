namespace ModelLayer.Shared.Entities;

public class ErrorLog
{
    public int Id { get; set; }
    public string ErrorNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? Source { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }
    public string? UserAgent { get; set; }
    public string? UserId { get; set; }
    public string? RequestBody { get; set; }
    public string? QueryString { get; set; }
    public string? ExceptionType { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Environment { get; set; }
    public string? AdditionalData { get; set; }
} 