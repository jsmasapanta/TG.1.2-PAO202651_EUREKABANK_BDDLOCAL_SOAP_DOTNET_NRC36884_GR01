using System;
using System.Drawing;
using System.Windows.Forms;
using ec.edu.monster.controlador;
using ec.edu.monster.modelo;
using ec.edu.monster.servicio;

namespace ec.edu.monster.vista
{
    /// <summary>
    /// Formulario de depósito, retiro o transferencia (el mismo panel; la transferencia muestra además la cuenta de destino).
    /// A la derecha lleva el registro de las operaciones hechas en la sesión.
    /// </summary>
    public class PanelOperacion : Panel
    {
        private readonly TipoOperacion _tipo;
        private readonly ControladorEureka _controlador;
        private readonly TextBox _txtCuenta;
        private readonly TextBox _txtDestino;
        private readonly TextBox _txtImporte;
        private readonly Button _btnRegistrar;
        private readonly Label _lblResultado;
        private readonly Label _lblDetalle;
        private readonly ListBox _lstSesion;

        public PanelOperacion(TipoOperacion tipo, ControladorEureka controlador)
        {
            _tipo = tipo;
            _controlador = controlador;
            BackColor = Tema.Fondo;
            Color color = Tema.ColorDe(tipo);
            bool esTransferencia = tipo == TipoOperacion.Transferencia;

            var titulo = Tema.Etiqueta(Formato.Titulo(tipo), 20f, FontStyle.Bold, color);
            titulo.Location = new Point(30, 20);
            var ayuda = Tema.Etiqueta(Ayuda(tipo), 9.5f, FontStyle.Regular, Tema.TextoSuave);
            ayuda.Location = new Point(32, 62);

            var tarjeta = new Panel { Location = new Point(32, 96), Size = new Size(470, 440), BackColor = Tema.Tarjeta, BorderStyle = BorderStyle.FixedSingle };

            int y = 18;
            var lblCuenta = Tema.Etiqueta(esTransferencia ? "Cuenta de origen" : "Cuenta", 9.5f, FontStyle.Bold);
            lblCuenta.Location = new Point(22, y);
            _txtCuenta = Tema.CajaTexto(22, y + 22, 420);
            _txtCuenta.MaxLength = 8;
            tarjeta.Controls.Add(lblCuenta);
            tarjeta.Controls.Add(_txtCuenta);
            y += 66;

            if (esTransferencia)
            {
                var lblDestino = Tema.Etiqueta("Cuenta de destino", 9.5f, FontStyle.Bold);
                lblDestino.Location = new Point(22, y);
                _txtDestino = Tema.CajaTexto(22, y + 22, 420);
                _txtDestino.MaxLength = 8;
                tarjeta.Controls.Add(lblDestino);
                tarjeta.Controls.Add(_txtDestino);
                y += 66;
            }

            var lblImporte = Tema.Etiqueta("Importe", 9.5f, FontStyle.Bold);
            lblImporte.Location = new Point(22, y);
            _txtImporte = Tema.CajaTexto(22, y + 22, 420);
            tarjeta.Controls.Add(lblImporte);
            tarjeta.Controls.Add(_txtImporte);
            y += 72;

            _btnRegistrar = Tema.Boton("REGISTRAR " + Formato.Titulo(tipo).ToUpperInvariant(), color);
            _btnRegistrar.Location = new Point(22, y);
            _btnRegistrar.Width = 420;
            _btnRegistrar.Click += (s, e) => Registrar();
            tarjeta.Controls.Add(_btnRegistrar);
            y += 56;

            tarjeta.Controls.Add(new Label { Location = new Point(22, y), Size = new Size(420, 1), BackColor = Tema.Borde });
            y += 12;

            _lblResultado = new Label
            {
                Location = new Point(22, y),
                Size = new Size(420, 34),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Tema.Fuente(15f, FontStyle.Bold),
                ForeColor = Tema.Borde,
                Text = "—"
            };
            _lblDetalle = new Label
            {
                Location = new Point(22, y + 36),
                Size = new Size(420, 50),
                TextAlign = ContentAlignment.TopCenter,
                Font = Tema.Fuente(9.5f),
                ForeColor = Tema.TextoSuave
            };
            tarjeta.Controls.Add(_lblResultado);
            tarjeta.Controls.Add(_lblDetalle);

            var lblSesion = Tema.Etiqueta("Registradas en esta sesión", 10f, FontStyle.Bold);
            lblSesion.Location = new Point(522, 96);
            _lstSesion = new ListBox
            {
                Location = new Point(522, 122),
                Size = new Size(310, 414),
                Font = Tema.Fuente(9f),
                BorderStyle = BorderStyle.FixedSingle,
                HorizontalScrollbar = true
            };

            Controls.AddRange(new Control[] { titulo, ayuda, tarjeta, lblSesion, _lstSesion });

            // Enter en cualquier campo registra la operación.
            foreach (TextBox caja in new[] { _txtCuenta, _txtDestino, _txtImporte })
            {
                if (caja == null) continue;
                caja.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.SuppressKeyPress = true;
                        Registrar();
                    }
                };
            }
        }

        private static string Ayuda(TipoOperacion tipo)
        {
            switch (tipo)
            {
                case TipoOperacion.Deposito: return "Suma el importe al saldo de la cuenta.";
                case TipoOperacion.Retiro: return "Resta el importe del saldo; la cuenta debe tener saldo suficiente.";
                default: return "Pasa el importe de una cuenta a otra de la misma moneda.";
            }
        }

        private async void Registrar()
        {
            string cuenta = _txtCuenta.Text.Trim();
            string destino = _txtDestino?.Text.Trim();

            // Las reglas (formato de cuenta, importe, cuentas distintas, saldo) las valida el servidor.
            string error = null;
            if (!Formato.IntentarLeerImporte(_txtImporte.Text, out _))
                error = "Escriba el importe con números (ejemplo: 150 o 75.50).";
            if (error != null)
            {
                MostrarError(error);
                return;
            }

            Formato.IntentarLeerImporte(_txtImporte.Text, out decimal importe);
            string titulo = Formato.Titulo(_tipo);
            string texto = _tipo == TipoOperacion.Transferencia
                ? $"¿Transferir {Formato.Dinero(importe)} de la cuenta {cuenta} a la cuenta {destino}?"
                : $"¿Registrar {titulo.ToLowerInvariant()} de {Formato.Dinero(importe)} en la cuenta {cuenta}?";
            if (MessageBox.Show(this, texto, "Confirmar " + titulo.ToLowerInvariant(), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            _btnRegistrar.Enabled = false;
            UseWaitCursor = true;
            try
            {
                decimal saldo = _tipo == TipoOperacion.Deposito ? await _controlador.RegistrarDepositoAsync(cuenta, importe)
                              : _tipo == TipoOperacion.Retiro ? await _controlador.RegistrarRetiroAsync(cuenta, importe)
                              : await _controlador.RegistrarTransferenciaAsync(cuenta, destino, importe);
                _lblResultado.ForeColor = Tema.ColorDe(_tipo);
                _lblDetalle.ForeColor = Tema.TextoSuave;
                _lblResultado.Text = $"{titulo} registrad{(_tipo == TipoOperacion.Transferencia ? "a" : "o")}";
                _lblDetalle.Text = $"Importe: {Formato.Dinero(importe)}\nNuevo saldo de {cuenta}: {Formato.Dinero(saldo)}";
                string linea = $"{DateTime.Now:HH:mm:ss}  {cuenta}" + (destino != null ? $" → {destino}" : "") +
                               $"  {Formato.Dinero(importe)}  (saldo {Formato.Dinero(saldo)})";
                _lstSesion.Items.Insert(0, linea);
                _txtImporte.Clear();
                await MostrarComprobante(cuenta, destino, importe, saldo);
                _txtImporte.Focus();
            }
            catch (ErrorServicioException ex)
            {
                MostrarError(ex.Message);
            }
            finally
            {
                _btnRegistrar.Enabled = true;
                UseWaitCursor = false;
            }
        }

        /// <summary>
        /// Recibo de la operación. El número de movimiento y el titular se piden al servidor (consultarCuenta):
        /// el movimiento recién grabado es el último de la cuenta. Si esa consulta falla, el recibo sale sin ellos.
        /// </summary>
        private async System.Threading.Tasks.Task MostrarComprobante(string cuenta, string destino, decimal importe, decimal saldo)
        {
            Cuenta datos = null;
            try
            {
                datos = await _controlador.ConsultarCuentaAsync(cuenta);
            }
            catch (ErrorServicioException)
            {
            }
            string empleado = _controlador.Empleado.Codigo + " · " + _controlador.Empleado.Nombre;
            using (var recibo = new FormComprobante(_tipo, cuenta, destino, datos, datos?.NumeroMovimientos ?? 0, importe, saldo, empleado))
            {
                recibo.ShowDialog(FindForm());
            }
        }

        private void MostrarError(string mensaje)
        {
            _lblResultado.ForeColor = Tema.Error;
            _lblResultado.Text = "No se pudo registrar";
            _lblDetalle.ForeColor = Tema.Error;
            _lblDetalle.Text = mensaje;
        }
    }
}
