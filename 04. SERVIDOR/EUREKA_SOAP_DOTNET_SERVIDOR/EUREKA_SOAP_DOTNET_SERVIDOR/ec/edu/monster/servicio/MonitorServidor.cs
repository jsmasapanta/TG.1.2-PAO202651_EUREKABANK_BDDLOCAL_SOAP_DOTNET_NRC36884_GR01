using System;
using System.Collections.Generic;
using System.Linq;
using ec.edu.monster.modelo;

namespace ec.edu.monster.servicio
{
    /// <summary>
    /// Métricas del servidor en memoria para el panel (index.html): hora de inicio, peticiones,
    /// operaciones exitosas, conteo por cliente y por operación bancaria y los últimos 60 eventos.
    /// No guarda contraseñas, tokens ni montos. Se reinicia cada vez que se vuelve a desplegar el sitio.
    /// Patrón Singleton y seguro entre hilos (todas las lecturas y escrituras van con lock).
    /// </summary>
    public sealed class MonitorServidor
    {
        public const int MaximoEventos = 60;

        /// <summary>Clientes que se cuentan por separado; cualquier otro valor de X-Cliente cuenta como OTRO.</summary>
        public static readonly string[] Clientes = { "WEB", "MOVIL", "ESCRITORIO", "CONSOLA", "PANEL", "OTRO" };

        /// <summary>Operaciones bancarias que se cuentan por tipo (login y cierre de sesión solo quedan como eventos).</summary>
        public static readonly string[] Operaciones = { "Deposito", "Retiro", "Transferencia", "Consulta", "Movimientos" };

        private static readonly Lazy<MonitorServidor> _instancia =
            new Lazy<MonitorServidor>(() => new MonitorServidor());

        public static MonitorServidor Instancia => _instancia.Value;

        private readonly object _candado = new object();
        private readonly DateTime _inicio = DateTime.Now;
        private readonly Dictionary<string, int> _porCliente;
        private readonly Dictionary<string, int> _porOperacion;
        private readonly LinkedList<EventoMonitor> _eventos = new LinkedList<EventoMonitor>();
        private long _peticiones;
        private long _exitosas;

        // Constructor privado: solo la propia clase crea la instancia.
        private MonitorServidor()
        {
            _porCliente = Clientes.ToDictionary(c => c, c => 0);
            _porOperacion = Operaciones.ToDictionary(o => o, o => 0);
        }

        /// <summary>Normaliza el header X-Cliente: WEB, MOVIL, ESCRITORIO, CONSOLA, PANEL u OTRO.</summary>
        public static string NormalizarCliente(string cliente)
        {
            string valor = (cliente ?? string.Empty).Trim().ToUpperInvariant();
            return Clientes.Contains(valor) ? valor : "OTRO";
        }

        /// <summary>Anota una petición atendida (con éxito o con error).</summary>
        public void Registrar(string cliente, string operacion, bool exito)
        {
            cliente = NormalizarCliente(cliente);
            lock (_candado)
            {
                _peticiones++;
                if (exito)
                {
                    _exitosas++;
                }
                _porCliente[cliente]++;
                if (_porOperacion.ContainsKey(operacion))
                {
                    _porOperacion[operacion]++;
                }
                _eventos.AddFirst(new EventoMonitor { Hora = DateTime.Now, Cliente = cliente, Operacion = operacion, Exito = exito });
                if (_eventos.Count > MaximoEventos)
                {
                    _eventos.RemoveLast();
                }
            }
        }

        /// <summary>Copia de las métricas en este momento, más las sesiones vigentes.</summary>
        public EstadoServidor Estado(int sesionesActivas)
        {
            lock (_candado)
            {
                return new EstadoServidor
                {
                    Inicio = _inicio,
                    SegundosActivo = (long)(DateTime.Now - _inicio).TotalSeconds,
                    TotalPeticiones = _peticiones,
                    TotalExitosas = _exitosas,
                    SesionesActivas = sesionesActivas,
                    PorCliente = Clientes.Select(c => new Conteo { Nombre = c, Cantidad = _porCliente[c] }).ToList(),
                    PorOperacion = Operaciones.Select(o => new Conteo { Nombre = o, Cantidad = _porOperacion[o] }).ToList(),
                    Eventos = _eventos.ToList()
                };
            }
        }
    }
}
