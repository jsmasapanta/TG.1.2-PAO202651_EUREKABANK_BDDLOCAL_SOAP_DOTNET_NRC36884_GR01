using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace ec.edu.monster.servicio
{
    /// <summary>
    /// Sesiones abiertas con login: token → vencimiento. Cada token dura 30 minutos desde su último uso.
    /// Un temporizador borra los vencidos cada minuto. Las operaciones del banco exigen un token vigente
    /// en el header SOAP &lt;Token&gt;; si no lo hay, responden "Token inválido o expirado. Inicie sesión."
    /// Patrón Singleton y seguro entre hilos.
    /// </summary>
    public sealed class GestorSesiones
    {
        public static readonly TimeSpan Duracion = TimeSpan.FromMinutes(30);
        public const string MensajeInvalido = "Token inválido o expirado. Inicie sesión.";

        private static readonly Lazy<GestorSesiones> _instancia =
            new Lazy<GestorSesiones>(() => new GestorSesiones());

        public static GestorSesiones Instancia => _instancia.Value;

        private readonly Dictionary<string, DateTime> _sesiones = new Dictionary<string, DateTime>();
        private readonly Timer _limpieza;

        // Constructor privado: solo la propia clase crea la instancia.
        private GestorSesiones()
        {
            _limpieza = new Timer(_ => Limpiar(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        /// <summary>Crea un token aleatorio (32 caracteres hexadecimales) para el empleado que inició sesión.</summary>
        public string Crear()
        {
            var bytes = new byte[16];
            using (var azar = RandomNumberGenerator.Create())
            {
                azar.GetBytes(bytes);
            }
            string token = BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
            lock (_sesiones)
            {
                _sesiones[token] = DateTime.Now.Add(Duracion);
            }
            return token;
        }

        /// <summary>Comprueba el token y le renueva los 30 minutos; si no sirve lanza la regla SESION_INVALIDA.</summary>
        public void Validar(string token)
        {
            lock (_sesiones)
            {
                if (string.IsNullOrWhiteSpace(token)
                    || !_sesiones.TryGetValue(token, out DateTime vence)
                    || vence <= DateTime.Now)
                {
                    if (token != null)
                    {
                        _sesiones.Remove(token);
                    }
                    throw new ReglaNegocioException(MensajeInvalido, "SESION_INVALIDA");
                }
                _sesiones[token] = DateTime.Now.Add(Duracion);
            }
        }

        /// <summary>Invalida el token. Devuelve false si ya no existía.</summary>
        public bool Cerrar(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }
            lock (_sesiones)
            {
                return _sesiones.Remove(token);
            }
        }

        public int ContarActivos()
        {
            Limpiar();
            lock (_sesiones)
            {
                return _sesiones.Count;
            }
        }

        private void Limpiar()
        {
            lock (_sesiones)
            {
                DateTime ahora = DateTime.Now;
                foreach (string token in _sesiones.Where(s => s.Value <= ahora).Select(s => s.Key).ToList())
                {
                    _sesiones.Remove(token);
                }
            }
        }
    }
}
