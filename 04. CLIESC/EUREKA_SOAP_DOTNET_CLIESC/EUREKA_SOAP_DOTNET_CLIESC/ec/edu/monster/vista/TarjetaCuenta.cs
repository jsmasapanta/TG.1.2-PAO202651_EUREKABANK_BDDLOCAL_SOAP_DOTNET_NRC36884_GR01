using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ec.edu.monster.modelo;

namespace ec.edu.monster.vista
{
    /// <summary>La cuenta dibujada como una tarjeta bancaria: chip, número 0010 0001, titular y saldo.</summary>
    public class TarjetaCuenta : Control
    {
        private Cuenta _cuenta;

        public TarjetaCuenta()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        /// <summary>null deja la tarjeta vacía ("Consulte una cuenta").</summary>
        public void Mostrar(Cuenta cuenta)
        {
            _cuenta = cuenta;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            var area = new Rectangle(0, 0, Width - 1, Height - 1);
            bool activa = _cuenta == null || _cuenta.Estado == "ACTIVO";

            using (GraphicsPath forma = Dibujo.Redondeado(area, 16))
            {
                Color a = activa ? Tema.Primario : Color.FromArgb(90, 98, 110);
                Color b = activa ? Tema.PrimarioOscuro : Color.FromArgb(55, 60, 68);
                using (var degradado = new LinearGradientBrush(area, a, b, 35f))
                {
                    g.FillPath(degradado, forma);
                }
                g.SetClip(forma);
                using (var brillo = new SolidBrush(Color.FromArgb(70, Tema.Sulley)))
                {
                    g.FillEllipse(brillo, Width - 190, -90, 260, 200);
                }
                using (var circulo = new SolidBrush(Color.FromArgb(45, Tema.Mike)))
                {
                    g.FillEllipse(circulo, Width - 150, Height - 90, 210, 210);
                }
                g.ResetClip();
            }

            using (var chip = new LinearGradientBrush(new Rectangle(20, 44, 42, 30), Color.FromArgb(245, 215, 122), Color.FromArgb(201, 162, 39), 45f))
            using (GraphicsPath formaChip = Dibujo.Redondeado(new Rectangle(20, 44, 42, 30), 5))
            {
                g.FillPath(chip, formaChip);
            }

            Color suave = Color.FromArgb(215, 230, 245);
            if (_cuenta == null)
            {
                Texto(g, "CUENTA DE AHORROS", 8.5f, FontStyle.Regular, suave, 20, 16);
                Texto(g, "Consulte una cuenta", 15f, FontStyle.Bold, Color.White, 20, 88);
                Texto(g, "MONSTER · EUREKA BANK", 9f, FontStyle.Bold, Color.White, Width - 190, Height - 30);
                return;
            }

            Texto(g, "CUENTA DE AHORROS · " + _cuenta.Moneda.ToUpperInvariant(), 8.5f, FontStyle.Regular, suave, 20, 16);
            using (var fuente = new Font("Consolas", 19f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, Formato.NumeroTarjeta(_cuenta.Codigo).Replace(" ", "  "), fuente, new Point(18, 78), Color.White);
            }
            Texto(g, "TITULAR", 7.5f, FontStyle.Regular, suave, 20, 110);
            Texto(g, _cuenta.Cliente.ToUpperInvariant(), 10.5f, FontStyle.Bold, Color.White, 20, 123);

            var saldo = Formato.Simbolo(_cuenta.Moneda) + " " + Formato.Dinero(_cuenta.Saldo);
            using (var fuente = Tema.Fuente(20f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, "SALDO DISPONIBLE", Tema.Fuente(7.5f), new Rectangle(0, 14, Width - 20, 16), suave, TextFormatFlags.Right);
                TextRenderer.DrawText(g, saldo, fuente, new Rectangle(0, 28, Width - 18, 40), Color.White, TextFormatFlags.Right);
            }
            Texto(g, $"{_cuenta.Sucursal} · {_cuenta.Estado} · desde {_cuenta.FechaCreacion:MM/yyyy}", 7.5f, FontStyle.Regular, suave, 20, Height - 26);
            TextRenderer.DrawText(g, "MONSTER · EUREKA BANK", Tema.Fuente(8.5f, FontStyle.Bold), new Rectangle(0, Height - 28, Width - 18, 20), Color.White, TextFormatFlags.Right);
        }

        private static void Texto(Graphics g, string texto, float tamano, FontStyle estilo, Color color, int x, int y)
        {
            using (Font fuente = Tema.Fuente(tamano, estilo))
            {
                TextRenderer.DrawText(g, texto, fuente, new Point(x, y), color);
            }
        }
    }
}
