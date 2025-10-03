using System.ComponentModel.DataAnnotations;

namespace ModelLayer.Security;

public class LoginRequest
{
    [Required(ErrorMessage = "El usuario es requerido")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "La clave de seguridad es requerida")]
    public string SecurityKey { get; set; } = string.Empty;

    /// <summary>
    /// Indica si se debe generar una cookie segura (1) o retornar JWT en body (0)
    /// </summary>
    public int IsCookie { get; set; } = 0;
} 