using System.Drawing;
using System.Globalization;
using System.Reflection;
using ec.edu.monster.modelo;

namespace ec.edu.monster.vista
{
    /// <summary>Cómo se muestran los datos en pantalla (textos, dinero, imagen). No tiene reglas del banco.</summary>
    internal static class Formato
    {
        /// <summary>Importe con separador de miles y 2 decimales: 6,900.00</summary>
        public static string Dinero(decimal importe) => importe.ToString("#,##0.00", CultureInfo.InvariantCulture);

        public static string Titulo(TipoOperacion tipo)
        {
            switch (tipo)
            {
                case TipoOperacion.Deposito: return "Depósito";
                case TipoOperacion.Retiro: return "Retiro";
                default: return "Transferencia";
            }
        }

        /// <summary>El servidor manda la acción (INGRESO o SALIDA); aquí solo se elige el color y el signo.</summary>
        public static bool EsIngreso(Movimiento m) => m.Accion == "INGRESO";

        /// <summary>
        /// Convierte lo que escribió el usuario a número (punto o coma decimal), porque el servicio recibe
        /// un decimal. Si es cero, negativo o tiene más de 2 decimales lo rechaza el servidor.
        /// </summary>
        public static bool IntentarLeerImporte(string texto, out decimal importe)
        {
            string normalizado = (texto ?? string.Empty).Trim().Replace(',', '.');
            return decimal.TryParse(normalizado, NumberStyles.Number, CultureInfo.InvariantCulture, out importe);
        }

        /// <summary>Número de cuenta en dos grupos, como en una tarjeta: 0010 0001.</summary>
        public static string NumeroTarjeta(string cuenta) =>
            cuenta != null && cuenta.Length == 8 ? cuenta.Substring(0, 4) + " " + cuenta.Substring(4) : cuenta;

        /// <summary>Símbolo según la moneda que manda el servidor (Soles, Dólares).</summary>
        public static string Simbolo(string moneda) =>
            (moneda ?? string.Empty).StartsWith("D", System.StringComparison.OrdinalIgnoreCase) ? "$" : "S/";

        /// <summary>
        /// Intentos que quedan según el mensaje del servidor ("Intentos restantes: 2"). Sin mensaje: los 3.
        /// Solo sirve para dibujar las caritas del login; quien cuenta los intentos es el servidor.
        /// </summary>
        public static int IntentosRestantes(string mensaje)
        {
            if (string.IsNullOrEmpty(mensaje)) return 3;
            var m = System.Text.RegularExpressions.Regex.Match(mensaje, @"Intentos restantes:\s*(\d+)");
            if (m.Success) return int.Parse(m.Groups[1].Value);
            return mensaje.IndexOf("bloquead", System.StringComparison.OrdinalIgnoreCase) >= 0 ? 0 : 3;
        }

        /// <summary>Host y puerto de la URL, para los indicadores (192.168.100.2:8087).</summary>
        public static string Host(string url)
        {
            return System.Uri.TryCreate(url, System.UriKind.Absolute, out System.Uri u) ? u.Authority : url;
        }

        /// <summary>Imagen de Monsters Inc. incluida dentro del .exe (Recursos/monster.jpg).</summary>
        public static Image Monster()
        {
            using (var flujo = Assembly.GetExecutingAssembly().GetManifestResourceStream("monster.jpg"))
            {
                return flujo == null ? null : new Bitmap(Image.FromStream(flujo));
            }
        }
    }
}
