using ec.edu.monster.modelo;

namespace ec.edu.monster.vista;

/// <summary>
/// Dibujo para un GraphicsView: barras con el total por tipo de movimiento y la línea de cómo cambió el saldo
/// (se reconstruye hacia atrás desde el saldo actual). Solo usa los datos que ya trajo el servidor.
/// </summary>
public class GraficoCuenta : IDrawable
{
    private const int Maximo = 15;
    private readonly List<(string Tipo, decimal Total, Color Color)> _barras = new();
    private readonly List<decimal> _saldos = new();

    public void Mostrar(Cuenta cuenta, IList<Movimiento> movimientos)
    {
        _barras.Clear();
        _saldos.Clear();
        foreach (var g in movimientos.GroupBy(m => m.Tipo).OrderByDescending(g => g.Sum(m => m.Importe)).Take(3))
        {
            Color color = g.Key.Contains("Transf", StringComparison.OrdinalIgnoreCase) ? Color.FromArgb("#6F42C1")
                        : Formato.EsIngreso(g.First()) ? Color.FromArgb("#198754") : Color.FromArgb("#C0392B");
            _barras.Add((g.Key, g.Sum(m => m.Importe), color));
        }
        decimal saldo = cuenta.Saldo;
        _saldos.Add(saldo);
        foreach (Movimiento m in movimientos.Take(Maximo - 1)) // del más reciente al más antiguo
        {
            saldo -= Formato.EsIngreso(m) ? m.Importe : -m.Importe;
            _saldos.Add(saldo);
        }
        _saldos.Reverse();
        if (_saldos.Count == 1) _saldos.Insert(0, _saldos[0]);
    }

    public void Draw(ICanvas canvas, RectF zona)
    {
        if (_barras.Count == 0) return;
        Color suave = Tema.Recurso("TextoSuave");
        Color texto = Tema.Recurso("Texto");
        float mitad = zona.Width / 2;

        canvas.FontSize = 12;
        canvas.FontColor = texto;
        canvas.DrawString("Total por tipo", 4, 2, mitad, 16, HorizontalAlignment.Left, VerticalAlignment.Top);
        canvas.DrawString("Saldo", mitad + 10, 2, mitad, 16, HorizontalAlignment.Left, VerticalAlignment.Top);

        // Barras
        float baseY = zona.Height - 18, alto = baseY - 40;
        decimal maximo = _barras.Max(b => b.Total);
        float ancho = (mitad - 12) / _barras.Count;
        canvas.FontSize = 9;
        for (int i = 0; i < _barras.Count; i++)
        {
            float h = maximo == 0 ? 2 : (float)(_barras[i].Total / maximo) * alto;
            float x = 4 + i * ancho + 4;
            canvas.FillColor = _barras[i].Color;
            canvas.FillRoundedRectangle(x, baseY - h, ancho - 8, h, 4);
            canvas.FontColor = suave;
            canvas.DrawString(Formato.Dinero(_barras[i].Total), x - 4, baseY - h - 14, ancho, 12, HorizontalAlignment.Center, VerticalAlignment.Top);
            string nombre = _barras[i].Tipo.Length > 9 ? _barras[i].Tipo[..9] + "…" : _barras[i].Tipo;
            canvas.DrawString(nombre, x - 4, baseY + 3, ancho, 12, HorizontalAlignment.Center, VerticalAlignment.Top);
        }

        // Línea del saldo
        float x0 = mitad + 12, x1 = zona.Width - 6, y0 = 26, y1 = baseY;
        decimal min = _saldos.Min(), max = _saldos.Max();
        decimal rango = max == min ? 1 : max - min;
        var puntos = _saldos.Select((v, i) => new PointF(
            x0 + (x1 - x0) * i / (_saldos.Count - 1),
            y1 - (float)((v - min) / rango) * (y1 - y0 - 6))).ToList();

        var area = new PathF();
        area.MoveTo(puntos[0].X, y1);
        foreach (PointF p in puntos) area.LineTo(p);
        area.LineTo(puntos[^1].X, y1);
        area.Close();
        canvas.FillColor = Color.FromRgba(41, 168, 224, 60);
        canvas.FillPath(area);

        canvas.StrokeColor = Color.FromArgb("#29A8E0");
        canvas.StrokeSize = 2.5f;
        for (int i = 1; i < puntos.Count; i++) canvas.DrawLine(puntos[i - 1], puntos[i]);
        canvas.FillColor = Color.FromArgb("#7AC943");
        canvas.FillCircle(puntos[^1], 5);
        canvas.FontColor = suave;
        canvas.DrawString("hoy " + Formato.Dinero(_saldos[^1]), x0, baseY + 3, x1 - x0, 12, HorizontalAlignment.Right, VerticalAlignment.Top);
    }
}
