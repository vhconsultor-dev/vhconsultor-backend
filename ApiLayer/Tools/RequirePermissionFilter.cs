using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ApiLayer.Tools;

/// <summary>
/// ActionFilter global que intercepta cada request y verifica que el usuario
/// tenga el permiso declarado en [RequirePermission] antes de ejecutar la acción.
///
/// Flujo:
///   1. Busca el atributo [RequirePermission] en la acción o el controlador.
///   2. Si no hay atributo, deja pasar (compatible con endpoints que solo usan [Authorize]).
///   3. Si hay atributo, busca en los claims del JWT:
///      a. Claim "IsSuperAdmin" = "true" → acceso sin restricción.
///      b. Claim "permission" que coincida con el PermissionKey → acceso permitido.
///      c. Si no hay coincidencia → 403 Forbidden con mensaje descriptivo.
///
/// El claim "permission" se emite en el JWT durante el login (ver JwtService).
/// Un usuario puede tener múltiples claims "permission" en su token.
/// </summary>
public class RequirePermissionFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Buscar el atributo en la acción concreta primero, luego en el controlador
        var attribute = context.ActionDescriptor.EndpointMetadata
            .OfType<RequirePermissionAttribute>()
            .FirstOrDefault();

        // Sin atributo: el endpoint solo usa [Authorize] genérico, dejar pasar
        if (attribute is null) return;

        var user = context.HttpContext.User;

        // Usuario no autenticado — [Authorize] ya maneja esto con 401, pero por si acaso
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // SuperAdmin: acceso total sin verificar permisos
        var isSuperAdmin = user.Claims
            .FirstOrDefault(c => c.Type == "IsSuperAdmin")?.Value;
        if (isSuperAdmin == "true") return;

        // Verificar que el usuario tenga el permiso requerido en sus claims
        var hasPermission = user.Claims
            .Where(c => c.Type == "permission")
            .Any(c => c.Value == attribute.PermissionKey);

        if (!hasPermission)
        {
            context.Result = new ObjectResult(new
            {
                success = false,
                message = $"Acceso denegado. Se requiere el permiso: {attribute.PermissionKey}",
                requiredPermission = attribute.PermissionKey
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
