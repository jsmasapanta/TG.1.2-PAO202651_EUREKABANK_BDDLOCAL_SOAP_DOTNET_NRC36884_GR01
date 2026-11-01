namespace ec.edu.monster.modelo;

/// <summary>Empleado que inició sesión, tal como lo devuelve el servicio. Solo datos (get/set).</summary>
public class Empleado
{
    public string Codigo { get; set; } = "";
    public string Usuario { get; set; } = "";
    public string Nombre { get; set; } = "";
    /// <summary>Token de la sesión que entrega el login; va en el header SOAP de las demás operaciones.</summary>
    public string Token { get; set; } = "";
}
