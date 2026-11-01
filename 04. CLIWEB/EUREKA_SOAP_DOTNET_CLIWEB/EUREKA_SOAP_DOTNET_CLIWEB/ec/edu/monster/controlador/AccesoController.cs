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
        PrepararServidor();
        return View(new LoginViewModel());
    }

    /// <summary>Host y puerto actuales para el formulario "Servidor" del login.</summary>
    private void PrepararServidor()
    {
        ViewData["Host"] = _servicio.HostServidor;
        ViewData["Puerto"] = _servicio.PuertoServidor;
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
            HttpContext.Session.SetString(RequiereSesionAttribute.ClaveActividad, DateTime.Now.Ticks.ToString());
            return RedirectToAction("Movimientos", "Banco");
        }
        catch (ErrorServicioException ex)
        {
            PrepararServidor();
            return View(new LoginViewModel { Usuario = entrada.Usuario, Error = ex.Message });
        }
    }

    /// <summary>
    /// POST /Acceso/CambiarServidor: la IP del servidor varía según la red (casa, aula, guardia);
    /// se cambia desde el login sin tocar archivos. El servicio valida host y puerto.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CambiarServidor(string? host, string? puerto)
    {
        string? error = _servicio.CambiarServidor(host ?? "", puerto ?? "");
        HttpContext.Session.Clear(); // el token del servidor anterior ya no sirve
        TempData["Servidor"] = error ?? "Servidor cambiado a " + _servicio.UrlServidor;
        TempData["ServidorError"] = error != null;
        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// GET /Acceso/Estado: lo consulta la página cada 10 segundos para el indicador "Servidor activo"
    /// y los minutos que le quedan a la sesión (el token dura 30 minutos desde la última operación).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Estado()
    {
        int? minutos = null;
        if (long.TryParse(HttpContext.Session.GetString(RequiereSesionAttribute.ClaveActividad), out long ticks))
        {
            minutos = Math.Max(0, 30 - (int)(DateTime.Now - new DateTime(ticks)).TotalMinutes);
        }
        return Json(new { activo = await _servicio.ServidorActivoAsync(), servidor = _servicio.UrlServidor, minutos });
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
