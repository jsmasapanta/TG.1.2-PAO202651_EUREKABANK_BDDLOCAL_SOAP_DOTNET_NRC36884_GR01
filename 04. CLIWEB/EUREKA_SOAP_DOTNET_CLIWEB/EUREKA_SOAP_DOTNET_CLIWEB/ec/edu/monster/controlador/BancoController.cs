using System.Text.Json;
using ec.edu.monster.modelo;
using ec.edu.monster.servicio;
using ec.edu.monster.vista;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;

namespace ec.edu.monster.controlador;

/// <summary>Consulta de movimientos y registro de depósitos, retiros y transferencias.</summary>
[RequiereSesion]
public class BancoController : Controller
{
    private const string ClaveSesionOperaciones = "Operaciones";
    private const int MaximoSesion = 10;

    private readonly ClienteEureka _cliente;

    public BancoController(ClienteEureka cliente)
    {
        _cliente = cliente;
    }

    // ---------- Movimientos ----------

    /// <summary>GET /Banco/Movimientos?cuenta=00100001 (sin cuenta muestra solo el formulario).</summary>
    [HttpGet]
    public async Task<IActionResult> Movimientos(string? cuenta)
    {
        PrepararVista("Movimientos");
        var modelo = new MovimientosViewModel { Cuenta = cuenta?.Trim() };
        if (string.IsNullOrWhiteSpace(cuenta))
        {
            return View(modelo);
        }

        try
        {
            modelo.Datos = await _cliente.ConsultarCuentaAsync(Token, modelo.Cuenta!);
            modelo.Movimientos = await _cliente.TraerMovimientosAsync(Token, modelo.Cuenta!);
            AnotarActividad();
        }
        catch (ErrorServicioException ex)
        {
            modelo.Error = ex.Message;
        }
        return View(modelo);
    }

    // ---------- Operaciones ----------

    [HttpGet]
    public IActionResult Deposito() => MostrarOperacion(TipoOperacion.Deposito);

    [HttpGet]
    public IActionResult Retiro() => MostrarOperacion(TipoOperacion.Retiro);

    [HttpGet]
    public IActionResult Transferencia() => MostrarOperacion(TipoOperacion.Transferencia);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(OperacionViewModel entrada)
    {
        PrepararVista(entrada.Tipo.ToString());
        var modelo = new OperacionViewModel
        {
            Tipo = entrada.Tipo,
            Cuenta = entrada.Cuenta?.Trim(),
            CuentaDestino = entrada.CuentaDestino?.Trim(),
            Importe = entrada.Importe,
            Sesion = LeerOperacionesSesion()
        };
        bool esTransferencia = modelo.Tipo == TipoOperacion.Transferencia;

        // El servicio recibe un decimal: solo se convierte lo escrito. Las reglas (cuenta, importe,
        // cuentas distintas, saldo) las valida el servidor y su mensaje se muestra.
        if (!LeerImporte(modelo.Importe, out decimal importe))
        {
            modelo.Error = "Escriba el importe con números (ejemplo: 150 o 75.50).";
            return View("Operacion", modelo);
        }

        string empleado = HttpContext.Session.GetString(RequiereSesionAttribute.ClaveEmpleado)!;
        string cuenta = modelo.Cuenta ?? "";
        string titulo = Formato.Titulo(modelo.Tipo);
        try
        {
            decimal saldo = modelo.Tipo switch
            {
                TipoOperacion.Deposito => await _cliente.RegistrarDepositoAsync(Token, cuenta, importe, empleado),
                TipoOperacion.Retiro => await _cliente.RegistrarRetiroAsync(Token, cuenta, importe, empleado),
                _ => await _cliente.RegistrarTransferenciaAsync(Token, cuenta, modelo.CuentaDestino ?? "", importe, empleado)
            };

            AnotarActividad();
            modelo.Comprobante = await ArmarComprobanteAsync(modelo, importe, saldo, empleado);
            modelo.Exito = $"{titulo} registrad{(esTransferencia ? "a" : "o")}";
            modelo.Detalle = esTransferencia
                ? $"{Formato.Dinero(importe)} de {modelo.Cuenta} a {modelo.CuentaDestino}. Nuevo saldo de {modelo.Cuenta}: {Formato.Dinero(saldo)}"
                : $"{Formato.Dinero(importe)} en la cuenta {modelo.Cuenta}. Nuevo saldo: {Formato.Dinero(saldo)}";

            modelo.Sesion.Insert(0, $"{DateTime.Now:HH:mm:ss} · {titulo} · {modelo.Cuenta}" +
                                    (esTransferencia ? $" → {modelo.CuentaDestino}" : "") +
                                    $" · {Formato.Dinero(importe)} (saldo {Formato.Dinero(saldo)})");
            while (modelo.Sesion.Count > MaximoSesion) modelo.Sesion.RemoveAt(modelo.Sesion.Count - 1);
            HttpContext.Session.SetString(ClaveSesionOperaciones, JsonSerializer.Serialize(modelo.Sesion));
            modelo.Importe = null;
        }
        catch (ErrorServicioException ex)
        {
            modelo.Error = ex.Message;
        }
        return View("Operacion", modelo);
    }

    /// <summary>
    /// Datos del recibo. El número de movimiento, el titular y la moneda se piden al servidor con consultarCuenta
    /// (el movimiento recién grabado es el último de la cuenta); si esa consulta falla, el recibo sale sin ellos.
    /// </summary>
    private async Task<Comprobante> ArmarComprobanteAsync(OperacionViewModel modelo, decimal importe, decimal saldo, string empleado)
    {
        var recibo = new Comprobante
        {
            Tipo = modelo.Tipo,
            Cuenta = modelo.Cuenta ?? "",
            CuentaDestino = modelo.CuentaDestino,
            Importe = importe,
            NuevoSaldo = saldo,
            Fecha = DateTime.Now,
            Empleado = $"{empleado} · {HttpContext.Session.GetString(RequiereSesionAttribute.ClaveNombre)}"
        };
        try
        {
            Cuenta cuenta = await _cliente.ConsultarCuentaAsync(Token, recibo.Cuenta);
            recibo.NumeroMovimiento = cuenta.NumeroMovimientos;
            recibo.Cliente = cuenta.Cliente;
            recibo.Moneda = cuenta.Moneda;
        }
        catch (ErrorServicioException)
        {
            // El recibo igual muestra importe y saldo.
        }
        return recibo;
    }

    private void AnotarActividad() =>
        HttpContext.Session.SetString(RequiereSesionAttribute.ClaveActividad, DateTime.Now.Ticks.ToString());

    /// <summary>Token de la sesión (lo pone AccesoController al iniciar sesión).</summary>
    private string Token => HttpContext.Session.GetString(RequiereSesionAttribute.ClaveToken) ?? "";

    /// <summary>Convierte lo escrito (punto o coma decimal) al decimal que recibe el servicio.</summary>
    private static bool LeerImporte(string? texto, out decimal importe) =>
        decimal.TryParse((texto ?? "").Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out importe);

    private IActionResult MostrarOperacion(TipoOperacion tipo)
    {
        PrepararVista(tipo.ToString());
        return View("Operacion", new OperacionViewModel { Tipo = tipo, Sesion = LeerOperacionesSesion() });
    }

    private IList<string> LeerOperacionesSesion()
    {
        string? json = HttpContext.Session.GetString(ClaveSesionOperaciones);
        return json == null ? new List<string>() : JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
    }

    /// <summary>Datos que usa el menú lateral del layout.</summary>
    private void PrepararVista(string opcionActiva)
    {
        ViewData["Opcion"] = opcionActiva;
        ViewData["Empleado"] = HttpContext.Session.GetString(RequiereSesionAttribute.ClaveEmpleado);
        ViewData["Nombre"] = HttpContext.Session.GetString(RequiereSesionAttribute.ClaveNombre);
        ViewData["Servidor"] = _cliente.UrlServidor;
    }
}
