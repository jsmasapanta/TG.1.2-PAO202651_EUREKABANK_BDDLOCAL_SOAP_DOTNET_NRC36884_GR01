using System.Net;
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

    private readonly ChannelFactory<IWSEureka>? _fabrica;

    /// <summary>Si Host o Puerto de appsettings.json no son válidos, este mensaje se muestra en cada pantalla.</summary>
    private readonly string? _errorConfiguracion;

    public ClienteEureka(IConfiguration configuracion)
    {
        string host = (configuracion["ServidorSoap:Host"] ?? "").Trim();
        string puerto = (configuracion["ServidorSoap:Puerto"] ?? "").Trim();
        string ruta = (configuracion["ServidorSoap:Ruta"] ?? "WSEureka.svc").Trim().TrimStart('/');

        if (host.Length == 0 || Uri.CheckHostName(host) == UriHostNameType.Unknown)
        {
            _errorConfiguracion = $"Host del servidor inválido: \"{host}\". Escriba la IP de la PC del servidor en ServidorSoap:Host de appsettings.json.";
        }
        else if (!int.TryParse(puerto, out int numero) || numero < 1 || numero > 65535)
        {
            _errorConfiguracion = $"Puerto del servidor inválido: \"{puerto}\". Debe ser un número entre 1 y 65535 (el servidor usa 8087).";
        }
        else
        {
            UrlServidor = new UriBuilder("http", host, numero, ruta).Uri.ToString();
            var binding = new BasicHttpBinding
            {
                OpenTimeout = TimeSpan.FromSeconds(10),
                SendTimeout = TimeSpan.FromSeconds(30),
                ReceiveTimeout = TimeSpan.FromSeconds(30),
                // Una cuenta con muchos movimientos supera los 64 KB por defecto.
                MaxReceivedMessageSize = 10 * 1024 * 1024
            };
            _fabrica = new ChannelFactory<IWSEureka>(binding, new EndpointAddress(UrlServidor));
        }
    }

    public string UrlServidor { get; } = "(configuración inválida)";

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
