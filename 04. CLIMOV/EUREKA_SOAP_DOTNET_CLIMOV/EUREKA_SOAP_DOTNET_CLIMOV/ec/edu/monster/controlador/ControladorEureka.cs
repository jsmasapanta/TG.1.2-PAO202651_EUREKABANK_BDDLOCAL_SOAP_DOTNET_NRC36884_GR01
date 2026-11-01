using ec.edu.monster.modelo;
using ec.edu.monster.servicio;

namespace ec.edu.monster.controlador;

/// <summary>
/// Intermediario entre las pantallas (vista) y el servicio SOAP: solo pasa las llamadas.
/// Guarda el empleado de la sesión y la lista de operaciones que muestra el menú.
/// Las reglas del banco (intentos de login, cuenta, importe, saldo) las valida el servidor
/// y su mensaje llega como ErrorServicioException.
/// Patrón Singleton: todas las pantallas comparten ControladorEureka.Instancia.
/// </summary>
public sealed class ControladorEureka
{
    private const int MaximoSesion = 15;

    private static readonly Lazy<ControladorEureka> _instancia =
        new(() => new ControladorEureka(ClienteSoap.Instancia));

    public static ControladorEureka Instancia => _instancia.Value;

    private readonly ClienteSoap _cliente;
    private readonly List<string> _operacionesSesion = new();

    private ControladorEureka(ClienteSoap cliente)
    {
        _cliente = cliente;
    }

    public Empleado? Empleado { get; private set; }

    public string UrlServidor => _cliente.Url;

    public string HostServidor => Uri.TryCreate(UrlServidor, UriKind.Absolute, out Uri? u) ? u.Host : "";

    public string PuertoServidor => Uri.TryCreate(UrlServidor, UriKind.Absolute, out Uri? u) ? u.Port.ToString() : "";

    /// <summary>
    /// Cambia el servidor (la IP de la PC con IIS varía según la red). Valida host y puerto
    /// (InvalidOperationException si no sirven) y lo guarda en el teléfono.
    /// </summary>
    public void CambiarServidor(string host, string puerto)
    {
        _cliente.Url = ConfiguracionServidor.ArmarUrl(host, puerto, "WSEureka.svc");
        Empleado = null;
    }

    public void RestablecerServidor()
    {
        _cliente.RestablecerServidor();
        Empleado = null;
    }

    public IReadOnlyList<string> OperacionesSesion => _operacionesSesion;

    /// <summary>Minutos que le quedan al token de la sesión (null si no hay sesión).</summary>
    public int? MinutosSesion => _cliente.MinutosSesion;

    /// <summary>Para el indicador "Servidor activo".</summary>
    public Task<bool> ServidorActivoAsync() => _cliente.ServidorActivoAsync();

    public async Task<Empleado> IniciarSesionAsync(string usuario, string clave)
    {
        Empleado = await _cliente.LoginAsync(usuario, clave);
        _operacionesSesion.Clear();
        return Empleado;
    }

    /// <summary>Avisa al servidor para que invalide el token.</summary>
    public async Task CerrarSesionAsync()
    {
        await _cliente.CerrarSesionAsync();
        Empleado = null;
        _operacionesSesion.Clear();
    }

    public Task<Cuenta> ConsultarCuentaAsync(string cuenta) => _cliente.ConsultarCuentaAsync(cuenta);

    public Task<Movimiento[]> TraerMovimientosAsync(string cuenta) => _cliente.TraerMovimientosAsync(cuenta);

    public Task<decimal> RegistrarDepositoAsync(string cuenta, decimal importe) =>
        _cliente.RegistrarDepositoAsync(cuenta, importe, CodigoEmpleado());

    public Task<decimal> RegistrarRetiroAsync(string cuenta, decimal importe) =>
        _cliente.RegistrarRetiroAsync(cuenta, importe, CodigoEmpleado());

    public Task<decimal> RegistrarTransferenciaAsync(string origen, string destino, decimal importe) =>
        _cliente.RegistrarTransferenciaAsync(origen, destino, importe, CodigoEmpleado());

    /// <summary>Anota una línea (ya formateada por la vista) en la lista de la sesión.</summary>
    public void AnotarOperacion(string linea)
    {
        _operacionesSesion.Insert(0, linea);
        if (_operacionesSesion.Count > MaximoSesion)
        {
            _operacionesSesion.RemoveAt(_operacionesSesion.Count - 1);
        }
    }

    private string CodigoEmpleado() =>
        Empleado?.Codigo ?? throw new ErrorServicioException("La sesión terminó. Vuelva a iniciar sesión.");
}
