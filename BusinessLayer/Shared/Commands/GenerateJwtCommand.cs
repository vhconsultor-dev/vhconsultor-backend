namespace BusinessLayer.Shared.Commands;

public class GenerateJwtCommand
{
    public string EncryptedPayload { get; set; } = string.Empty;

    /// <summary>
    /// Opcional. Cuando se genera el JWT para Brand Partner (verify-2fa), se incluye en el token como claim "UserId".
    /// </summary>
    public int? UserId { get; set; }
}

