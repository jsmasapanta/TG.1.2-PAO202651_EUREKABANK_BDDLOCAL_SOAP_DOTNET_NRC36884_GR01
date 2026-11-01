using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ec.edu.monster.controlador;
using ec.edu.monster.modelo;
using ec.edu.monster.servicio;

namespace ec.edu.monster.vista
{
    /// <summary>
    /// Pantallas de la consola: pide los datos, se los pasa al controlador y muestra lo que devuelve
    /// el servidor (datos o su mensaje de error). No tiene reglas del banco ni cuenta intentos de login.
    /// </summary>
    public class VistaConsola
    {
        private const int Ancho = 76;

        // Título en letras grandes: MONSTER y EUREKA BANK en bloques, como la consola del TI.1.2.
        private static readonly string[] TituloMonster =
        {
            "███╗   ███╗ ██████╗ ███╗   ██╗███████╗████████╗███████╗██████╗ ",
            "████╗ ████║██╔═══██╗████╗  ██║██╔════╝╚══██╔══╝██╔════╝██╔══██╗",
            "██╔████╔██║██║   ██║██╔██╗ ██║███████╗   ██║   █████╗  ██████╔╝",
            "██║╚██╔╝██║██║   ██║██║╚██╗██║╚════██║   ██║   ██╔══╝  ██╔══██╗",
            "██║ ╚═╝ ██║╚██████╔╝██║ ╚████║███████║   ██║   ███████╗██║  ██║",
            "╚═╝     ╚═╝ ╚═════╝ ╚═╝  ╚═══╝╚══════╝   ╚═╝   ╚══════╝╚═╝  ╚═╝",
        };

        private static readonly string[] TituloEureka =
        {
            "█▀▀ █ █ █▀█ █▀▀ █▄▀ ▄▀█    █▄▄ ▄▀█ █▄ █ █▄▀",
            "██▄ █▄█ █▀▄ ██▄ █ █ █▀█    █▄█ █▀█ █ ▀█ █ █",
        };

        private readonly ControladorConsola _controlador = new ControladorConsola();

        public VistaConsola()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
            Console.Title = "MONSTER · EurekaBank - SOAP .NET";
        }

        // ================= Flujo de pantallas =================

        public void Iniciar()
        {
            if (!PantallaLogin())
            {
                return;
            }
            PantallaMenu();
            MostrarEncabezado("Sesión finalizada. ¡Hasta pronto!");
        }

        /// <summary>El servidor decide si el login es correcto, cuenta los intentos y bloquea al usuario.</summary>
        private bool PantallaLogin()
        {
            while (true)
            {
                MostrarEncabezado("INICIO DE SESIÓN");
                MostrarServidor(_controlador.UrlServidor);
                string usuario = PedirTexto("Usuario (o S para cambiar el servidor)");
                if (usuario.Trim().Equals("S", StringComparison.OrdinalIgnoreCase))
                {
                    PantallaCambiarServidor();
                    continue;
                }
                string clave = PedirClave("Clave");
                try
                {
                    Empleado empleado = ConEspera("Verificando credenciales en el servidor", () => _controlador.IniciarSesion(usuario, clave));
                    MostrarExito($"Bienvenido, {empleado.Nombre} (empleado {empleado.Codigo}).");
                    Pausa();
                    return true;
                }
                catch (ErrorServicioException ex)
                {
                    MostrarError(ex.Message);
                    MostrarIntentos(ex.Message);
                }
                if (!PreguntarSiNo("¿Intentar de nuevo?"))
                {
                    return false;
                }
            }
        }

        private void PantallaMenu()
        {
            var opciones = new List<string> { "Consultar cuenta y movimientos", "Depósito", "Retiro", "Transferencia", "Estado del servidor" };
            while (true)
            {
                MostrarEncabezado($"MENÚ PRINCIPAL · {_controlador.Empleado.Nombre}");
                switch (PedirOpcion(opciones, "Cerrar sesión"))
                {
                    case 0: _controlador.CerrarSesion(); return;
                    case 1: PantallaMovimientos(); break;
                    case 2: PantallaOperacion(TipoOperacion.Deposito); break;
                    case 3: PantallaOperacion(TipoOperacion.Retiro); break;
                    case 4: PantallaOperacion(TipoOperacion.Transferencia); break;
                    case 5: PantallaEstado(); break;
                }
            }
        }

        private void PantallaMovimientos()
        {
            do
            {
                MostrarEncabezado("CONSULTAR CUENTA Y MOVIMIENTOS");
                string cuenta = PedirCuenta("Número de cuenta");
                try
                {
                    Cuenta datos = ConEspera("Consultando la cuenta", () => _controlador.ConsultarCuenta(cuenta));
                    Movimiento[] movimientos = ConEspera("Trayendo movimientos", () => _controlador.TraerMovimientos(cuenta));
                    MostrarCuenta(datos);
                    MostrarMovimientos(movimientos);
                }
                catch (ErrorServicioException ex)
                {
                    MostrarError(ex.Message);
                }
            }
            while (PreguntarSiNo("¿Consultar otra cuenta?"));
        }

        private void PantallaOperacion(TipoOperacion tipo)
        {
            do
            {
                MostrarEncabezado(Titulo(tipo).ToUpperInvariant());
                string cuenta = PedirCuenta(tipo == TipoOperacion.Transferencia ? "Cuenta de origen" : "Número de cuenta");
                string destino = tipo == TipoOperacion.Transferencia ? PedirCuenta("Cuenta de destino") : null;
                decimal importe = PedirImporte("Importe");

                if (!PreguntarSiNo($"¿Confirma {Titulo(tipo).ToLowerInvariant()} de {Dinero(importe)}?"))
                {
                    MostrarError("Operación cancelada.");
                    continue;
                }
                try
                {
                    decimal saldo = ConEspera("Enviando al servidor SOAP", () =>
                          tipo == TipoOperacion.Deposito ? _controlador.RegistrarDeposito(cuenta, importe)
                        : tipo == TipoOperacion.Retiro ? _controlador.RegistrarRetiro(cuenta, importe)
                        : _controlador.RegistrarTransferencia(cuenta, destino, importe));
                    MostrarComprobante(tipo, cuenta, destino, importe, saldo);
                }
                catch (ErrorServicioException ex)
                {
                    MostrarError(ex.Message);
                }
            }
            while (PreguntarSiNo("¿Realizar otra operación de este tipo?"));
        }

        /// <summary>La IP del servidor varía según la red: aquí se cambia host y puerto (se guardan en el .exe.config).</summary>
        private void PantallaCambiarServidor()
        {
            MostrarEncabezado("CAMBIAR SERVIDOR");
            Console.WriteLine("  Actual: " + _controlador.UrlServidor);
            Console.WriteLine();
            string host = PedirTexto($"IP o nombre del servidor [{_controlador.HostServidor}]").Trim();
            string puerto = PedirTexto($"Puerto [{_controlador.PuertoServidor}]").Trim();
            try
            {
                _controlador.CambiarServidor(host.Length == 0 ? _controlador.HostServidor : host,
                                             puerto.Length == 0 ? _controlador.PuertoServidor : puerto);
                _servidorActivo = null; // se vuelve a comprobar en el encabezado
                MostrarExito("Servidor cambiado a " + _controlador.UrlServidor);
            }
            catch (InvalidOperationException ex)
            {
                MostrarError(ex.Message);
            }
            catch (System.Configuration.ConfigurationErrorsException ex)
            {
                MostrarError("No se pudo guardar en el .exe.config: " + ex.Message);
            }
            Pausa();
        }

        /// <summary>Opción 5: ¿responde el servidor?, cuánto tarda y cuánto le queda a la sesión.</summary>
        private void PantallaEstado()
        {
            MostrarEncabezado("ESTADO DEL SERVIDOR");
            var reloj = Stopwatch.StartNew();
            bool activo = ConEspera("Comprobando el servidor", () => _controlador.ServidorActivo());
            reloj.Stop();
            _servidorActivo = activo;
            _ultimaComprobacion = DateTime.Now;
            Console.ForegroundColor = activo ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine(activo ? "  ■ Servidor activo" : "  ■ Sin respuesta del servidor");
            Console.ResetColor();
            Console.WriteLine("    URL           : " + _controlador.UrlServidor);
            Console.WriteLine("    Respuesta     : " + (activo ? reloj.ElapsedMilliseconds + " ms" : "—"));
            Console.WriteLine("    Sesión        : " + (_controlador.MinutosSesion == null ? "—" : $"vence en {_controlador.MinutosSesion} min (token de 30 min)"));
            Console.WriteLine("    Este cliente  : X-Cliente: CONSOLA");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("    En el panel del servidor (http://IP:8087/) las peticiones de esta consola salen como CONSOLA.");
            Console.ResetColor();
            Pausa();
        }

        // ================= Entrada y salida =================

        // El estado del servidor se vuelve a comprobar como mucho cada 10 s (cada pantalla dibuja el encabezado).
        private bool? _servidorActivo;
        private DateTime _ultimaComprobacion = DateTime.MinValue;

        private bool ServidorActivo()
        {
            if (_servidorActivo == null || (DateTime.Now - _ultimaComprobacion).TotalSeconds > 10)
            {
                _servidorActivo = _controlador.ServidorActivo();
                _ultimaComprobacion = DateTime.Now;
            }
            return _servidorActivo.Value;
        }

        /// <summary>
        /// Ejecuta la llamada al servidor mostrando un spinner ("⠋ Enviando…") mientras espera.
        /// Con la salida redirigida (pruebas) llama directo, sin animación.
        /// </summary>
        private static T ConEspera<T>(string texto, Func<T> llamada)
        {
            if (Console.IsOutputRedirected)
            {
                return llamada();
            }
            Task<T> tarea = Task.Run(llamada);
            string[] cuadros = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
            int i = 0;
            Console.CursorVisible = false;
            while (!tarea.Wait(90))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("\r  " + cuadros[i++ % cuadros.Length] + " ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(texto + "...");
            }
            Console.Write("\r" + new string(' ', texto.Length + 10) + "\r");
            Console.ResetColor();
            Console.CursorVisible = true;
            return tarea.GetAwaiter().GetResult(); // relanza ErrorServicioException tal cual
        }

        /// <summary>Caritas de intentos según el mensaje del servidor ("Intentos restantes: N"); quien cuenta es el servidor.</summary>
        private static void MostrarIntentos(string mensaje)
        {
            Match m = Regex.Match(mensaje ?? string.Empty, @"Intentos restantes:\s*(\d+)");
            if (!m.Success && (mensaje ?? string.Empty).IndexOf("bloquead", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }
            int restantes = m.Success ? int.Parse(m.Groups[1].Value) : 0;
            Console.Write("  Intentos: ");
            for (int i = 0; i < 3; i++)
            {
                Console.ForegroundColor = i < restantes ? ConsoleColor.Green : ConsoleColor.DarkGray;
                Console.Write(i < restantes ? "■ " : "□ ");
            }
            Console.ResetColor();
            Console.WriteLine();
        }

        public void MostrarEncabezado(string subtitulo)
        {
            if (!Console.IsOutputRedirected)
            {
                Console.Clear();
            }
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            foreach (string linea in TituloMonster)
            {
                Console.WriteLine(Centrar(linea).TrimEnd());
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (string linea in TituloEureka)
            {
                Console.WriteLine(Centrar(linea).TrimEnd());
            }
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(" ╔" + new string('═', Ancho - 2) + "╗");
            Console.WriteLine(" ║" + Centrar("SOAP .NET + SQL SERVER · ESPE · ARQUITECTURA DE SOFTWARE", Ancho - 2) + "║");
            LineaEstado();
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(" ╚" + new string('═', Ancho - 2) + "╝");
            Console.ResetColor();
            if (!string.IsNullOrEmpty(subtitulo))
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  " + subtitulo);
                Console.ResetColor();
            }
            Console.WriteLine();
        }

        /// <summary>Línea del recuadro: ■ Servidor activo host   Empleado   sesión: N min.</summary>
        private void LineaEstado()
        {
            bool activo = ServidorActivo();
            string host = Uri.TryCreate(_controlador.UrlServidor, UriKind.Absolute, out Uri u) ? u.Authority : _controlador.UrlServidor;
            string estado = (activo ? "Servidor activo " : "Sin respuesta ") + host;
            string empleado = _controlador.Empleado == null ? "" : $"   {_controlador.Empleado.Nombre} ({_controlador.Empleado.Codigo})";
            string sesion = _controlador.MinutosSesion == null ? "" : $"   sesión: {_controlador.MinutosSesion} min";
            // La línea mide lo mismo que el marco: " ║ ■ " + texto + relleno + " ║" = Ancho + 1.
            int espacio = Ancho - 6;
            if (estado.Length + empleado.Length + sesion.Length > espacio)
            {
                estado = activo ? "Servidor activo" : "Sin respuesta"; // sin el host si no alcanza
            }
            int relleno = Math.Max(0, espacio - estado.Length - empleado.Length - sesion.Length);

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(" ║ ");
            Console.ForegroundColor = activo ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write("■ ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(estado);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(empleado);
            Console.ForegroundColor = _controlador.MinutosSesion <= 5 ? ConsoleColor.Red : ConsoleColor.DarkGray;
            Console.Write(sesion);
            Console.Write(new string(' ', relleno));
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(" ║");
        }

        public void MostrarServidor(string url)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  Servidor: " + url);
            Console.ResetColor();
            Console.WriteLine();
        }

        public string PedirTexto(string etiqueta)
        {
            Console.Write("  " + etiqueta + ": ");
            return Console.ReadLine() ?? string.Empty;
        }

        /// <summary>Lee la clave mostrando asteriscos.</summary>
        public string PedirClave(string etiqueta)
        {
            Console.Write("  " + etiqueta + ": ");
            if (Console.IsInputRedirected)
            {
                // Entrada desde archivo o pruebas automáticas: no hay teclado para leer tecla por tecla.
                string linea = Console.ReadLine() ?? string.Empty;
                Console.WriteLine();
                return linea;
            }
            var clave = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo tecla = Console.ReadKey(true);
                if (tecla.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return clave.ToString();
                }
                if (tecla.Key == ConsoleKey.Backspace)
                {
                    if (clave.Length > 0)
                    {
                        clave.Length--;
                        Console.Write("\b \b");
                    }
                }
                else if (!char.IsControl(tecla.KeyChar))
                {
                    clave.Append(tecla.KeyChar);
                    Console.Write('*');
                }
            }
        }

        /// <summary>Muestra opciones numeradas y devuelve la elegida (0 = volver/salir).</summary>
        public int PedirOpcion(IList<string> opciones, string textoCero)
        {
            for (int i = 0; i < opciones.Count; i++)
            {
                Console.WriteLine($"   [{i + 1}] {opciones[i]}");
            }
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"   [0] {textoCero}");
            Console.ResetColor();
            Console.WriteLine();

            while (true)
            {
                string entrada = PedirTexto("Opción");
                if (int.TryParse(entrada, out int opcion) && opcion >= 0 && opcion <= opciones.Count)
                {
                    return opcion;
                }
                MostrarError($"Opción inválida. Escriba un número entre 0 y {opciones.Count}.");
            }
        }

        /// <summary>Pide un número de cuenta. Si no tiene el formato correcto lo dirá el servidor.</summary>
        public string PedirCuenta(string etiqueta)
        {
            while (true)
            {
                string cuenta = PedirTexto(etiqueta).Trim();
                if (cuenta.Length > 0)
                {
                    return cuenta;
                }
                MostrarError("Escriba el número de cuenta.");
            }
        }

        /// <summary>
        /// Pide el importe (punto o coma decimal). Solo se comprueba que sea un número, porque el servicio
        /// recibe un decimal; si es cero, negativo o tiene más de 2 decimales lo rechaza el servidor.
        /// </summary>
        public decimal PedirImporte(string etiqueta)
        {
            while (true)
            {
                string entrada = PedirTexto(etiqueta).Trim().Replace(',', '.');
                if (decimal.TryParse(entrada, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal importe))
                {
                    return importe;
                }
                MostrarError("Escriba el importe con números (ejemplo: 150 o 75.50).");
            }
        }

        public void MostrarCuenta(Cuenta cuenta)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ┌" + new string('─', Ancho - 4) + "┐");
            Console.WriteLine("  │" + Ajustar($" Cuenta   : {cuenta.Codigo}  ({cuenta.Estado})", Ancho - 4) + "│");
            Console.WriteLine("  │" + Ajustar($" Cliente  : {cuenta.Cliente}", Ancho - 4) + "│");
            Console.WriteLine("  │" + Ajustar($" Sucursal : {cuenta.Sucursal}   ·   Moneda: {cuenta.Moneda}", Ancho - 4) + "│");
            Console.WriteLine("  │" + Ajustar($" SALDO    : {Dinero(cuenta.Saldo)}", Ancho - 4) + "│");
            Console.WriteLine("  └" + new string('─', Ancho - 4) + "┘");
            Console.ResetColor();
        }

        public void MostrarMovimientos(IList<Movimiento> movimientos)
        {
            Console.WriteLine();
            Console.WriteLine($"  Movimientos ({movimientos.Count}), el más reciente primero:");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("  {0,4}  {1,-16}  {2,-20}  {3,14}  {4,-8}  {5,-8}", "Nro", "Fecha", "Tipo", "Importe", "Empleado", "Ref.");
            Console.WriteLine("  " + new string('─', Ancho - 4));
            Console.ResetColor();
            foreach (Movimiento m in movimientos)
            {
                Console.Write("  {0,4}  {1,-16}  {2,-20}  ", m.Numero, m.Fecha.ToString("dd/MM/yyyy HH:mm"), Recortar(m.Tipo, 20));
                Console.ForegroundColor = EsIngreso(m) ? ConsoleColor.Green : ConsoleColor.Red;
                Console.Write("{0,14}", (EsIngreso(m) ? "+" : "-") + Dinero(m.Importe));
                Console.ResetColor();
                Console.WriteLine("  {0,-8}  {1,-8}", m.Empleado, m.CuentaReferencia);
            }
            if (movimientos.Count == 0)
            {
                Console.WriteLine("  (la cuenta no tiene movimientos)");
            }
        }

        /// <summary>La acción (INGRESO o SALIDA) viene del servidor, de la tabla TipoMovimiento.</summary>
        private static bool EsIngreso(Movimiento m) => m.Accion == "INGRESO";

        /// <summary>
        /// Comprobante en un marco doble. El número de movimiento y el titular se piden al servidor (consultarCuenta):
        /// el movimiento recién grabado es el último de la cuenta. Si esa consulta falla, sale sin ellos.
        /// </summary>
        public void MostrarComprobante(TipoOperacion tipo, string cuenta, string destino, decimal importe, decimal saldo)
        {
            Cuenta datos = null;
            try
            {
                datos = ConEspera("Preparando comprobante", () => _controlador.ConsultarCuenta(cuenta));
            }
            catch (ErrorServicioException)
            {
            }

            const int ancho = 58;
            string registrado = tipo == TipoOperacion.Transferencia ? "REGISTRADA" : "REGISTRADO";
            string titulo = " COMPROBANTE ";
            int izquierda = (ancho - 2 - titulo.Length) / 2;
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("   ╔" + new string('═', izquierda) + titulo + new string('═', ancho - 2 - izquierda - titulo.Length) + "╗");
            FilaComprobante($"✔ {Titulo(tipo).ToUpperInvariant()} {registrado}", DateTime.Now.ToString("dd/MM/yyyy HH:mm"), ConsoleColor.White, ancho);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("   ╟" + new string('─', ancho - 2) + "╢");
            if (datos != null)
            {
                FilaComprobante("Movimiento   " + datos.NumeroMovimientos, "", ConsoleColor.Gray, ancho);
            }
            string titular = datos != null ? " · " + datos.Cliente : "";
            FilaComprobante("Cuenta       " + Recortar(cuenta.Substring(0, Math.Min(4, cuenta.Length)) + " " + (cuenta.Length > 4 ? cuenta.Substring(4) : "") + titular, ancho - 18), "", ConsoleColor.Gray, ancho);
            if (destino != null)
            {
                FilaComprobante("Destino      " + destino, "", ConsoleColor.Gray, ancho);
            }
            FilaComprobante("Importe      " + (tipo == TipoOperacion.Deposito ? "+" : "−") + Dinero(importe), "", tipo == TipoOperacion.Deposito ? ConsoleColor.Green : ConsoleColor.Red, ancho);
            FilaComprobante("Nuevo saldo  " + Dinero(saldo), "", ConsoleColor.Yellow, ancho);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("   ╚" + new string('═', ancho - 2) + "╝");
            Console.ResetColor();
        }

        private static void FilaComprobante(string izquierda, string derecha, ConsoleColor color, int ancho)
        {
            int espacio = ancho - 4;
            string texto = izquierda.Length + derecha.Length >= espacio
                ? Recortar(izquierda, espacio - derecha.Length - 1) + " " + derecha
                : izquierda + new string(' ', espacio - izquierda.Length - derecha.Length) + derecha;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("   ║ ");
            Console.ForegroundColor = color;
            Console.Write(texto);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" ║");
        }

        public void MostrarExito(string mensaje)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✔ " + mensaje);
            Console.ResetColor();
        }

        public void MostrarError(string mensaje)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✖ " + mensaje);
            Console.ResetColor();
        }

        public bool PreguntarSiNo(string pregunta)
        {
            Console.WriteLine();
            string r = PedirTexto(pregunta + " (s/n)").Trim().ToLowerInvariant();
            return r == "s" || r == "si" || r == "sí";
        }

        public void Pausa()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("  Presione una tecla para continuar...");
            Console.ResetColor();
            if (Console.IsInputRedirected)
            {
                Console.ReadLine();
                Console.WriteLine();
                return;
            }
            Console.ReadKey(true);
        }

        public static string Titulo(TipoOperacion tipo)
        {
            switch (tipo)
            {
                case TipoOperacion.Deposito: return "Depósito";
                case TipoOperacion.Retiro: return "Retiro";
                default: return "Transferencia";
            }
        }

        /// <summary>Importe con separador de miles y 2 decimales: 6,900.00</summary>
        public static string Dinero(decimal importe)
        {
            return importe.ToString("#,##0.00", CultureInfo.InvariantCulture);
        }

        private static string Recortar(string texto, int largo)
        {
            texto = texto ?? string.Empty;
            return texto.Length <= largo ? texto : texto.Substring(0, largo);
        }

        private static string Ajustar(string texto, int ancho)
        {
            return texto.Length >= ancho ? texto.Substring(0, ancho) : texto.PadRight(ancho);
        }

        private static string Centrar(string texto, int ancho = Ancho)
        {
            if (texto.Length >= ancho) return texto;
            int izquierda = (ancho - texto.Length) / 2;
            return new string(' ', izquierda) + texto + new string(' ', ancho - texto.Length - izquierda);
        }
    }
}
