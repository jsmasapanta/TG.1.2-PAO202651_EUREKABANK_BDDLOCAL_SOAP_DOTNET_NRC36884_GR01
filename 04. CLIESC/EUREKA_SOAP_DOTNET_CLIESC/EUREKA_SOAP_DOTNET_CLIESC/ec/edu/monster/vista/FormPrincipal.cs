using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ec.edu.monster.controlador;
using ec.edu.monster.modelo;

namespace ec.edu.monster.vista
{
    /// <summary>
    /// Ventana principal: menú lateral (Movimientos, Depósito, Retiro, Transferencia) y el panel elegido a la derecha.
    /// Si el usuario cierra sesión, devuelve DialogResult.Retry para volver al login.
    /// </summary>
    public class FormPrincipal : Form
    {
        private const string Movimientos = "Movimientos";

        private readonly ControladorEureka _controlador;
        private readonly Panel _contenido;
        private readonly Dictionary<string, Control> _paneles = new Dictionary<string, Control>();
        private readonly Dictionary<string, Button> _botonesMenu = new Dictionary<string, Button>();
        private readonly Dictionary<string, Color> _colores = new Dictionary<string, Color>();

        public FormPrincipal(ControladorEureka controlador)
        {
            _controlador = controlador;

            Text = "EurekaBank · SOAP .NET + SQL Server";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1100, 620);
            BackColor = Tema.Fondo;
            Font = Tema.Fuente(9.5f);
            AutoScaleMode = AutoScaleMode.Dpi;

            // ---------- Menú lateral ----------
            var menu = new Panel { Dock = DockStyle.Left, Width = 230, BackColor = Tema.PrimarioOscuro };
            var marca = Tema.Etiqueta("$ EurekaBank", 16f, FontStyle.Bold, Color.White);
            marca.Location = new Point(22, 18);
            var monster = Tema.Etiqueta("M O N S T E R", 7.5f, FontStyle.Bold, Tema.Mike);
            monster.Location = new Point(26, 48);
            menu.Controls.Add(monster);
            var usuario = Tema.Etiqueta(_controlador.Empleado.Nombre + "\nEmpleado " + _controlador.Empleado.Codigo,
                9.5f, FontStyle.Regular, Color.FromArgb(190, 212, 238));
            usuario.Location = new Point(25, 70);
            menu.Controls.Add(marca);
            menu.Controls.Add(usuario);

            int y = 130;
            AgregarBotonMenu(menu, Movimientos, "≡", Tema.Consulta, ref y, () => new PanelMovimientos(_controlador));
            foreach (TipoOperacion tipo in new[] { TipoOperacion.Deposito, TipoOperacion.Retiro, TipoOperacion.Transferencia })
            {
                TipoOperacion actual = tipo;
                AgregarBotonMenu(menu, Formato.Titulo(tipo), Tema.IconoDe(tipo), Tema.ColorDe(tipo), ref y,
                    () => new PanelOperacion(actual, _controlador));
            }

            var imagen = new PictureBox
            {
                Location = new Point(20, y + 10),
                Size = new Size(190, 160),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = Formato.Monster()
            };
            menu.Controls.Add(imagen);

            var btnSalir = Tema.Boton("Cerrar sesión", Color.FromArgb(160, 50, 40));
            btnSalir.Location = new Point(25, 520);
            btnSalir.Width = 180;
            btnSalir.Click += async (s, e) =>
            {
                btnSalir.Enabled = false;
                await _controlador.CerrarSesionAsync();
                DialogResult = DialogResult.Retry;
                Close();
            };
            menu.Controls.Add(btnSalir);

            // ---------- Barra de estado ----------
            var estado = new StatusStrip { BackColor = Color.White, SizingGrip = false };
            _lblServidor = new ToolStripStatusLabel("●  Comprobando servidor…") { ForeColor = Tema.TextoSuave, Font = Tema.Fuente(9f, FontStyle.Bold) };
            _lblSesion = new ToolStripStatusLabel("") { ForeColor = Tema.TextoSuave, Spring = true, TextAlign = ContentAlignment.MiddleRight };
            estado.Items.Add(_lblServidor);
            estado.Items.Add(new ToolStripStatusLabel("Conectado a: " + _controlador.UrlServidor) { ForeColor = Tema.TextoSuave });
            estado.Items.Add(_lblSesion);

            // Cada 10 s: ¿responde el servidor? y minutos que le quedan al token.
            _reloj = new Timer { Interval = 10000 };
            _reloj.Tick += async (s, e) => await ActualizarEstado();
            Shown += async (s, e) => { _reloj.Start(); await ActualizarEstado(); };
            FormClosed += (s, e) => _reloj.Stop();

            _contenido = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Fondo };

            Controls.Add(_contenido);
            Controls.Add(menu);
            Controls.Add(estado);

            Mostrar(Movimientos);
        }

        private readonly Dictionary<string, System.Func<Control>> _fabricas = new Dictionary<string, System.Func<Control>>();
        private readonly ToolStripStatusLabel _lblServidor;
        private readonly ToolStripStatusLabel _lblSesion;
        private readonly Timer _reloj;

        private async System.Threading.Tasks.Task ActualizarEstado()
        {
            bool activo = await _controlador.ServidorActivoAsync();
            if (IsDisposed) return;
            _lblServidor.Text = activo ? "●  Servidor activo · " + Formato.Host(_controlador.UrlServidor) : "●  Sin respuesta del servidor";
            _lblServidor.ForeColor = activo ? Tema.Ingreso : Tema.Error;
            int? minutos = _controlador.MinutosSesion;
            _lblSesion.Text = minutos == null ? "" : "⏱ Sesión: " + minutos + " min";
            _lblSesion.ForeColor = minutos != null && minutos <= 5 ? Tema.Error : Tema.TextoSuave;
        }

        private void AgregarBotonMenu(Panel menu, string nombre, string icono, Color color, ref int y, System.Func<Control> crear)
        {
            var boton = new Button
            {
                Text = $"   {icono,-3} {nombre}",
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(0, y),
                Size = new Size(230, 50),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Tema.PrimarioOscuro,
                Font = Tema.Fuente(11f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            boton.FlatAppearance.BorderSize = 0;
            boton.FlatAppearance.MouseOverBackColor = Tema.Primario;
            boton.Click += (s, e) => Mostrar(nombre);
            menu.Controls.Add(boton);
            _botonesMenu[nombre] = boton;
            _colores[nombre] = color;
            _fabricas[nombre] = crear;
            y += 54;
        }

        private void Mostrar(string nombre)
        {
            if (!_paneles.TryGetValue(nombre, out Control panel))
            {
                panel = _fabricas[nombre]();
                panel.Dock = DockStyle.Fill;
                _paneles[nombre] = panel;   // se conserva lo escrito al cambiar de opción
            }

            _contenido.Controls.Clear();
            _contenido.Controls.Add(panel);

            foreach (var par in _botonesMenu)
            {
                bool activo = par.Key == nombre;
                par.Value.BackColor = activo ? _colores[par.Key] : Tema.PrimarioOscuro;
                par.Value.FlatAppearance.MouseOverBackColor = activo ? _colores[par.Key] : Tema.Primario;
            }
        }
    }
}
