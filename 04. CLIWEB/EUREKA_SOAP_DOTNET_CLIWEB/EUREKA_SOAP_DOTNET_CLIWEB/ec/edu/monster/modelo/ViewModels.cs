namespace ec.edu.monster.modelo;

/// <summary>Datos de la pantalla de login (solo get/set).</summary>
public class LoginViewModel
{
    public string? Usuario { get; set; }
    public string? Clave { get; set; }
    /// <summary>Mensaje del servidor (clave incorrecta con intentos restantes, usuario bloqueado...).</summary>
    public string? Error { get; set; }
}

/// <summary>Datos de la pantalla de movimientos (solo get/set).</summary>
public class MovimientosViewModel
{
    public string? Cuenta { get; set; }
    public Cuenta? Datos { get; set; }
    public IList<Movimiento> Movimientos { get; set; } = new List<Movimiento>();
    public string? Error { get; set; }
}

/// <summary>Datos de la pantalla de depósito, retiro o transferencia (solo get/set).</summary>
public class OperacionViewModel
{
    public TipoOperacion Tipo { get; set; }
    public string? Cuenta { get; set; }
    public string? CuentaDestino { get; set; }
    public string? Importe { get; set; }

    public string? Exito { get; set; }
    public string? Detalle { get; set; }
    public string? Error { get; set; }

    /// <summary>Operaciones registradas en esta sesión del navegador (la más reciente primero).</summary>
    public IList<string> Sesion { get; set; } = new List<string>();

    /// <summary>Recibo de la última operación registrada (se muestra en una ventana sobre la pantalla).</summary>
    public Comprobante? Comprobante { get; set; }
}

/// <summary>Datos del comprobante de una operación (solo get/set).</summary>
public class Comprobante
{
    public TipoOperacion Tipo { get; set; }
    public int NumeroMovimiento { get; set; }
    public string Cuenta { get; set; } = "";
    public string? CuentaDestino { get; set; }
    public string Cliente { get; set; } = "";
    public string Moneda { get; set; } = "";
    public decimal Importe { get; set; }
    public decimal NuevoSaldo { get; set; }
    public DateTime Fecha { get; set; }
    public string Empleado { get; set; } = "";
}
