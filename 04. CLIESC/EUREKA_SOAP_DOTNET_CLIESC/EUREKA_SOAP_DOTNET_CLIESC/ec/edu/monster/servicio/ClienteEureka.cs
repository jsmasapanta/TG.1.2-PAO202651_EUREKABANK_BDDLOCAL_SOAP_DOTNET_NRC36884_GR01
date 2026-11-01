using System;
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

        private readonly ChannelFactory<IWSEureka> _fabrica;

        /// <summary>Token de la sesión; nunca se muestra en pantalla.</summary>
        private string _token;

        // Constructor privado: solo la propia clase crea la instancia.
        private ClienteEureka()
        {
            UrlServidor = ConfiguracionServidor.LeerUrl();
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

        public string UrlServidor { get; }

        public Empleado Login(string usuario, string clave)
        {
            Empleado empleado = Invocar(c => c.Login(usuario, clave));
            _token = empleado.Token;
            return empleado;
        }

        /// <summary>Invalida el token en el servidor. Si no se puede avisar, igual se olvida aquí.</summary>
        public void CerrarSesion()
        {
            string token = _token;
            _token = null;
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
