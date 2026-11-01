using System.Globalization;
using System.Text.RegularExpressions;
using ec.edu.monster.modelo;

namespace ec.edu.monster.vista;

/// <summary>Cómo se muestran los datos en pantalla (formatos, tarjeta, gráficos, intentos). No tiene reglas del banco.</summary>
public static class Formato
{
    /// <summary>Importe con separador de miles y 2 decimales: 6,900.00</summary>
    public static string Dinero(decimal importe) => importe.ToString("#,##0.00", CultureInfo.InvariantCulture);

    public static string Titulo(TipoOperacion tipo) => tipo switch
    {
        TipoOperacion.Deposito => "Depósito",
        TipoOperacion.Retiro => "Retiro",
        _ => "Transferencia"
    };

    /// <summary>El servidor manda la acción (INGRESO o SALIDA); aquí solo se elige el color y el signo.</summary>
    public static bool EsIngreso(Movimiento m) => m.Accion == "INGRESO";

    /// <summary>Número de cuenta en dos grupos, como en una tarjeta: 0010 0001.</summary>
    public static string NumeroTarjeta(string cuenta) =>
        cuenta.Length == 8 ? cuenta[..4] + " " + cuenta[4..] : cuenta;

    /// <summary>Símbolo según la moneda que manda el servidor (Soles, Dólares).</summary>
    public static string Simbolo(string? moneda) =>
        (moneda ?? "").StartsWith("D", StringComparison.OrdinalIgnoreCase) ? "$" : "S/";

    /// <summary>
    /// Intentos que quedan según el mensaje del servidor ("Intentos restantes: 2"). Sin mensaje: los 3.
    /// Solo sirve para dibujar las caritas del login; quien cuenta los intentos es el servidor.
    /// </summary>
    public static int IntentosRestantes(string? mensaje)
    {
        if (string.IsNullOrEmpty(mensaje)) return 3;
        Match m = Regex.Match(mensaje, @"Intentos restantes:\s*(\d+)");
        if (m.Success) return int.Parse(m.Groups[1].Value);
        return mensaje.Contains("bloquead", StringComparison.OrdinalIgnoreCase) ? 0 : 3;
    }

    /// <summary>Barras del gráfico: total por tipo de movimiento (ingresos en verde, salidas en rojo, transferencias en morado).</summary>
    public static IList<(string Tipo, decimal Total, string Color)> Barras(IList<Movimiento> movimientos) =>
        movimientos.GroupBy(m => m.Tipo)
            .Select(g => (Tipo: g.Key, Total: g.Sum(m => m.Importe),
                          Color: g.Key.Contains("Transf", StringComparison.OrdinalIgnoreCase) ? "var(--transferencia)"
                               : EsIngreso(g.First()) ? "var(--deposito)" : "var(--retiro)"))
            .OrderByDescending(b => b.Total)
            .Take(5)
            .ToList();

    /// <summary>
    /// Puntos (x,y) de la línea del saldo en un lienzo de ancho x alto, del movimiento más antiguo al actual.
    /// Se parte del saldo actual y se recorre hacia atrás deshaciendo cada movimiento.
    /// </summary>
    public static string PuntosSaldo(decimal saldoActual, IList<Movimiento> movimientos, int ancho, int alto, int maximo = 15)
    {
        var saldos = new List<decimal> { saldoActual };
        decimal saldo = saldoActual;
        foreach (Movimiento m in movimientos.Take(maximo - 1)) // vienen del más reciente al más antiguo
        {
            saldo -= EsIngreso(m) ? m.Importe : -m.Importe;
            saldos.Add(saldo);
        }
        saldos.Reverse();
        if (saldos.Count == 1) saldos.Insert(0, saldoActual);

        decimal min = saldos.Min(), max = saldos.Max();
        decimal rango = max == min ? 1 : max - min;
        var puntos = saldos.Select((v, i) =>
        {
            double x = 6 + (ancho - 12) * i / (double)(saldos.Count - 1);
            double y = alto - 8 - (double)((v - min) / rango) * (alto - 24);
            return x.ToString("0.#", CultureInfo.InvariantCulture) + "," + y.ToString("0.#", CultureInfo.InvariantCulture);
        });
        return string.Join(" ", puntos);
    }
}
