using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ec.edu.monster.controlador;

/// <summary>
/// Si no hay empleado en la sesión, redirige al login.
/// Así nadie entra a /Banco escribiendo la URL directamente.
/// </summary>
public class RequiereSesionAttribute : ActionFilterAttribute
{
    public const string ClaveEmpleado = "Empleado";
    public const string ClaveNombre = "NombreEmpleado";
    /// <summary>Token que devolvió el login; nunca se muestra en las vistas.</summary>
    public const string ClaveToken = "Token";
    /// <summary>Hora (ticks) de la última respuesta del servidor; el token vence 30 minutos después.</summary>
    public const string ClaveActividad = "UltimaActividad";

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (string.IsNullOrEmpty(context.HttpContext.Session.GetString(ClaveEmpleado))
            || string.IsNullOrEmpty(context.HttpContext.Session.GetString(ClaveToken)))
        {
            context.Result = new RedirectToActionResult("Login", "Acceso", null);
        }
    }
}
