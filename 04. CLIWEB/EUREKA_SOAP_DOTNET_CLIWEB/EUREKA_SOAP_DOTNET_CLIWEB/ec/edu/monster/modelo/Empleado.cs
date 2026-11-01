using System.Runtime.Serialization;

namespace ec.edu.monster.modelo;

/// <summary>Empleado que inició sesión, tal como lo devuelve el servicio. Solo datos (get/set).</summary>
[DataContract(Namespace = "http://espe.edu.ec/eureka/")]
public class Empleado
{
    [DataMember(Order = 1)] public string Codigo { get; set; } = "";
    [DataMember(Order = 2)] public string Usuario { get; set; } = "";
    [DataMember(Order = 3)] public string Nombre { get; set; } = "";
    /// <summary>Token de la sesión que entrega el login; va en el header SOAP de las demás operaciones.</summary>
    [DataMember(Order = 4)] public string Token { get; set; } = "";
}
