using System.ServiceModel;
using ec.edu.monster.modelo;

namespace ec.edu.monster.servicio;

/// <summary>
/// Operaciones que publica el servidor (mismo Name y Namespace que su contrato), en versión asíncrona.
/// Con esta interfaz el cliente llama al servicio mediante ChannelFactory.
/// </summary>
[ServiceContract(Name = "WSEureka", Namespace = "http://espe.edu.ec/eureka/")]
public interface IWSEureka
{
    [OperationContract(Name = "login")]
    [FaultContract(typeof(ErrorServicio))]
    Task<Empleado> LoginAsync(string usuario, string clave);

    [OperationContract(Name = "cerrarSesion")]
    [FaultContract(typeof(ErrorServicio))]
    Task<bool> CerrarSesionAsync(string token);

    [OperationContract(Name = "consultarCuenta")]
    [FaultContract(typeof(ErrorServicio))]
    Task<Cuenta> ConsultarCuentaAsync(string cuenta);

    [OperationContract(Name = "traerMovimientos")]
    [FaultContract(typeof(ErrorServicio))]
    Task<Movimiento[]> TraerMovimientosAsync(string cuenta);

    [OperationContract(Name = "registrarDeposito")]
    [FaultContract(typeof(ErrorServicio))]
    Task<decimal> RegistrarDepositoAsync(string cuenta, decimal importe, string codEmpleado);

    [OperationContract(Name = "registrarRetiro")]
    [FaultContract(typeof(ErrorServicio))]
    Task<decimal> RegistrarRetiroAsync(string cuenta, decimal importe, string codEmpleado);

    [OperationContract(Name = "registrarTransferencia")]
    [FaultContract(typeof(ErrorServicio))]
    Task<decimal> RegistrarTransferenciaAsync(string cuentaOrigen, string cuentaDestino, decimal importe, string codEmpleado);
}
