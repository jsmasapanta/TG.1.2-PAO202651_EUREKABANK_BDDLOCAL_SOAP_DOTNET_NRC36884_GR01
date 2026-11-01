using ec.edu.monster.modelo;
using ec.edu.monster.servicio;

namespace ec.edu.monster.controlador
{
    /// <summary>
    /// Intermediario entre la vista y el servicio: solo pasa las llamadas.
    /// Guarda el empleado que inició sesión porque su código va en cada operación.
    /// </summary>
    public class ControladorConsola
    {
        // Se pide al usarlo: si App.config tiene un host o puerto inválido, el error se muestra en la vista.
        private ClienteEureka _servicio => ClienteEureka.Instancia;

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

        /// <summary>Para el indicador "Servidor activo" (espera hasta 3 s si no responde).</summary>
        public bool ServidorActivo() => _servicio.ServidorActivo();

        public Empleado Empleado { get; private set; }

        public Empleado IniciarSesion(string usuario, string clave)
        {
            Empleado = _servicio.Login(usuario, clave);
            return Empleado;
        }

        /// <summary>Avisa al servidor para que invalide el token.</summary>
        public void CerrarSesion()
        {
            _servicio.CerrarSesion();
            Empleado = null;
        }

        public Cuenta ConsultarCuenta(string cuenta) => _servicio.ConsultarCuenta(cuenta);

        public Movimiento[] TraerMovimientos(string cuenta) => _servicio.TraerMovimientos(cuenta);

        public decimal RegistrarDeposito(string cuenta, decimal importe) =>
            _servicio.RegistrarDeposito(cuenta, importe, Empleado.Codigo);

        public decimal RegistrarRetiro(string cuenta, decimal importe) =>
            _servicio.RegistrarRetiro(cuenta, importe, Empleado.Codigo);

        public decimal RegistrarTransferencia(string origen, string destino, decimal importe) =>
            _servicio.RegistrarTransferencia(origen, destino, importe, Empleado.Codigo);
    }
}
