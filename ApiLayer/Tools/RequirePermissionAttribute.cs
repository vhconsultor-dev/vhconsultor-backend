namespace ApiLayer.Tools;

/// <summary>
/// Atributo que declara el permiso requerido para ejecutar un endpoint.
/// Se aplica a nivel de acción (método) o de controlador.
///
/// El permiso se verifica contra los claims "permission" del JWT del usuario.
/// Si el usuario es SuperAdmin (claim "IsSuperAdmin" = "true"), se omite la verificación.
///
/// Uso:
///   [RequirePermission("corporate.customers.create")]
///   public async Task&lt;IActionResult&gt; CreateCustomer(...) { ... }
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequirePermissionAttribute : Attribute
{
    public string PermissionKey { get; }

    public RequirePermissionAttribute(string permissionKey)
    {
        if (string.IsNullOrWhiteSpace(permissionKey))
            throw new ArgumentException("PermissionKey no puede estar vacío.", nameof(permissionKey));

        PermissionKey = permissionKey;
    }
}
