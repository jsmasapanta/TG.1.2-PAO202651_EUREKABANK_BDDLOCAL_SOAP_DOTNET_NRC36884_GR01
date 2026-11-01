using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Windows.Forms;
using ec.edu.monster.modelo;

namespace ec.edu.monster.vista
{
    /// <summary>
    /// Recibo de la operación: importe, sello APROBADO, movimiento, cuenta, fecha, empleado y nuevo saldo.
    /// "Imprimir / PDF" lo manda a la impresora (o a "Microsoft Print to PDF").
    /// </summary>
    public class FormComprobante : Form
    {
        private readonly Panel _recibo;

        /// <param name="numeroMovimiento">0 si no se pudo consultar.</param>
        public FormComprobante(TipoOperacion tipo, string cuenta, string destino, Cuenta datos, int numeroMovimiento,
                               decimal importe, decimal saldo, string empleado)
        {
            Color color = Tema.ColorDe(tipo);
            string simbolo = Formato.Simbolo(datos?.Moneda);
            Text = "Comprobante · " + Formato.Titulo(tipo);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(380, 462);
            BackColor = Tema.Tarjeta;
            Font = Tema.Fuente(9.5f);
            AutoScaleMode = AutoScaleMode.Dpi;

            _recibo = new Panel { Location = new Point(0, 0), Size = new Size(380, 398), BackColor = Tema.Tarjeta };
            _recibo.Paint += (s, e) => DibujarEncabezado(e.Graphics, color, tipo, importe, simbolo);

            int y = 196;
            void Fila(string etiqueta, string valor, bool grande = false)
            {
                var lbl = Tema.Etiqueta(etiqueta, 9.5f, FontStyle.Regular, Tema.TextoSuave);
                lbl.Location = new Point(24, y);
                var val = new Label
                {
                    Text = valor,
                    Location = new Point(130, y - (grande ? 3 : 0)),
                    Size = new Size(226, grande ? 26 : 20),
                    TextAlign = ContentAlignment.TopRight,
                    Font = Tema.Fuente(grande ? 12.5f : 9.5f, FontStyle.Bold),
                    ForeColor = Tema.Texto,
                    AutoEllipsis = true
                };
                _recibo.Controls.Add(lbl);
                _recibo.Controls.Add(val);
                y += 32;
            }

            if (numeroMovimiento > 0) Fila("Movimiento N.º", numeroMovimiento.ToString());
            Fila(destino != null ? "Cuenta origen" : "Cuenta", Formato.NumeroTarjeta(cuenta) + (datos != null ? " · " + datos.Cliente : ""));
            if (destino != null) Fila("Cuenta destino", Formato.NumeroTarjeta(destino));
            Fila("Fecha y hora", DateTime.Now.ToString("dd/MM/yyyy · HH:mm:ss"));
            Fila("Empleado", empleado);
            Fila("Nuevo saldo", simbolo + " " + Formato.Dinero(saldo), true);

            var btnImprimir = Tema.Boton("🖨  Imprimir / PDF", Tema.Fondo);
            btnImprimir.ForeColor = Tema.Primario;
            btnImprimir.Location = new Point(22, 408);
            btnImprimir.Width = 160;
            btnImprimir.Click += (s, e) => Imprimir();

            var btnCerrar = Tema.Boton("Nueva operación", color);
            btnCerrar.Location = new Point(198, 408);
            btnCerrar.Width = 160;
            btnCerrar.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { _recibo, btnImprimir, btnCerrar });
            AcceptButton = btnCerrar;
        }

        private void DibujarEncabezado(Graphics g, Color color, TipoOperacion tipo, decimal importe, string simbolo)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var fondo = new LinearGradientBrush(new Rectangle(0, 0, 380, 140), color, ControlPaint.Dark(color, 0.25f), 45f))
            {
                g.FillRectangle(fondo, 0, 0, 380, 140);
            }
            g.FillEllipse(Brushes.White, 163, 14, 54, 54);
            TextRenderer.DrawText(g, "✓", Tema.Fuente(20f, FontStyle.Bold), new Rectangle(163, 14, 54, 54), color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            string titulo = Formato.Titulo(tipo) + (tipo == TipoOperacion.Transferencia ? " registrada" : " registrado");
            TextRenderer.DrawText(g, titulo, Tema.Fuente(13f, FontStyle.Bold), new Rectangle(0, 72, 380, 26), Color.White, TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(g, "MONSTER · EUREKA BANK", Tema.Fuente(8f), new Rectangle(0, 98, 380, 18), Color.FromArgb(225, 240, 230), TextFormatFlags.HorizontalCenter);

            var caja = new Rectangle(75, 122, 230, 60);
            using (GraphicsPath forma = Dibujo.Redondeado(caja, 10))
            using (var sombra = new Pen(Tema.Borde))
            {
                g.FillPath(Brushes.White, forma);
                g.DrawPath(sombra, forma);
            }
            TextRenderer.DrawText(g, "IMPORTE", Tema.Fuente(7.5f), new Rectangle(caja.X, caja.Y + 6, caja.Width, 14), Tema.TextoSuave, TextFormatFlags.HorizontalCenter);
            string signo = tipo == TipoOperacion.Deposito ? "+ " : "− ";
            TextRenderer.DrawText(g, signo + simbolo + " " + Formato.Dinero(importe), Tema.Fuente(16f, FontStyle.Bold),
                new Rectangle(caja.X, caja.Y + 20, caja.Width, 34), color, TextFormatFlags.HorizontalCenter);

            // Sello inclinado
            var estado = g.Save();
            g.TranslateTransform(318, 164);
            g.RotateTransform(-14);
            using (var lapiz = new Pen(Color.FromArgb(190, Tema.Ingreso), 2.5f))
            using (var pincel = new SolidBrush(Tema.Ingreso))
            using (Font fuente = Tema.Fuente(9f, FontStyle.Bold))
            using (var centro = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawRectangle(lapiz, -44, -12, 88, 24);
                // DrawString (no TextRenderer) para que el texto también gire con el sello.
                g.DrawString("APROBADO", fuente, pincel, new RectangleF(-44, -12, 88, 24), centro);
            }
            g.Restore(estado);
        }

        private void Imprimir()
        {
            var imagen = new Bitmap(_recibo.Width, _recibo.Height);
            _recibo.DrawToBitmap(imagen, new Rectangle(Point.Empty, _recibo.Size));
            using (var documento = new PrintDocument { DocumentName = "Comprobante EurekaBank" })
            using (var dialogo = new PrintDialog { Document = documento, UseEXDialog = true })
            {
                documento.PrintPage += (s, e) => e.Graphics.DrawImage(imagen, e.MarginBounds.Location);
                if (dialogo.ShowDialog(this) == DialogResult.OK)
                {
                    documento.Print();
                }
            }
            imagen.Dispose();
        }
    }
}
