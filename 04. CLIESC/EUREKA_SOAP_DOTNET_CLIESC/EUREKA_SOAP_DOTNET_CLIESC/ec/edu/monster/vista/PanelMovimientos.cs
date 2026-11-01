using System;
using System.Drawing;
using System.Windows.Forms;
using ec.edu.monster.controlador;
using ec.edu.monster.modelo;
using ec.edu.monster.servicio;

namespace ec.edu.monster.vista
{
    /// <summary>Consulta de una cuenta: datos, saldo y la tabla de movimientos (el más reciente primero).</summary>
    public class PanelMovimientos : Panel
    {
        private readonly ControladorEureka _controlador;
        private readonly TextBox _txtCuenta;
        private readonly Button _btnConsultar;
        private readonly Label _lblMensaje;
        private readonly TarjetaCuenta _tarjeta;
        private readonly GraficoCuenta _grafico;
        private readonly DataGridView _tabla;

        public PanelMovimientos(ControladorEureka controlador)
        {
            _controlador = controlador;
            BackColor = Tema.Fondo;

            var titulo = Tema.Etiqueta("Movimientos de cuenta", 20f, FontStyle.Bold, Tema.Consulta);
            titulo.Location = new Point(30, 20);
            var ayuda = Tema.Etiqueta("Escriba el número de cuenta (8 dígitos) y presione CONSULTAR (o Enter).", 9.5f, FontStyle.Regular, Tema.TextoSuave);
            ayuda.Location = new Point(32, 62);

            var lblCuenta = Tema.Etiqueta("Cuenta", 9.5f, FontStyle.Bold);
            lblCuenta.Location = new Point(32, 96);
            _txtCuenta = Tema.CajaTexto(32, 118, 200);
            _txtCuenta.MaxLength = 8;
            _txtCuenta.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    Consultar();
                }
            };

            _btnConsultar = Tema.Boton("CONSULTAR", Tema.Consulta);
            _btnConsultar.Location = new Point(244, 116);
            _btnConsultar.Width = 140;
            _btnConsultar.Click += (s, e) => Consultar();

            _lblMensaje = new Label { Location = new Point(400, 122), Size = new Size(430, 24), ForeColor = Tema.Error, Font = Tema.Fuente(9.5f) };

            // La cuenta como tarjeta bancaria y, al lado, los gráficos de sus movimientos.
            _tarjeta = new TarjetaCuenta { Location = new Point(32, 158), Size = new Size(390, 176) };
            _grafico = new GraficoCuenta { Location = new Point(436, 158), Size = new Size(396, 176) };

            _tabla = new DataGridView
            {
                Location = new Point(32, 346),
                Size = new Size(800, 220),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = Tema.Fuente(9.5f),
                EnableHeadersVisualStyles = false
            };
            _tabla.ColumnHeadersDefaultCellStyle.BackColor = Tema.Primario;
            _tabla.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _tabla.ColumnHeadersDefaultCellStyle.Font = Tema.Fuente(9.5f, FontStyle.Bold);
            _tabla.Columns.Add("Numero", "Nro");
            _tabla.Columns.Add("Fecha", "Fecha");
            _tabla.Columns.Add("Tipo", "Tipo");
            _tabla.Columns.Add("Accion", "Acción");
            _tabla.Columns.Add("Importe", "Importe");
            _tabla.Columns.Add("Empleado", "Empleado");
            _tabla.Columns.Add("Referencia", "Cuenta ref.");
            _tabla.Columns["Numero"].FillWeight = 40;
            _tabla.Columns["Fecha"].FillWeight = 110;
            _tabla.Columns["Tipo"].FillWeight = 120;
            _tabla.Columns["Importe"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            Controls.AddRange(new Control[] { titulo, ayuda, lblCuenta, _txtCuenta, _btnConsultar, _lblMensaje, _tarjeta, _grafico, _tabla });
        }

        private async void Consultar()
        {
            string cuenta = _txtCuenta.Text.Trim();

            _btnConsultar.Enabled = false;
            _lblMensaje.Text = string.Empty;
            UseWaitCursor = true;
            try
            {
                Cuenta datos = await _controlador.ConsultarCuentaAsync(cuenta);
                Movimiento[] movimientos = await _controlador.TraerMovimientosAsync(cuenta);
                _tarjeta.Mostrar(datos);
                _grafico.Mostrar(datos, movimientos);
                MostrarMovimientos(movimientos);
            }
            catch (ErrorServicioException ex)
            {
                _lblMensaje.Text = ex.Message;
                Limpiar();
            }
            finally
            {
                _btnConsultar.Enabled = true;
                UseWaitCursor = false;
            }
        }

        private void MostrarMovimientos(Movimiento[] movimientos)
        {
            _tabla.Rows.Clear();
            foreach (Movimiento m in movimientos)
            {
                int fila = _tabla.Rows.Add(m.Numero, m.Fecha.ToString("dd/MM/yyyy HH:mm"), m.Tipo, m.Accion,
                    (Formato.EsIngreso(m) ? "+" : "−") + Formato.Dinero(m.Importe), m.Empleado, m.CuentaReferencia);
                _tabla.Rows[fila].Cells["Importe"].Style.ForeColor = Formato.EsIngreso(m) ? Tema.Ingreso : Tema.Salida;
            }
            if (movimientos.Length == 0)
            {
                _lblMensaje.Text = "La cuenta no tiene movimientos.";
            }
        }

        private void Limpiar()
        {
            _tarjeta.Mostrar(null);
            _grafico.Mostrar(null, null);
            _tabla.Rows.Clear();
        }

        /// <summary>Permite que otro panel pida refrescar la consulta (por ejemplo después de un depósito).</summary>
        public void ConsultarCuenta(string cuenta)
        {
            _txtCuenta.Text = cuenta;
            Consultar();
        }
    }
}
