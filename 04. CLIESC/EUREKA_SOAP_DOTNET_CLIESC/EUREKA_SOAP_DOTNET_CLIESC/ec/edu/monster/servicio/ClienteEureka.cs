using System;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using ec.edu.monster.modelo;

namespace ec.edu.monster.servicio
{
    /// <summary>Error que la vista puede mostrar tal cual al usuario (normalmente el mensaje que envió el servidor).</summary>
    public class ErrorServicioException : Exception
    {
        public ErrorServicioException(string mensaje) : base(mensaje) { }
    }

    /// <summary>
    /// Llama a las operaciones del servicio SOAP. La dirección se arma con ConfiguracionServidor (App.config o
    /// variables SOAP_HOST/SOAP_PORT). Cada petición lleva el header HTTP X-Cliente: ESCRITORIO y, después del
    /// login, el token en el header SOAP &lt;Token&gt;. No tiene reglas del banco: si el servidor rechaza algo,
    /// su mensaje (ErrorServicio) se entrega a la vista.
    /// Patrón Singleton: toda la aplicación usa ClienteEureka.Instancia (una sola ChannelFactory).
    /// </summary>
    public sealed class ClienteEureka
    {
        public const string NombreCliente = "ESCRITORIO";
        private const string Namespace = "http://espe.edu.ec/eureka/";

        private static readonly Lazy<ClienteEureka> _instancia =
            new Lazy<ClienteEureka>(() => new ClienteEureka());

        public static ClienteEureka Instancia => _instancia.Value;

        private ChannelFactory<IWSEureka> _fabrica;

        /// <summary>Token de la sesión; nunca se muestra en pantalla.</summary>
        private string _token;

        /// <summary>Última respuesta correcta del servidor: el token vence 30 minutos después.</summary>
        private DateTime? _ultimaActividad;

        // Constructor privado: solo la propia clase crea la instancia.
        private ClienteEureka()
        {
            Conectar(ConfiguracionServidor.LeerUrl());
        }

        public string UrlServidor { get; private set; }

        /// <summary>
        /// Cambia el servidor desde la pantalla (la IP varía según la red). Valida host y puerto, lo guarda en el
        /// .exe.config y crea la conexión nueva. La sesión anterior se olvida: hay que volver a iniciar sesión.
        /// </summary>
        public void CambiarServidor(string host, string puerto)
        {
            string url = ConfiguracionServidor.ArmarUrl(host, puerto); // lanza InvalidOperationException si no es válido
            ConfiguracionServidor.Guardar(host, puerto);
            _token = null;
            _ultimaActividad = null;
            try { _fabrica.Close(); } catch { _fabrica.Abort(); }
            Conectar(url);
        }

        private void Conectar(string url)
        {
            UrlServidor = url;
            var binding = new BasicHttpBinding
            {
                OpenTimeout = TimeSpan.FromSeconds(10),
                SendTimeout = TimeSpan.FromSeconds(30),
                ReceiveTimeout = TimeSpan.FromSeconds(30),
                // Una cuenta con muchos movimientos supera los 64 KB por defecto.
                MaxReceivedMessageSize = 10 * 1024 * 1024
            };
            _fabrica = new ChannelFactory<IWSEureka>(binding, new EndpointAddress(url));
        }

        /// <summary>Minutos que le quedan a la sesión (null si no hay sesión).</summary>
        public int? MinutosSesion =>
            _token == null || _ultimaActividad == null
                ? (int?)null
                : Math.Max(0, 30 - (int)(DateTime.Now - _ultimaActividad.Value).TotalMinutes);

        /// <summary>true si el servidor responde (pide el WSDL con 3 segundos de espera). Para el indicador "Servidor activo".</summary>
        public bool ServidorActivo()
        {
            try
            {
                var peticion = (HttpWebRequest)WebRequest.Create(UrlServidor + "?wsdl");
                peticion.Timeout = 3000;
                using (var respuesta = (HttpWebResponse)peticion.GetResponse())
                {
                    return respuesta.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (WebException)
            {
                return false;
            }
        }

        public Empleado Login(string usuario, string clave)
        {
            Empleado empleado = Invocar(c => c.Login(usuario, clave));
            _token = empleado.Token;
            _ultimaActividad = DateTime.Now;
            return empleado;
        }

        /// <summary>Invalida el token en el servidor. Si no se puede avisar, igual se olvida aquí.</summary>
        public void CerrarSesion()
        {
            string token = _token;
            _token = null;
            _ultimaActividad = null;
            if (token == null)
            {
                return;
            }
            try
            {
                Invocar(c => c.CerrarSesion(token));
            }
            catch (ErrorServicioException)
            {
                // El token vence solo a los 30 minutos.
            }
        }

        public Cuenta ConsultarCuenta(string cuenta) => Invocar(c => c.ConsultarCuenta(cuenta));

        public Movimiento[] TraerMovimientos(string cuenta) =>
            Invocar(c => c.TraerMovimientos(cuenta)) ?? new Movimiento[0];

        public decimal RegistrarDeposito(string cuenta, decimal importe, string codEmpleado) =>
            Invocar(c => c.RegistrarDeposito(cuenta, importe, codEmpleado));

        public decimal RegistrarRetiro(string cuenta, decimal importe, string codEmpleado) =>
            Invocar(c => c.RegistrarRetiro(cuenta, importe, codEmpleado));

        public decimal RegistrarTransferencia(string origen, string destino, decimal importe, string codEmpleado) =>
            Invocar(c => c.RegistrarTransferencia(origen, destino, importe, codEmpleado));

        private T Invocar<T>(Func<IWSEureka, T> llamada)
        {
            IWSEureka canal = _fabrica.CreateChannel();
            var comunicacion = (ICommunicationObject)canal;
            try
            {
                T resultado;
                using (new OperationContextScope((IContextChannel)canal))
                {
                    var http = new HttpRequestMessageProperty();
                    http.Headers["X-Cliente"] = NombreCliente;
                    OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = http;
                    if (_token != null)
                    {
                        OperationContext.Current.OutgoingMessageHeaders.Add(MessageHeader.CreateHeader("Token", Namespace, _token));
                    }
                    resultado = llamada(canal);
                }
                comunicacion.Close();
                if (_token != null)
                {
                    _ultimaActividad = DateTime.Now;
                }
                return resultado;
            }
            catch (FaultException<ErrorServicio> ex)
            {
                comunicacion.Abort();
                throw new ErrorServicioException(ex.Detail.Mensaje);
            }
            catch (FaultException ex)
            {
                comunicacion.Abort();
                throw new ErrorServicioException("El servidor rechazó la petición: " + ex.Message);
            }
            catch (EndpointNotFoundException)
            {
                comunicacion.Abort();
                throw new ErrorServicioException("No se pudo conectar a " + UrlServidor + ". Verifica IP, puerto y red.");
            }
            catch (TimeoutException)
            {
                comunicacion.Abort();
                throw new ErrorServicioException("El servidor en " + UrlServidor + " tardó demasiado en responder. Verifica IP, puerto y red.");
            }
            catch (CommunicationException ex)
            {
                comunicacion.Abort();
                throw new ErrorServicioException("Error de comunicación con " + UrlServidor + ": " + ex.Message);
            }
        }
    }
}
