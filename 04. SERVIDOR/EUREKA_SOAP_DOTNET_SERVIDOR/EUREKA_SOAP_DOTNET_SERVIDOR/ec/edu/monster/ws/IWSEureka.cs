using System.Collections.Generic;
using System.ServiceModel;
using ec.edu.monster.modelo;

namespace ec.edu.monster.ws
{
    /// <summary>
    /// Contrato SOAP de EurekaBank. Los importes y saldos viajan como xs:decimal.
    /// Si algo falla (cuenta inexistente, saldo insuficiente, base de datos caída...)
    /// la operación responde un SOAP Fault con ErrorServicio (Codigo, Mensaje).
    /// login devuelve un token; consultarCuenta, traerMovimientos y registrar* lo exigen en el
    /// header SOAP &lt;Token xmlns="http://espe.edu.ec/eureka/"&gt;. obtenerEstado no pide login.
    /// </summary>
    [ServiceContract(Name = "WSEureka", Namespace = Constantes.NamespaceServicio)]
    public interface IWSEureka
    {
        /// <summary>Devuelve el empleado; si usuario o clave no coinciden responde un Fault con los intentos restantes.</summary>
        [OperationContract(Name = "login")]
        [FaultContract(typeof(ErrorServicio))]
        Empleado Login(string usuario, string clave);

        /// <summary>Invalida el token de la sesión. Devuelve false si ya no existía.</summary>
        [OperationContract(Name = "cerrarSesion")]
        [FaultContract(typeof(ErrorServicio))]
        bool CerrarSesion(string token);

        /// <summary>Métricas del monitor para el panel. No pide login ni cuenta como petición.</summary>
        [OperationContract(Name = "obtenerEstado")]
        EstadoServidor ObtenerEstado();

        [OperationContract(Name = "consultarCuenta")]
        [FaultContract(typeof(ErrorServicio))]
        Cuenta ConsultarCuenta(string cuenta);

        [OperationContract(Name = "traerMovimientos")]
        [FaultContract(typeof(ErrorServicio))]
        List<Movimiento> TraerMovimientos(string cuenta);

        /// <summary>Devuelve el nuevo saldo de la cuenta.</summary>
        [OperationContract(Name = "registrarDeposito")]
        [FaultContract(typeof(ErrorServicio))]
        decimal RegistrarDeposito(string cuenta, decimal importe, string codEmpleado);

        /// <summary>Devuelve el nuevo saldo de la cuenta.</summary>
        [OperationContract(Name = "registrarRetiro")]
        [FaultContract(typeof(ErrorServicio))]
        decimal RegistrarRetiro(string cuenta, decimal importe, string codEmpleado);

        /// <summary>Devuelve el nuevo saldo de la cuenta de origen.</summary>
        [OperationContract(Name = "registrarTransferencia")]
        [FaultContract(typeof(ErrorServicio))]
        decimal RegistrarTransferencia(string cuentaOrigen, string cuentaDestino, decimal importe, string codEmpleado);
    }
}
