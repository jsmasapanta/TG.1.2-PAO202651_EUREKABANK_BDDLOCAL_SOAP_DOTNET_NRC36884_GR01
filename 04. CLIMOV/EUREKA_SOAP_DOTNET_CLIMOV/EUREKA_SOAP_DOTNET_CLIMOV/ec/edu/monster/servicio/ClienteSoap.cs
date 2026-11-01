using System.Globalization;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Xml.Linq;
using ec.edu.monster.modelo;

namespace ec.edu.monster.servicio;

/// <summary>Error que la vista puede mostrar tal cual al usuario (normalmente el mensaje que envió el servidor).</summary>
public class ErrorServicioException : Exception
{
    public ErrorServicioException(string mensaje) : base(mensaje) { }
}

/// <summary>
/// Llama a las operaciones del servicio SOAP con HttpClient: arma el sobre (valores escapados con
/// SecurityElement.Escape y el token en &lt;soap:Header&gt;), lo envía por POST con SOAPAction y el header
/// X-Cliente, y copia la respuesta en los modelos (get/set). Si llega un SOAP Fault, entrega el
/// &lt;Mensaje&gt; de ErrorServicio. No tiene reglas del banco: todas las valida el servidor.
/// Patrón Singleton: toda la app usa ClienteSoap.Instancia (un solo HttpClient).
/// </summary>
public sealed class ClienteSoap
{
    public const string Namespace = "http://espe.edu.ec/eureka/";
    private const string Contrato = "WSEureka";

    /// <summary>MOVIL en Android; ESCRITORIO si la app corre en Windows.</summary>
    public static readonly string NombreCliente =
        DeviceInfo.Current.Platform == DevicePlatform.WinUI ? "ESCRITORIO" : "MOVIL";

    private static readonly Lazy<ClienteSoap> _instancia = new(() => new ClienteSoap());

    public static ClienteSoap Instancia => _instancia.Value;

    private readonly HttpClient _http;

    /// <summary>Token de la sesión; nunca se muestra en pantalla.</summary>
    private string? _token;

    /// <summary>Última respuesta correcta del servidor: el token vence 30 minutos después.</summary>
    private DateTime? _ultimaActividad;

    /// <summary>Minutos que le quedan a la sesión (null si no hay sesión).</summary>
    public int? MinutosSesion =>
        _token == null || _ultimaActividad == null
            ? null
            : Math.Max(0, 30 - (int)(DateTime.Now - _ultimaActividad.Value).TotalMinutes);

    /// <summary>true si el servidor responde (pide el WSDL con 3 segundos de espera). Para el indicador "Servidor activo".</summary>
    public async Task<bool> ServidorActivoAsync()
    {
        if (!Uri.TryCreate(Url, UriKind.Absolute, out Uri? destino))
        {
            return false;
        }
        try
        {
            using var espera = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using HttpResponseMessage r = await _http.GetAsync(destino + "?wsdl", espera.Token);
            return r.IsSuccessStatusCode;
        }
        catch (Exception e) when (e is HttpRequestException || e is TaskCanceledException || e is SocketException || e is IOException)
        {
            return false;
        }
    }

    // Constructor privado: solo la propia clase crea la instancia.
    private ClienteSoap()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
    }

    private const string ClavePreferencia = "servidor_eureka";

    /// <summary>
    /// URL del servicio. Por defecto la de appsettings.json (incrustado en la app); si en el login se cambia
    /// la IP o el puerto, la nueva se guarda en el teléfono y se usa hasta "Restablecer".
    /// </summary>
    public string Url
    {
        get => Preferences.Default.Get(ClavePreferencia, ConfiguracionServidor.Url);
        set => Preferences.Default.Set(ClavePreferencia, value);
    }

    /// <summary>Vuelve a la dirección de appsettings.json.</summary>
    public void RestablecerServidor()
    {
        Preferences.Default.Remove(ClavePreferencia);
        _token = null;
        _ultimaActividad = null;
    }

    public async Task<Empleado> LoginAsync(string usuario, string clave)
    {
        _token = null;
        XElement r = await InvocarAsync("login", ("usuario", usuario), ("clave", clave));
        var empleado = new Empleado
        {
            Codigo = Texto(r, "Codigo"),
            Usuario = Texto(r, "Usuario"),
            Nombre = Texto(r, "Nombre"),
            Token = Texto(r, "Token")
        };
        _token = empleado.Token;
        _ultimaActividad = DateTime.Now;
        return empleado;
    }

    /// <summary>Invalida el token en el servidor. Si no se puede avisar, igual se olvida aquí.</summary>
    public async Task CerrarSesionAsync()
    {
        string? token = _token;
        _token = null;
        _ultimaActividad = null;
        if (string.IsNullOrEmpty(token))
        {
            return;
        }
        try
        {
            await InvocarAsync("cerrarSesion", ("token", token));
        }
        catch (ErrorServicioException)
        {
            // El token vence solo a los 30 minutos.
        }
    }

    public async Task<Cuenta> ConsultarCuentaAsync(string cuenta)
    {
        XElement r = await InvocarAsync("consultarCuenta", ("cuenta", cuenta));
        return new Cuenta
        {
            Codigo = Texto(r, "Codigo"),
            Cliente = Texto(r, "Cliente"),
            Moneda = Texto(r, "Moneda"),
            Sucursal = Texto(r, "Sucursal"),
            Saldo = Decimal(Texto(r, "Saldo")),
            Estado = Texto(r, "Estado"),
            FechaCreacion = DateTime.Parse(Texto(r, "FechaCreacion"), CultureInfo.InvariantCulture),
            NumeroMovimientos = int.Parse(Texto(r, "NumeroMovimientos"), CultureInfo.InvariantCulture)
        };
    }

    public async Task<Movimiento[]> TraerMovimientosAsync(string cuenta)
    {
        XElement r = await InvocarAsync("traerMovimientos", ("cuenta", cuenta));
        return r.Elements().Where(e => e.Name.LocalName == "Movimiento").Select(m => new Movimiento
        {
            Numero = int.Parse(Texto(m, "Numero"), CultureInfo.InvariantCulture),
            Fecha = DateTime.Parse(Texto(m, "Fecha"), CultureInfo.InvariantCulture),
            Tipo = Texto(m, "Tipo"),
            Accion = Texto(m, "Accion"),
            Importe = Decimal(Texto(m, "Importe")),
            Empleado = Texto(m, "Empleado"),
            CuentaReferencia = Texto(m, "CuentaReferencia")
        }).ToArray();
    }

    public async Task<decimal> RegistrarDepositoAsync(string cuenta, decimal importe, string codEmpleado) =>
        Decimal((await InvocarAsync("registrarDeposito",
            ("cuenta", cuenta), ("importe", Numero(importe)), ("codEmpleado", codEmpleado))).Value);

    public async Task<decimal> RegistrarRetiroAsync(string cuenta, decimal importe, string codEmpleado) =>
        Decimal((await InvocarAsync("registrarRetiro",
            ("cuenta", cuenta), ("importe", Numero(importe)), ("codEmpleado", codEmpleado))).Value);

    public async Task<decimal> RegistrarTransferenciaAsync(string origen, string destino, decimal importe, string codEmpleado) =>
        Decimal((await InvocarAsync("registrarTransferencia",
            ("cuentaOrigen", origen), ("cuentaDestino", destino), ("importe", Numero(importe)), ("codEmpleado", codEmpleado))).Value);

    /// <summary>Envía la petición y devuelve el elemento &lt;operacionResult&gt;, o lanza el mensaje del Fault.</summary>
    private async Task<XElement> InvocarAsync(string operacion, params (string Nombre, string Valor)[] parametros)
    {
        string url = Url;
        if (string.IsNullOrWhiteSpace(url) && ConfiguracionServidor.Error != null)
        {
            throw new ErrorServicioException(ConfiguracionServidor.Error);
        }
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? destino) || (destino.Scheme != Uri.UriSchemeHttp && destino.Scheme != Uri.UriSchemeHttps))
        {
            throw new ErrorServicioException($"La URL del servidor no es válida: {url}");
        }

        var peticion = new HttpRequestMessage(HttpMethod.Post, destino)
        {
            Content = new StringContent(ConstruirSobre(operacion, parametros, _token), Encoding.UTF8, "text/xml")
        };
        peticion.Headers.Add("SOAPAction", $"\"{Namespace}{Contrato}/{operacion}\"");
        peticion.Headers.Add("X-Cliente", NombreCliente);

        try
        {
            using HttpResponseMessage respuesta = await _http.SendAsync(peticion);
            string cuerpo = await respuesta.Content.ReadAsStringAsync();
            XElement resultado = LeerRespuesta(cuerpo, (int)respuesta.StatusCode, operacion);
            if (_token != null)
            {
                _ultimaActividad = DateTime.Now;
            }
            return resultado;
        }
        catch (ErrorServicioException)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            throw new ErrorServicioException("El servidor tardó demasiado en responder. Intente de nuevo.");
        }
        catch (Exception e) when (e is HttpRequestException || e is SocketException || e is IOException)
        {
            throw new ErrorServicioException($"No se pudo conectar a {url}. Verifica IP, puerto y red.");
        }
    }

    private static string ConstruirSobre(string operacion, (string Nombre, string Valor)[] parametros, string? token)
    {
        var cuerpo = new StringBuilder();
        foreach (var (nombre, valor) in parametros)
        {
            cuerpo.Append($"<{nombre}>{SecurityElement.Escape(valor)}</{nombre}>");
        }
        return "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
            + "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\">"
            + (string.IsNullOrEmpty(token) ? "" : $"<s:Header><Token xmlns=\"{Namespace}\">{SecurityElement.Escape(token)}</Token></s:Header>")
            + $"<s:Body><{operacion} xmlns=\"{Namespace}\">{cuerpo}</{operacion}></s:Body>"
            + "</s:Envelope>";
    }

    private static XElement LeerRespuesta(string xml, int codigoHttp, string operacion)
    {
        XDocument documento;
        try
        {
            documento = XDocument.Parse(xml);
        }
        catch (Exception)
        {
            throw new ErrorServicioException($"Respuesta inesperada del servidor (HTTP {codigoHttp}).");
        }

        XElement? Buscar(string nombre) =>
            documento.Descendants().FirstOrDefault(e => e.Name.LocalName == nombre);

        return Buscar(operacion + "Result")
            ?? throw new ErrorServicioException(
                Buscar("Mensaje")?.Value ?? Buscar("faultstring")?.Value ?? $"Respuesta inesperada del servidor (HTTP {codigoHttp}).");
    }

    private static string Texto(XElement padre, string hijo) =>
        padre.Elements().FirstOrDefault(e => e.Name.LocalName == hijo)?.Value.Trim() ?? "";

    private static string Numero(decimal valor) => valor.ToString(CultureInfo.InvariantCulture);

    private static decimal Decimal(string texto) =>
        decimal.Parse(texto, NumberStyles.Float, CultureInfo.InvariantCulture);
}
