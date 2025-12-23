namespace ModelLayer.Corporate.Entities;

/// <summary>
/// Entidad que representa un adjunto de una factura (comprobantes de pago, etc.)
/// </summary>
public class InvoiceAttachment
{
    public int InvoiceAttachmentId { get; set; }
    public int InvoiceId { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public int UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
    
    // Navigation properties
    public virtual Invoice? Invoice { get; set; }
}




