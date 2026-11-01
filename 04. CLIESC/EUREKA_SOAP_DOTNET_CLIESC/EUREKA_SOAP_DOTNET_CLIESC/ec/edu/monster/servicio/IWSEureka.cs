using System.ServiceModel;
using ec.edu.monster.modelo;

namespace ec.edu.monster.servicio
{
    /// <summary>
    /// Operaciones que publica el servidor (mismo Name y Namespace que su contrato).
    /// Con esta interfaz el cliente llama al servicio mediante ChannelFactory.
    /// </summary>
    [ServiceContract(Name = "WSEureka", Namespace = "http://espe.edu.ec/eureka/")]
    public interface IWSEureka
    {
        [OperationContract(Name = "login")]
        [FaultContract(typeof(ErrorServicio))]
        Empleado Login(string usuario, string clave);

        [OperationContract(Name = "cerrarSesion")]
        [FaultContract(typeof(ErrorServicio))]
        bool CerrarSesion(string token);

        [OperationContract(Name = "consultarCuenta")]
        [FaultContract(typeof(ErrorServicio))]
        Cuenta ConsultarCuenta(string cuenta);

        [OperationContract(Name = "traerMovimientos")]
        [FaultContract(typeof(ErrorServicio))]
        Movimiento[] TraerMovimientos(string cuenta);

        [OperationContract(Name = "registrarDeposito")]
        [FaultContract(typeof(ErrorServicio))]
        decimal RegistrarDeposito(string cuenta, decimal importe, string codEmpleado);

        [OperationContract(Name = "registrarRetiro")]
        [FaultContract(typeof(ErrorServicio))]
        decimal RegistrarRetiro(string cuenta, decimal importe, string codEmpleado);

        [OperationContract(Name = "registrarTransferencia")]
        [FaultContract(typeof(ErrorServicio))]
        decimal RegistrarTransferencia(string cuentaOrigen, string cuentaDestino, decimal importe, string codEmpleado);
    }
}
