using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.ServiceModel;
using System.ServiceModel.Channels;
using ec.edu.monster.modelo;

namespace ec.edu.monster.servicio;

/// <summary>Error que la vista puede mostrar tal cual al usuario (normalmente el mensaje que envió el servidor).</summary>
public class ErrorServicioException : Exception
{
    public ErrorServicioException(string mensaje) : base(mensaje) { }
}

/// <summary>
/// Llama a las operaciones del servicio SOAP con el contrato IWSEureka. La dirección se arma con la sección
/// "ServidorSoap" (Host, Puerto, Ruta) de appsettings.json. Cada petición lleva el header HTTP X-Cliente: WEB
/// y el token de la sesión del usuario en el header SOAP &lt;Token&gt;. No tiene reglas del banco: si el servidor
/// rechaza algo, su mensaje se entrega a la vista.
/// Se registra como Singleton en Program.cs: la ChannelFactory se crea una sola vez y es segura entre hilos.
/// El token no se guarda aquí (la web atiende a varios usuarios): cada controlador lo toma de la sesión.
/// </summary>
public class ClienteEureka : IDisposable
{
    public const string NombreCliente = "WEB";
    private const string Namespace = "http://espe.edu.ec/eureka/";

    private ChannelFactory<IWSEureka>? _fabrica;

    /// <summary>Si Host o Puerto de appsettings.json no son válidos, este mensaje se muestra en cada pantalla.</summary>
    private string? _errorConfiguracion;

    private readonly string _ruta;
    private readonly string _archivoConfiguracion;
    private readonly object _candado = new();

    public ClienteEureka(IConfiguration configuracion, IWebHostEnvironment entorno)
    {
        _ruta = (configuracion["ServidorSoap:Ruta"] ?? "WSEureka.svc").Trim().TrimStart('/');
        _archivoConfiguracion = Path.Combine(entorno.ContentRootPath, "appsettings.json");
        _errorConfiguracion = Conectar(configuracion["ServidorSoap:Host"] ?? "", configuracion["ServidorSoap:Puerto"] ?? "");
    }

    public string UrlServidor { get; private set; } = "(configuración inválida)";

    public string HostServidor { get; private set; } = "";

    public string PuertoServidor { get; private set; } = "";

    /// <summary>
    /// Cambia el servidor desde la pantalla de login (la IP varía según la red). Si host y puerto son válidos,
    /// crea la conexión nueva y los guarda en appsettings.json; si no, devuelve el mensaje de error.
    /// Las sesiones abiertas con el servidor anterior dejan de servir: hay que volver a iniciar sesión.
    /// </summary>
    public string? CambiarServidor(string host, string puerto)
    {
        lock (_candado)
        {
            ChannelFactory<IWSEureka>? anterior = _fabrica;
            string? error = Conectar(host, puerto);
            if (error != null)
            {
                return error;
            }
            _errorConfiguracion = null;
            if (anterior != null)
            {
                try { anterior.Close(); } catch { anterior.Abort(); }
            }
            Guardar();
            return null;
        }
    }

    /// <summary>Valida host y puerto y crea la ChannelFactory. Devuelve el mensaje de error o null si quedó conectado.</summary>
    private string? Conectar(string host, string puerto)
    {
        host = host.Trim();
        puerto = puerto.Trim();
        Separar(ref host, ref puerto);
        if (host.Length == 0 || Uri.CheckHostName(host) == UriHostNameType.Unknown)
        {
            return $"Host del servidor inválido: \"{host}\". Escriba la IP de la PC del servidor (ejemplo: 192.168.1.10).";
        }
        if (!int.TryParse(puerto, out int numero) || numero < 1 || numero > 65535)
        {
            return $"Puerto del servidor inválido: \"{puerto}\". Debe ser un número entre 1 y 65535 (el servidor usa 8087).";
        }
        HostServidor = host;
        PuertoServidor = numero.ToString();
        UrlServidor = new UriBuilder("http", host, numero, _ruta).Uri.ToString();
        var binding = new BasicHttpBinding
        {
            OpenTimeout = TimeSpan.FromSeconds(10),
            SendTimeout = TimeSpan.FromSeconds(30),
            ReceiveTimeout = TimeSpan.FromSeconds(30),
            // Una cuenta con muchos movimientos supera los 64 KB por defecto.
            MaxReceivedMessageSize = 10 * 1024 * 1024
        };
        _fabrica = new ChannelFactory<IWSEureka>(binding, new EndpointAddress(UrlServidor));
        return null;
    }

    /// <summary>
    /// En "host" se acepta la IP sola (192.168.100.2), con puerto (192.168.100.2:8087) o la URL completa
    /// (http://192.168.100.2:8087/WSEureka.svc): de ahí se toman la IP y el puerto.
    /// </summary>
    private static void Separar(ref string host, ref string puerto)
    {
        if (host.Contains("://"))
        {
            if (Uri.TryCreate(host, UriKind.Absolute, out Uri? url))
            {
                host = url.Host;
                puerto = url.Port.ToString();
            }
            return;
        }
        int dosPuntos = host.LastIndexOf(':');
        if (dosPuntos > 0 && host.IndexOf(':') == dosPuntos) // "192.168.100.2:8087" (no una IPv6)
        {
            puerto = host[(dosPuntos + 1)..].Split('/')[0];
            host = host[..dosPuntos];
        }
        host = host.Split('/')[0];
    }

    /// <summary>Escribe Host y Puerto en la sección ServidorSoap de appsettings.json (para el próximo arranque).</summary>
    private void Guardar()
    {
        try
        {
            JsonNode raiz = JsonNode.Parse(File.ReadAllText(_archivoConfiguracion)) ?? new JsonObject();
            JsonObject seccion = raiz["ServidorSoap"] as JsonObject ?? new JsonObject();
            seccion["Host"] = HostServidor;
            seccion["Puerto"] = int.Parse(PuertoServidor);
            raiz["ServidorSoap"] = seccion;
            File.WriteAllText(_archivoConfiguracion, raiz.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
        {
            // Si no se puede escribir (por ejemplo publicada en una carpeta de solo lectura), el cambio vale hasta reiniciar.
        }
    }

    // Solo para el indicador "Servidor activo": pide el WSDL con un tiempo de espera corto.
    private static readonly HttpClient _ping = new() { Timeout = TimeSpan.FromSeconds(3) };

    /// <summary>true si el servidor SOAP responde (se usa en el indicador de la barra lateral y del login).</summary>
    public async Task<bool> ServidorActivoAsync()
    {
        if (_fabrica == null)
        {
            return false;
        }
        try
        {
            using HttpResponseMessage r = await _ping.GetAsync(UrlServidor + "?wsdl");
            return r.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
        {
            return false;
        }
    }

    public Task<Empleado> LoginAsync(string usuario, string clave) =>
        InvocarAsync(null, c => c.LoginAsync(usuario, clave));

    public Task<bool> CerrarSesionAsync(string token) =>
        InvocarAsync(null, c => c.CerrarSesionAsync(token));

    public Task<Cuenta> ConsultarCuentaAsync(string token, string cuenta) =>
        InvocarAsync(token, c => c.ConsultarCuentaAsync(cuenta));

    public async Task<Movimiento[]> TraerMovimientosAsync(string token, string cuenta) =>
        await InvocarAsync(token, c => c.TraerMovimientosAsync(cuenta)) ?? Array.Empty<Movimiento>();

    public Task<decimal> RegistrarDepositoAsync(string token, string cuenta, decimal importe, string codEmpleado) =>
        InvocarAsync(token, c => c.RegistrarDepositoAsync(cuenta, importe, codEmpleado));

    public Task<decimal> RegistrarRetiroAsync(string token, string cuenta, decimal importe, string codEmpleado) =>
        InvocarAsync(token, c => c.RegistrarRetiroAsync(cuenta, importe, codEmpleado));

    public Task<decimal> RegistrarTransferenciaAsync(string token, string origen, string destino, decimal importe, string codEmpleado) =>
        InvocarAsync(token, c => c.RegistrarTransferenciaAsync(origen, destino, importe, codEmpleado));

    private async Task<T> InvocarAsync<T>(string? token, Func<IWSEureka, Task<T>> llamada)
    {
        if (_fabrica == null)
        {
            throw new ErrorServicioException(_errorConfiguracion!);
        }
        IWSEureka canal = _fabrica.CreateChannel();
        var comunicacion = (ICommunicationObject)canal;
        try
        {
            // Los headers se agregan dentro del scope y la llamada se inicia ahí; el await va afuera
            // porque OperationContextScope no puede cruzar un await.
            Task<T> tarea;
            using (new OperationContextScope((IContextChannel)canal))
            {
                var http = new HttpRequestMessageProperty();
                http.Headers["X-Cliente"] = NombreCliente;
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = http;
                if (!string.IsNullOrEmpty(token))
                {
                    OperationContext.Current.OutgoingMessageHeaders.Add(MessageHeader.CreateHeader("Token", Namespace, token));
                }
                tarea = llamada(canal);
            }
            T resultado = await tarea;
            comunicacion.Close();
            return resultado;
        }
        catch (FaultException<ErrorServicio> ex)
        {
            comunicacion.Abort();
            throw new ErrorServicioException(ex.Detail.Mensaje ?? ex.Message);
        }
        catch (FaultException ex)
        {
            comunicacion.Abort();
            throw new ErrorServicioException("El servidor rechazó la petición: " + ex.Message);
        }
        catch (Exception ex) when (ex is EndpointNotFoundException || ex is HttpRequestException || ex is WebException)
        {
            comunicacion.Abort();
            throw new ErrorServicioException($"No se pudo conectar a {UrlServidor}. Verifica IP, puerto y red.");
        }
        catch (TimeoutException)
        {
            comunicacion.Abort();
            throw new ErrorServicioException($"El servidor en {UrlServidor} tardó demasiado en responder. Verifica IP, puerto y red.");
        }
        catch (CommunicationException ex)
        {
            comunicacion.Abort();
            throw new ErrorServicioException($"Error de comunicación con {UrlServidor}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_fabrica == null)
        {
            return;
        }
        try { _fabrica.Close(); } catch { _fabrica.Abort(); }
    }
}
