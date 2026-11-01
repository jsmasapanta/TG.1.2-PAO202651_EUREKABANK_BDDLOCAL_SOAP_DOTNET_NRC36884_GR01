using System.Globalization;
using System.Text.RegularExpressions;
using ec.edu.monster.modelo;

namespace ec.edu.monster.vista;

/// <summary>Cómo se muestran los datos en pantalla. No tiene reglas del banco.</summary>
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

    public static Color ColorDe(TipoOperacion tipo) => tipo switch
    {
        TipoOperacion.Deposito => Color.FromArgb("#198754"),
        TipoOperacion.Retiro => Color.FromArgb("#C0392B"),
        _ => Color.FromArgb("#6F42C1")
    };

    /// <summary>Número de cuenta en dos grupos, como en una tarjeta: 0010 0001.</summary>
    public static string NumeroTarjeta(string cuenta) =>
        cuenta.Length == 8 ? cuenta[..4] + "  " + cuenta[4..] : cuenta;

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

    /// <summary>Host y puerto de la URL, para el indicador (192.168.100.2:8087).</summary>
    public static string Host(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out Uri? u) ? u.Authority : url;

    /// <summary>El servidor manda la acción (INGRESO o SALIDA); aquí solo se elige el color y el signo.</summary>
    public static bool EsIngreso(Movimiento m) => m.Accion == "INGRESO";

    /// <summary>
    /// Convierte lo escrito (punto o coma decimal) al decimal que recibe el servicio; null si no es un número.
    /// Si el importe es cero, negativo o tiene más de 2 decimales lo rechaza el servidor.
    /// </summary>
    public static decimal? LeerImporte(string? texto)
    {
        string limpio = (texto ?? "").Trim().Replace(',', '.');
        return decimal.TryParse(limpio, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal importe) ? importe : null;
    }
}
