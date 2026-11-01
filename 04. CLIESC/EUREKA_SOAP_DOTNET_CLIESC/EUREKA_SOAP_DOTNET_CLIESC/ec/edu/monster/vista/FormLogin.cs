using System;
using System.Drawing;
using System.Windows.Forms;
using ec.edu.monster.controlador;
using ec.edu.monster.servicio;

namespace ec.edu.monster.vista
{
    /// <summary>
    /// Ventana de inicio de sesión. Devuelve DialogResult.OK si el servidor acepta las credenciales.
    /// No cuenta intentos: el servidor los controla y su mensaje se muestra tal cual.
    /// </summary>
    public class FormLogin : Form
    {
        private const int X = 400; // el formulario va a la derecha de la imagen

        private readonly ControladorEureka _controlador;
        private readonly TextBox _txtUsuario;
        private readonly TextBox _txtClave;
        private readonly Button _btnIngresar;
        private readonly Label _lblMensaje;
        private readonly Label[] _caritas = new Label[3];
        private readonly IndicadorServidor _indicador;
        private readonly TextBox _txtHost;
        private readonly TextBox _txtPuerto;

        public FormLogin(ControladorEureka controlador)
        {
            _controlador = controlador;

            Text = "EurekaBank · Iniciar sesión";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(X + 420, 510);
            BackColor = Tema.Fondo;
            Font = Tema.Fuente(9.5f);
            AutoScaleMode = AutoScaleMode.Dpi;

            var imagen = new PictureBox
            {
                Location = new Point(0, 0),
                Size = new Size(X, 510),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Tema.PrimarioOscuro,
                Image = Formato.Monster()
            };

            var encabezado = new Panel { Location = new Point(X, 0), Size = new Size(420, 130), BackColor = Tema.Primario };
            var icono = Tema.Etiqueta("$", 30f, FontStyle.Bold, Color.White);
            icono.Location = new Point(32, 30);
            var titulo = Tema.Etiqueta("EurekaBank", 18f, FontStyle.Bold, Color.White);
            titulo.Location = new Point(82, 32);
            var subtitulo = Tema.Etiqueta("Cliente de escritorio · SOAP .NET + SQL Server", 9.5f, FontStyle.Regular, Color.FromArgb(200, 222, 245));
            subtitulo.Location = new Point(85, 72);
            encabezado.Controls.AddRange(new Control[] { icono, titulo, subtitulo });

            var lblUsuario = Tema.Etiqueta("Usuario", 9.5f, FontStyle.Bold);
            lblUsuario.Location = new Point(X + 40, 160);
            _txtUsuario = new TextBox { Location = new Point(X + 40, 184), Width = 340, Font = Tema.Fuente(11f) };

            var lblClave = Tema.Etiqueta("Clave", 9.5f, FontStyle.Bold);
            lblClave.Location = new Point(X + 40, 226);
            _txtClave = new TextBox { Location = new Point(X + 40, 250), Width = 340, Font = Tema.Fuente(11f), UseSystemPasswordChar = true };

            var chkMostrar = new CheckBox { Text = "Mostrar clave", Location = new Point(X + 40, 284), AutoSize = true, ForeColor = Tema.TextoSuave };
            chkMostrar.CheckedChanged += (s, e) => _txtClave.UseSystemPasswordChar = !chkMostrar.Checked;

            _btnIngresar = Tema.Boton("INGRESAR", Tema.Primario);
            _btnIngresar.Location = new Point(X + 40, 318);
            _btnIngresar.Width = 340;
            _btnIngresar.Click += BtnIngresar_Click;

            _lblMensaje = new Label
            {
                Location = new Point(X + 40, 362),
                Size = new Size(340, 36),
                ForeColor = Tema.Error,
                Font = Tema.Fuente(9f, FontStyle.Bold),
                TextAlign = ContentAlignment.TopCenter
            };

            // Intentos: 3 caritas que se apagan según lo que diga el servidor ("Intentos restantes: N").
            var lblIntentos = Tema.Etiqueta("Intentos:", 9f, FontStyle.Regular, Tema.TextoSuave);
            lblIntentos.Location = new Point(X + 40, 406);
            for (int i = 0; i < 3; i++)
            {
                _caritas[i] = new Label
                {
                    Text = "👾",
                    Font = new Font("Segoe UI Emoji", 11f),
                    Location = new Point(X + 102 + i * 34, 400),
                    Size = new Size(30, 30),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                Controls.Add(_caritas[i]);
            }
            PintarIntentos(3);
            _indicador = new IndicadorServidor { Location = new Point(X + 214, 402), Size = new Size(166, 26) };

            // Cambiar de servidor desde aquí: la IP de la PC del servidor varía según la red (casa, aula, guardia).
            var lblServidor = Tema.Etiqueta("Servidor", 9f, FontStyle.Bold, Tema.TextoSuave);
            lblServidor.Location = new Point(X + 40, 450);
            _txtHost = new TextBox { Location = new Point(X + 106, 446), Width = 140, Font = Tema.Fuente(10f), Text = _controlador.HostServidor };
            var dosPuntos = Tema.Etiqueta(":", 10f, FontStyle.Bold, Tema.TextoSuave);
            dosPuntos.Location = new Point(X + 248, 448);
            _txtPuerto = new TextBox { Location = new Point(X + 260, 446), Width = 50, Font = Tema.Fuente(10f), Text = _controlador.PuertoServidor };
            var btnCambiar = Tema.Boton("Cambiar", Tema.Sulley);
            btnCambiar.Location = new Point(X + 316, 445);
            btnCambiar.Size = new Size(64, 28);
            btnCambiar.Font = Tema.Fuente(8.5f, FontStyle.Bold);
            btnCambiar.Click += async (s, e) => await CambiarServidor();
            Controls.AddRange(new Control[] { _txtHost, dosPuntos, _txtPuerto, btnCambiar });

            Controls.AddRange(new Control[] { imagen, encabezado, lblUsuario, _txtUsuario, lblClave, _txtClave, chkMostrar, _btnIngresar, _lblMensaje, lblIntentos, _indicador, lblServidor });
            AcceptButton = _btnIngresar;
            Shown += async (s, e) =>
            {
                _txtUsuario.Focus();
                if (_controlador.ErrorConfiguracion != null)
                {
                    _lblMensaje.ForeColor = Tema.Error;
                    _lblMensaje.Text = "✖ " + _controlador.ErrorConfiguracion + " Corríjalo abajo en Servidor.";
                }
                bool activo = await _controlador.ServidorActivoAsync();
                if (!IsDisposed) _indicador.Actualizar(activo, string.Empty); // la URL ya se ve abajo
            };
        }

        private async System.Threading.Tasks.Task CambiarServidor()
        {
            try
            {
                _controlador.CambiarServidor(_txtHost.Text, _txtPuerto.Text);
                _lblMensaje.ForeColor = Tema.Ingreso;
                _lblMensaje.Text = "Servidor cambiado a " + _controlador.UrlServidor;
            }
            catch (InvalidOperationException ex)
            {
                _lblMensaje.ForeColor = Tema.Error;
                _lblMensaje.Text = "✖ " + ex.Message;
                return;
            }
            catch (System.Configuration.ConfigurationErrorsException ex)
            {
                _lblMensaje.ForeColor = Tema.Error;
                _lblMensaje.Text = "✖ No se pudo guardar en el .exe.config: " + ex.Message;
                return;
            }
            _indicador.Actualizar(null, string.Empty);
            bool activo = await _controlador.ServidorActivoAsync();
            if (!IsDisposed) _indicador.Actualizar(activo, string.Empty);
        }

        private void PintarIntentos(int restantes)
        {
            for (int i = 0; i < 3; i++)
            {
                bool viva = i < restantes;
                _caritas[i].BackColor = viva ? Color.FromArgb(232, 246, 220) : Color.FromArgb(235, 235, 235);
                _caritas[i].ForeColor = viva ? Tema.Boo : Color.Silver;
            }
        }

        private async void BtnIngresar_Click(object sender, EventArgs e)
        {
            CambiarEstadoEspera(true);
            try
            {
                await _controlador.IniciarSesionAsync(_txtUsuario.Text, _txtClave.Text);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (ErrorServicioException ex)
            {
                // Mensaje del servidor: clave incorrecta con intentos restantes, usuario bloqueado o falla de conexión.
                _lblMensaje.ForeColor = Tema.Error;
                _lblMensaje.Text = "✖ " + ex.Message;
                PintarIntentos(Formato.IntentosRestantes(ex.Message));
                _txtClave.Clear();
                _txtClave.Focus();
            }
            finally
            {
                if (!IsDisposed)
                {
                    CambiarEstadoEspera(false);
                }
            }
        }

        private void CambiarEstadoEspera(bool esperando)
        {
            _btnIngresar.Enabled = !esperando;
            _btnIngresar.Text = esperando ? "CONECTANDO..." : "INGRESAR";
            UseWaitCursor = esperando;
            if (esperando)
            {
                _lblMensaje.Text = string.Empty;
            }
        }
    }
}
