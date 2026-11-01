using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using ec.edu.monster.db;
using ec.edu.monster.modelo;

namespace ec.edu.monster.servicio
{
    /// <summary>
    /// Regla del banco que no se cumple (saldo insuficiente, cuenta cancelada, credenciales...). El mensaje va al usuario
    /// y el código viaja en ErrorServicio.Codigo para que el cliente sepa qué pasó.
    /// </summary>
    public class ReglaNegocioException : Exception
    {
        public ReglaNegocioException(string mensaje, string codigo = "REGLA_NEGOCIO") : base(mensaje)
        {
            Codigo = codigo;
        }

        public string Codigo { get; }
    }

    /// <summary>
    /// Lógica de negocio de EurekaBank sobre SQL Server (no sabe nada de SOAP).
    /// Patrón Singleton: no guarda estado propio, así que una sola instancia atiende todas las peticiones.
    /// Depósito, retiro y transferencia se hacen dentro de una transacción: o se graba todo o nada.
    /// </summary>
    public sealed class EurekaServicio
    {
        private const decimal ImporteMaximo = 1000000m;

        private static readonly Lazy<EurekaServicio> _instancia =
            new Lazy<EurekaServicio>(() => new EurekaServicio(ConexionBD.Instancia));

        public static EurekaServicio Instancia => _instancia.Value;

        private readonly ConexionBD _bd;

        private EurekaServicio(ConexionBD bd)
        {
            _bd = bd;
        }

        // ---------- Autenticación ----------

        private const int IntentosMaximos = 3;
        private static readonly TimeSpan TiempoBloqueo = TimeSpan.FromMinutes(2);

        /// <summary>Intentos fallidos por usuario (en minúsculas). Los clientes no cuentan nada: lo hace el servidor.</summary>
        private readonly Dictionary<string, ControlIntentos> _intentos = new Dictionary<string, ControlIntentos>();

        private class ControlIntentos
        {
            public int Fallidos;
            public DateTime? BloqueadoHasta;
        }

        /// <summary>
        /// Devuelve el empleado si usuario y clave son correctos. Si no, lanza ReglaNegocioException con el
        /// mensaje para el usuario. Después de 3 intentos fallidos el usuario queda bloqueado 2 minutos.
        /// </summary>
        public Empleado Login(string usuario, string clave)
        {
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrEmpty(clave))
            {
                throw new ArgumentException("Ingrese usuario y clave.");
            }
            string llave = usuario.Trim().ToLowerInvariant();

            lock (_intentos)
            {
                if (_intentos.TryGetValue(llave, out ControlIntentos control) && control.BloqueadoHasta > DateTime.Now)
                {
                    int segundos = (int)Math.Ceiling((control.BloqueadoHasta.Value - DateTime.Now).TotalSeconds);
                    throw new ReglaNegocioException(
                        "Cuenta bloqueada por superar " + IntentosMaximos + " intentos. Intentos restantes: 0. Intente de nuevo en " + segundos + " segundos.",
                        "USUARIO_BLOQUEADO");
                }
            }

            Empleado empleado = BuscarEmpleado(usuario.Trim(), clave);

            lock (_intentos)
            {
                if (empleado != null)
                {
                    _intentos.Remove(llave);
                    // El token va en el header SOAP <Token> de las demás operaciones (dura 30 minutos).
                    empleado.Token = GestorSesiones.Instancia.Crear();
                    return empleado;
                }

                if (!_intentos.TryGetValue(llave, out ControlIntentos control) || control.BloqueadoHasta != null)
                {
                    control = new ControlIntentos();
                    _intentos[llave] = control;
                }
                control.Fallidos++;
                if (control.Fallidos >= IntentosMaximos)
                {
                    control.BloqueadoHasta = DateTime.Now.Add(TiempoBloqueo);
                    throw new ReglaNegocioException(
                        "Cuenta bloqueada: se superaron los " + IntentosMaximos + " intentos. Intentos restantes: 0. Espere " + TiempoBloqueo.TotalMinutes + " minutos.",
                        "USUARIO_BLOQUEADO");
                }
                throw new ReglaNegocioException(
                    "Credenciales inválidas. Intentos restantes: " + (IntentosMaximos - control.Fallidos) + ".",
                    "CREDENCIALES_INVALIDAS");
            }
        }

        /// <summary>Busca el empleado por usuario y compara la clave. Devuelve null si no coinciden.</summary>
        private Empleado BuscarEmpleado(string usuario, string clave)
        {
            const string sql = @"
                SELECT chr_emplcodigo, vch_emplusuario, vch_emplclave,
                       vch_emplnombre + ' ' + vch_emplpaterno AS nombre
                FROM Empleado
                WHERE vch_emplusuario = @usuario";

            using (SqlConnection cn = _bd.AbrirConexion())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@usuario", SqlDbType.VarChar, 15).Value = usuario;
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (!dr.Read())
                    {
                        return null;
                    }
                    // El usuario no distingue mayúsculas (lo resuelve SQL Server); la clave sí.
                    if (!string.Equals(dr.GetString(2), clave, StringComparison.Ordinal))
                    {
                        return null;
                    }
                    return new Empleado
                    {
                        Codigo = dr.GetString(0),
                        Usuario = dr.GetString(1),
                        Nombre = dr.GetString(3)
                    };
                }
            }
        }

        // ---------- Consultas ----------

        public Cuenta ConsultarCuenta(string cuenta)
        {
            cuenta = Validacion.Cuenta(cuenta, "cuenta");

            const string sql = @"
                SELECT c.chr_cuencodigo,
                       cl.vch_clienombre + ' ' + cl.vch_cliepaterno + ' ' + cl.vch_cliematerno AS cliente,
                       m.vch_monedescripcion, s.vch_sucunombre, c.dec_cuensaldo, c.vch_cuenestado,
                       c.dtt_cuenfechacreacion, c.int_cuencontmov
                FROM Cuenta c
                JOIN Cliente cl ON cl.chr_cliecodigo = c.chr_cliecodigo
                JOIN Moneda m ON m.chr_monecodigo = c.chr_monecodigo
                JOIN Sucursal s ON s.chr_sucucodigo = c.chr_sucucodigo
                WHERE c.chr_cuencodigo = @cuenta";

            using (SqlConnection cn = _bd.AbrirConexion())
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@cuenta", SqlDbType.Char, 8).Value = cuenta;
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (!dr.Read())
                    {
                        throw new ReglaNegocioException("La cuenta " + cuenta + " no existe.");
                    }
                    return new Cuenta
                    {
                        Codigo = dr.GetString(0),
                        Cliente = dr.GetString(1),
                        Moneda = dr.GetString(2),
                        Sucursal = dr.GetString(3),
                        Saldo = dr.GetDecimal(4),
                        Estado = dr.GetString(5),
                        FechaCreacion = dr.GetDateTime(6),
                        NumeroMovimientos = dr.GetInt32(7)
                    };
                }
            }
        }

        /// <summary>Movimientos de la cuenta, el más reciente primero.</summary>
        public List<Movimiento> TraerMovimientos(string cuenta)
        {
            cuenta = Validacion.Cuenta(cuenta, "cuenta");

            const string sql = @"
                SELECT mv.int_movinumero, mv.dtt_movifecha, t.vch_tipodescripcion, t.vch_tipoaccion,
                       mv.dec_moviimporte, mv.chr_emplcodigo, mv.chr_cuenreferencia
                FROM Movimiento mv
                JOIN TipoMovimiento t ON t.chr_tipocodigo = mv.chr_tipocodigo
                WHERE mv.chr_cuencodigo = @cuenta
                ORDER BY mv.int_movinumero DESC";

            using (SqlConnection cn = _bd.AbrirConexion())
            {
                if (!ExisteCuenta(cn, cuenta))
                {
                    throw new ReglaNegocioException("La cuenta " + cuenta + " no existe.");
                }

                var lista = new List<Movimiento>();
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add("@cuenta", SqlDbType.Char, 8).Value = cuenta;
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new Movimiento
                            {
                                Numero = dr.GetInt32(0),
                                Fecha = dr.GetDateTime(1),
                                Tipo = dr.GetString(2),
                                Accion = dr.GetString(3),
                                Importe = dr.GetDecimal(4),
                                Empleado = dr.GetString(5),
                                CuentaReferencia = dr.IsDBNull(6) ? string.Empty : dr.GetString(6)
                            });
                        }
                    }
                }
                return lista;
            }
        }

        // ---------- Transacciones ----------

        /// <summary>Suma el importe al saldo y registra el movimiento 003. Devuelve el nuevo saldo.</summary>
        public decimal RegistrarDeposito(string cuenta, decimal importe, string codEmpleado)
        {
            cuenta = Validacion.Cuenta(cuenta, "cuenta");
            Validacion.Importe(importe, ImporteMaximo);
            codEmpleado = Validacion.Empleado(codEmpleado);

            return EnTransaccion((cn, tx) =>
            {
                ValidarEmpleado(cn, tx, codEmpleado);
                EstadoCuenta estado = LeerCuentaBloqueada(cn, tx, cuenta);
                return Aplicar(cn, tx, estado, importe, Constantes.TipoDeposito, codEmpleado, null);
            });
        }

        /// <summary>Resta el importe (si alcanza el saldo) y registra el movimiento 004. Devuelve el nuevo saldo.</summary>
        public decimal RegistrarRetiro(string cuenta, decimal importe, string codEmpleado)
        {
            cuenta = Validacion.Cuenta(cuenta, "cuenta");
            Validacion.Importe(importe, ImporteMaximo);
            codEmpleado = Validacion.Empleado(codEmpleado);

            return EnTransaccion((cn, tx) =>
            {
                ValidarEmpleado(cn, tx, codEmpleado);
                EstadoCuenta estado = LeerCuentaBloqueada(cn, tx, cuenta);
                return Aplicar(cn, tx, estado, -importe, Constantes.TipoRetiro, codEmpleado, null);
            });
        }

        /// <summary>
        /// Pasa el importe de una cuenta a otra de la misma moneda: movimiento 009 (salida) en el origen
        /// y 008 (ingreso) en el destino, cada uno con la otra cuenta como referencia. Devuelve el saldo del origen.
        /// </summary>
        public decimal RegistrarTransferencia(string cuentaOrigen, string cuentaDestino, decimal importe, string codEmpleado)
        {
            cuentaOrigen = Validacion.Cuenta(cuentaOrigen, "cuenta de origen");
            cuentaDestino = Validacion.Cuenta(cuentaDestino, "cuenta de destino");
            if (cuentaOrigen == cuentaDestino)
            {
                throw new ArgumentException("La cuenta de origen y la de destino deben ser distintas.");
            }
            Validacion.Importe(importe, ImporteMaximo);
            codEmpleado = Validacion.Empleado(codEmpleado);

            return EnTransaccion((cn, tx) =>
            {
                ValidarEmpleado(cn, tx, codEmpleado);

                // Se bloquean las dos cuentas siempre en el mismo orden (por código) para evitar
                // un bloqueo mutuo si dos personas transfieren entre las mismas cuentas en sentido contrario.
                string primera = string.CompareOrdinal(cuentaOrigen, cuentaDestino) < 0 ? cuentaOrigen : cuentaDestino;
                string segunda = primera == cuentaOrigen ? cuentaDestino : cuentaOrigen;
                EstadoCuenta a = LeerCuentaBloqueada(cn, tx, primera);
                EstadoCuenta b = LeerCuentaBloqueada(cn, tx, segunda);
                EstadoCuenta origen = a.Codigo == cuentaOrigen ? a : b;
                EstadoCuenta destino = a.Codigo == cuentaOrigen ? b : a;

                if (origen.Moneda != destino.Moneda)
                {
                    throw new ReglaNegocioException("Solo se puede transferir entre cuentas de la misma moneda.");
                }

                decimal saldoOrigen = Aplicar(cn, tx, origen, -importe, Constantes.TipoTransferenciaSalida, codEmpleado, cuentaDestino);
                Aplicar(cn, tx, destino, importe, Constantes.TipoTransferenciaIngreso, codEmpleado, cuentaOrigen);
                return saldoOrigen;
            });
        }

        // ---------- Apoyo ----------

        /// <summary>Importe con separador de miles y 2 decimales (6,900.00), igual que en los clientes.</summary>
        internal static string Dinero(decimal importe)
        {
            return importe.ToString("#,##0.00", CultureInfo.InvariantCulture);
        }

        /// <summary>Lo que hace falta de una cuenta para mover dinero.</summary>
        private class EstadoCuenta
        {
            public string Codigo;
            public string Moneda;
            public decimal Saldo;
            public int ContadorMovimientos;
        }

        private T EnTransaccion<T>(Func<SqlConnection, SqlTransaction, T> trabajo)
        {
            using (SqlConnection cn = _bd.AbrirConexion())
            using (SqlTransaction tx = cn.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                try
                {
                    T resultado = trabajo(cn, tx);
                    tx.Commit();
                    return resultado;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        /// <summary>Lee la cuenta con UPDLOCK: nadie más puede modificarla hasta que termine la transacción.</summary>
        private static EstadoCuenta LeerCuentaBloqueada(SqlConnection cn, SqlTransaction tx, string cuenta)
        {
            const string sql = @"
                SELECT chr_monecodigo, dec_cuensaldo, int_cuencontmov, vch_cuenestado
                FROM Cuenta WITH (UPDLOCK, ROWLOCK)
                WHERE chr_cuencodigo = @cuenta";

            using (var cmd = new SqlCommand(sql, cn, tx))
            {
                cmd.Parameters.Add("@cuenta", SqlDbType.Char, 8).Value = cuenta;
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (!dr.Read())
                    {
                        throw new ReglaNegocioException("La cuenta " + cuenta + " no existe.");
                    }
                    string estado = dr.GetString(3);
                    if (estado != "ACTIVO")
                    {
                        throw new ReglaNegocioException("La cuenta " + cuenta + " no está activa (" + estado + ").");
                    }
                    return new EstadoCuenta
                    {
                        Codigo = cuenta,
                        Moneda = dr.GetString(0),
                        Saldo = dr.GetDecimal(1),
                        ContadorMovimientos = dr.GetInt32(2)
                    };
                }
            }
        }

        /// <summary>
        /// Actualiza saldo y contador de la cuenta e inserta el movimiento.
        /// "variacion" es positiva para ingresos y negativa para salidas; el importe se guarda siempre positivo.
        /// </summary>
        private static decimal Aplicar(SqlConnection cn, SqlTransaction tx, EstadoCuenta cuenta, decimal variacion,
                                       string tipo, string codEmpleado, string referencia)
        {
            decimal nuevoSaldo = cuenta.Saldo + variacion;
            if (nuevoSaldo < 0)
            {
                throw new ReglaNegocioException(
                    "Saldo insuficiente en la cuenta " + cuenta.Codigo + ". Saldo disponible: " + Dinero(cuenta.Saldo) + ".");
            }
            int numero = cuenta.ContadorMovimientos + 1;

            const string sqlCuenta = @"
                UPDATE Cuenta SET dec_cuensaldo = @saldo, int_cuencontmov = @numero
                WHERE chr_cuencodigo = @cuenta";
            using (var cmd = new SqlCommand(sqlCuenta, cn, tx))
            {
                cmd.Parameters.Add("@saldo", SqlDbType.Money).Value = nuevoSaldo;
                cmd.Parameters.Add("@numero", SqlDbType.Int).Value = numero;
                cmd.Parameters.Add("@cuenta", SqlDbType.Char, 8).Value = cuenta.Codigo;
                cmd.ExecuteNonQuery();
            }

            const string sqlMovimiento = @"
                INSERT INTO Movimiento (chr_cuencodigo, int_movinumero, dtt_movifecha, chr_emplcodigo,
                                        chr_tipocodigo, dec_moviimporte, chr_cuenreferencia)
                VALUES (@cuenta, @numero, GETDATE(), @empleado, @tipo, @importe, @referencia)";
            using (var cmd = new SqlCommand(sqlMovimiento, cn, tx))
            {
                cmd.Parameters.Add("@cuenta", SqlDbType.Char, 8).Value = cuenta.Codigo;
                cmd.Parameters.Add("@numero", SqlDbType.Int).Value = numero;
                cmd.Parameters.Add("@empleado", SqlDbType.Char, 4).Value = codEmpleado;
                cmd.Parameters.Add("@tipo", SqlDbType.Char, 3).Value = tipo;
                cmd.Parameters.Add("@importe", SqlDbType.Money).Value = Math.Abs(variacion);
                cmd.Parameters.Add("@referencia", SqlDbType.Char, 8).Value = (object)referencia ?? DBNull.Value;
                cmd.ExecuteNonQuery();
            }

            cuenta.Saldo = nuevoSaldo;
            cuenta.ContadorMovimientos = numero;
            return nuevoSaldo;
        }

        private static bool ExisteCuenta(SqlConnection cn, string cuenta)
        {
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Cuenta WHERE chr_cuencodigo = @cuenta", cn))
            {
                cmd.Parameters.Add("@cuenta", SqlDbType.Char, 8).Value = cuenta;
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        private static void ValidarEmpleado(SqlConnection cn, SqlTransaction tx, string codEmpleado)
        {
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Empleado WHERE chr_emplcodigo = @empleado", cn, tx))
            {
                cmd.Parameters.Add("@empleado", SqlDbType.Char, 4).Value = codEmpleado;
                if ((int)cmd.ExecuteScalar() == 0)
                {
                    throw new ReglaNegocioException("El empleado " + codEmpleado + " no existe. Vuelva a iniciar sesión.");
                }
            }
        }
    }

    /// <summary>Validaciones de formato. Lanzan ArgumentException con un mensaje para el usuario.</summary>
    internal static class Validacion
    {
        /// <summary>Las cuentas de EurekaBank tienen 8 dígitos (CHAR(8)).</summary>
        public static string Cuenta(string cuenta, string nombre)
        {
            string limpia = (cuenta ?? string.Empty).Trim();
            if (limpia.Length != 8 || !limpia.All(char.IsDigit))
            {
                throw new ArgumentException("La " + nombre + " debe tener 8 dígitos (ejemplo: 00100001).");
            }
            return limpia;
        }

        public static void Importe(decimal importe, decimal maximo)
        {
            if (importe <= 0)
            {
                throw new ArgumentException("El importe debe ser mayor que cero.");
            }
            if (decimal.Round(importe, 2) != importe)
            {
                throw new ArgumentException("El importe admite como máximo 2 decimales.");
            }
            if (importe > maximo)
            {
                throw new ArgumentException("El importe no puede superar " + EurekaServicio.Dinero(maximo) + ".");
            }
        }

        public static string Empleado(string codigo)
        {
            string limpio = (codigo ?? string.Empty).Trim();
            if (limpio.Length != 4)
            {
                throw new ArgumentException("Código de empleado inválido. Vuelva a iniciar sesión.");
            }
            return limpio;
        }
    }
}
