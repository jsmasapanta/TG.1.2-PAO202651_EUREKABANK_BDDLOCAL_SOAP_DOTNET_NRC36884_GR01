using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using ec.edu.monster.modelo;

namespace ec.edu.monster.vista
{
    /// <summary>
    /// Dos gráficos con los movimientos que ya trajo el servidor: barras con el total por tipo de movimiento
    /// y la línea de cómo cambió el saldo (se reconstruye hacia atrás desde el saldo actual).
    /// </summary>
    public class GraficoCuenta : Control
    {
        private const int Maximo = 15;
        private List<KeyValuePair<string, decimal>> _barras = new List<KeyValuePair<string, decimal>>();
        private Dictionary<string, Color> _colores = new Dictionary<string, Color>();
        private List<decimal> _saldos = new List<decimal>();

        public GraficoCuenta()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            BackColor = Tema.Tarjeta;
        }

        public void Mostrar(Cuenta cuenta, IList<Movimiento> movimientos)
        {
            _barras.Clear();
            _colores.Clear();
            _saldos.Clear();
            if (cuenta != null && movimientos != null && movimientos.Count > 0)
            {
                foreach (var grupo in movimientos.GroupBy(m => m.Tipo).OrderByDescending(g => g.Sum(m => m.Importe)).Take(4))
                {
                    _barras.Add(new KeyValuePair<string, decimal>(grupo.Key, grupo.Sum(m => m.Importe)));
                    _colores[grupo.Key] = grupo.Key.IndexOf("Transf", System.StringComparison.OrdinalIgnoreCase) >= 0 ? Tema.Transferencia
                                        : Formato.EsIngreso(grupo.First()) ? Tema.Ingreso : Tema.Salida;
                }
                decimal saldo = cuenta.Saldo;
                _saldos.Add(saldo);
                foreach (Movimiento m in movimientos.Take(Maximo - 1)) // del más reciente al más antiguo
                {
                    saldo -= Formato.EsIngreso(m) ? m.Importe : -m.Importe;
                    _saldos.Add(saldo);
                }
                _saldos.Reverse();
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var lapiz = new Pen(Tema.Borde))
            {
                g.DrawRectangle(lapiz, 0, 0, Width - 1, Height - 1);
            }
            int mitad = Width / 2;
            using (var lapiz = new Pen(Tema.Borde))
            {
                g.DrawLine(lapiz, mitad, 12, mitad, Height - 12);
            }
            using (Font titulo = Tema.Fuente(9f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, "Total por tipo", titulo, new Point(12, 8), Tema.Texto);
                TextRenderer.DrawText(g, "Evolución del saldo", titulo, new Point(mitad + 12, 8), Tema.Texto);
            }
            if (_barras.Count == 0)
            {
                TextRenderer.DrawText(g, "Consulte una cuenta para ver sus gráficos.", Tema.Fuente(9f), new Rectangle(0, 0, Width, Height), Tema.TextoSuave,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }
            DibujarBarras(g, new Rectangle(14, 34, mitad - 28, Height - 64));
            DibujarLinea(g, new Rectangle(mitad + 16, 34, mitad - 32, Height - 52));
        }

        private void DibujarBarras(Graphics g, Rectangle zona)
        {
            decimal maximo = _barras.Max(b => b.Value);
            int ancho = zona.Width / _barras.Count;
            using (Font chica = Tema.Fuente(7.5f))
            using (Font negrita = Tema.Fuente(7.5f, FontStyle.Bold))
            {
                for (int i = 0; i < _barras.Count; i++)
                {
                    int alto = maximo == 0 ? 2 : (int)(_barras[i].Value / maximo * (zona.Height - 16));
                    var barra = new Rectangle(zona.X + i * ancho + 6, zona.Bottom - alto, ancho - 12, alto);
                    using (var pincel = new SolidBrush(_colores[_barras[i].Key]))
                    {
                        g.FillRectangle(pincel, barra);
                    }
                    TextRenderer.DrawText(g, Formato.Dinero(_barras[i].Value), negrita, new Rectangle(barra.X - 6, barra.Y - 15, barra.Width + 12, 14),
                        Tema.TextoSuave, TextFormatFlags.HorizontalCenter);
                    TextRenderer.DrawText(g, _barras[i].Key, chica, new Rectangle(barra.X - 6, zona.Bottom + 3, barra.Width + 12, 16),
                        Tema.TextoSuave, TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis);
                }
            }
        }

        private void DibujarLinea(Graphics g, Rectangle zona)
        {
            List<decimal> saldos = _saldos.Count == 1 ? new List<decimal> { _saldos[0], _saldos[0] } : _saldos;
            decimal min = saldos.Min(), max = saldos.Max();
            decimal rango = max == min ? 1 : max - min;
            var puntos = saldos.Select((v, i) => new PointF(
                zona.X + zona.Width * i / (float)(saldos.Count - 1),
                zona.Bottom - (float)((v - min) / rango) * (zona.Height - 18))).ToArray();

            var area = new List<PointF>(puntos) { new PointF(zona.Right, zona.Bottom), new PointF(zona.X, zona.Bottom) };
            using (var relleno = new SolidBrush(Color.FromArgb(55, Tema.Sulley)))
            {
                g.FillPolygon(relleno, area.ToArray());
            }
            using (var lapiz = new Pen(Tema.Primario, 2.5f))
            {
                g.DrawLines(lapiz, puntos);
            }
            PointF ultimo = puntos[puntos.Length - 1];
            using (var punto = new SolidBrush(Tema.Mike))
            {
                g.FillEllipse(punto, ultimo.X - 5, ultimo.Y - 5, 10, 10);
            }
            TextRenderer.DrawText(g, "hoy: " + Formato.Dinero(_saldos[_saldos.Count - 1]), Tema.Fuente(7.5f, FontStyle.Bold),
                new Rectangle(zona.X, zona.Bottom + 2, zona.Width, 14), Tema.TextoSuave, TextFormatFlags.Right);
        }
    }
}
