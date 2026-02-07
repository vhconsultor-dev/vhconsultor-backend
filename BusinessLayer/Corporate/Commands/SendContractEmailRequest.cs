namespace BusinessLayer.Corporate.Commands;

/// <summary>
/// Request model for sending contract email
/// </summary>
public class SendContractEmailRequest
{
    public string ToEmail { get; set; } = string.Empty;
    public string? CcEmail { get; set; }
    public ContractEmailData Data { get; set; } = new();
}

/// <summary>
/// Contract data for email template
/// </summary>
public class ContractEmailData
{
    public string FullName { get; set; } = string.Empty;
    public string Identification { get; set; } = string.Empty;
    public string ContractNumber { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
}

/// <summary>
/// Response model for sending contract email
/// </summary>
public class SendContractEmailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
