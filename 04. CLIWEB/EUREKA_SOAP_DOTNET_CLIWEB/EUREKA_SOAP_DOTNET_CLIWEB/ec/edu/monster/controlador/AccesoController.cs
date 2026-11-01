using ec.edu.monster.modelo;
using ec.edu.monster.servicio;
using Microsoft.AspNetCore.Mvc;

namespace ec.edu.monster.controlador;

/// <summary>
/// Inicio y cierre de sesión. Solo pasa usuario y clave al servicio: el servidor valida,
/// cuenta los intentos fallidos y bloquea; aquí se muestra su mensaje.
/// </summary>
public class AccesoController : Controller
{
    private readonly ClienteEureka _servicio;

    public AccesoController(ClienteEureka servicio)
    {
        _servicio = servicio;
    }

    /// <summary>La raíz del sitio (/) lleva al login.</summary>
    [HttpGet]
    public IActionResult Index() => RedirectToAction(nameof(Login));

    [HttpGet]
    public IActionResult Login()
    {
        if (!string.IsNullOrEmpty(HttpContext.Session.GetString(RequiereSesionAttribute.ClaveEmpleado)))
        {
            return RedirectToAction("Movimientos", "Banco");
        }
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel entrada)
    {
        try
        {
            Empleado empleado = await _servicio.LoginAsync(entrada.Usuario ?? "", entrada.Clave ?? "");
            HttpContext.Session.SetString(RequiereSesionAttribute.ClaveEmpleado, empleado.Codigo);
            HttpContext.Session.SetString(RequiereSesionAttribute.ClaveNombre, empleado.Nombre);
            HttpContext.Session.SetString(RequiereSesionAttribute.ClaveToken, empleado.Token);
            return RedirectToAction("Movimientos", "Banco");
        }
        catch (ErrorServicioException ex)
        {
            return View(new LoginViewModel { Usuario = entrada.Usuario, Error = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        string? token = HttpContext.Session.GetString(RequiereSesionAttribute.ClaveToken);
        HttpContext.Session.Clear();
        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                await _servicio.CerrarSesionAsync(token);
            }
            catch (ErrorServicioException)
            {
                // Si el servidor no responde, el token vence solo a los 30 minutos.
            }
        }
        return RedirectToAction(nameof(Login));
    }
}
