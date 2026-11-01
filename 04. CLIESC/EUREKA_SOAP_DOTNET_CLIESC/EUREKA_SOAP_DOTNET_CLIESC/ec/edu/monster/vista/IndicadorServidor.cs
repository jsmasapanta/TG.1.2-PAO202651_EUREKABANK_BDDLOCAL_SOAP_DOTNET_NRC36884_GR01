using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ec.edu.monster.vista
{
    /// <summary>Píldora "● Servidor activo · host" (verde) o "● Sin respuesta" (roja), como en el panel del servidor.</summary>
    public class IndicadorServidor : Control
    {
        private bool? _activo;
        private string _host = string.Empty;

        public IndicadorServidor()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = Tema.Fuente(8.5f, FontStyle.Bold);
            Size = new Size(250, 26);
        }

        /// <summary>null = comprobando; true = responde; false = no responde.</summary>
        public void Actualizar(bool? activo, string host)
        {
            _activo = activo;
            _host = host ?? string.Empty;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color fondo, borde, texto, punto;
            string mensaje;
            if (_activo == true)
            {
                fondo = Color.FromArgb(233, 247, 239); borde = Color.FromArgb(183, 228, 199); texto = Color.FromArgb(20, 108, 67); punto = Color.FromArgb(34, 197, 94);
                mensaje = _host.Length == 0 ? "Servidor activo" : "Servidor activo · " + _host;
            }
            else if (_activo == false)
            {
                fondo = Color.FromArgb(253, 236, 234); borde = Color.FromArgb(245, 194, 192); texto = Color.FromArgb(161, 38, 34); punto = Tema.Error;
                mensaje = _host.Length == 0 ? "Sin respuesta" : "Sin respuesta · " + _host;
            }
            else
            {
                fondo = Color.FromArgb(241, 243, 245); borde = Tema.Borde; texto = Tema.TextoSuave; punto = Tema.TextoSuave;
                mensaje = "Comprobando servidor…";
            }

            var area = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath forma = Dibujo.Redondeado(area, Height / 2))
            using (var pincel = new SolidBrush(fondo))
            using (var lapiz = new Pen(borde))
            {
                e.Graphics.FillPath(pincel, forma);
                e.Graphics.DrawPath(lapiz, forma);
            }
            using (var pincel = new SolidBrush(punto))
            {
                e.Graphics.FillEllipse(pincel, 10, Height / 2 - 4, 9, 9);
            }
            TextRenderer.DrawText(e.Graphics, mensaje, Font, new Rectangle(25, 0, Width - 28, Height), texto,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }
    }

    /// <summary>Ayudas de dibujo compartidas por la tarjeta, el gráfico y el comprobante.</summary>
    internal static class Dibujo
    {
        public static GraphicsPath Redondeado(Rectangle r, int radio)
        {
            int d = radio * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
