using System.Threading.Tasks;
using ec.edu.monster.modelo;
using ec.edu.monster.servicio;

namespace ec.edu.monster.controlador
{
    /// <summary>
    /// Intermediario entre las ventanas y el servicio: solo pasa las llamadas (en segundo plano,
    /// para que la ventana no se congele). Guarda el empleado que inició sesión porque su código
    /// va en cada operación. Las reglas y los intentos de login los controla el servidor.
    /// </summary>
    public class ControladorEureka
    {
        private readonly ClienteEureka _servicio = ClienteEureka.Instancia;

        public string UrlServidor => _servicio.UrlServidor;

        /// <summary>Motivo por el que la dirección guardada del servidor no sirve (null si está bien).</summary>
        public string ErrorConfiguracion => _servicio.ErrorConfiguracion;

        /// <summary>Host y puerto actuales (para mostrarlos en la pantalla de cambio de servidor).</summary>
        public string HostServidor => System.Uri.TryCreate(UrlServidor, System.UriKind.Absolute, out System.Uri u) ? u.Host : string.Empty;

        public string PuertoServidor => System.Uri.TryCreate(UrlServidor, System.UriKind.Absolute, out System.Uri u) ? u.Port.ToString() : string.Empty;

        /// <summary>Cambia el servidor (la IP varía según la red). Lanza InvalidOperationException si host o puerto no son válidos.</summary>
        public void CambiarServidor(string host, string puerto)
        {
            _servicio.CambiarServidor(host, puerto);
            Empleado = null;
        }

        /// <summary>Minutos que le quedan al token de la sesión (null si no hay sesión).</summary>
        public int? MinutosSesion => _servicio.MinutosSesion;

        /// <summary>Para el indicador "Servidor activo" (en segundo plano: tarda hasta 3 s si no responde).</summary>
        public Task<bool> ServidorActivoAsync() => Task.Run(() => _servicio.ServidorActivo());

        public Empleado Empleado { get; private set; }

        public async Task<Empleado> IniciarSesionAsync(string usuario, string clave)
        {
            Empleado = await Task.Run(() => _servicio.Login(usuario, clave));
            return Empleado;
        }

        /// <summary>Avisa al servidor (en segundo plano) para que invalide el token.</summary>
        public async Task CerrarSesionAsync()
        {
            await Task.Run(() => _servicio.CerrarSesion());
            Empleado = null;
        }

        public Task<Cuenta> ConsultarCuentaAsync(string cuenta) =>
            Task.Run(() => _servicio.ConsultarCuenta(cuenta));

        public Task<Movimiento[]> TraerMovimientosAsync(string cuenta) =>
            Task.Run(() => _servicio.TraerMovimientos(cuenta));

        public Task<decimal> RegistrarDepositoAsync(string cuenta, decimal importe) =>
            Task.Run(() => _servicio.RegistrarDeposito(cuenta, importe, Empleado.Codigo));

        public Task<decimal> RegistrarRetiroAsync(string cuenta, decimal importe) =>
            Task.Run(() => _servicio.RegistrarRetiro(cuenta, importe, Empleado.Codigo));

        public Task<decimal> RegistrarTransferenciaAsync(string origen, string destino, decimal importe) =>
            Task.Run(() => _servicio.RegistrarTransferencia(origen, destino, importe, Empleado.Codigo));
    }
}
