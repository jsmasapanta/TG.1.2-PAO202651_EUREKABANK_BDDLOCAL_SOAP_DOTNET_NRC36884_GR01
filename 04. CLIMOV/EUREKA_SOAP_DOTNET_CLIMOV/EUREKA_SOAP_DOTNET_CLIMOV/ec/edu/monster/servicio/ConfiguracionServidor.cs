using System.Reflection;
using System.Text.Json;

namespace ec.edu.monster.servicio;

/// <summary>
/// Lee la dirección del servidor de appsettings.json, que va incrustado en la app (EmbeddedResource):
/// sección "ServidorSoap" con Host, Puerto y Ruta. Para cambiar la IP se edita ese archivo y se vuelve
/// a compilar e instalar. Si el host o el puerto no son válidos, Error trae un mensaje claro.
/// </summary>
public static class ConfiguracionServidor
{
    private const string Recurso = "appsettings.json";

    /// <summary>URL armada (http://Host:Puerto/Ruta), o vacía si la configuración no es válida.</summary>
    public static string Url { get; }

    /// <summary>Mensaje para el usuario cuando appsettings.json tiene un host o puerto inválido.</summary>
    public static string? Error { get; }

    static ConfiguracionServidor()
    {
        try
        {
            using Stream? flujo = Assembly.GetExecutingAssembly().GetManifestResourceStream(Recurso);
            if (flujo == null)
            {
                throw new InvalidOperationException("No se encontró appsettings.json dentro de la app.");
            }
            using JsonDocument json = JsonDocument.Parse(flujo);
            JsonElement seccion = json.RootElement.GetProperty("ServidorSoap");
            string host = Texto(seccion, "Host");
            string puerto = Texto(seccion, "Puerto");
            string ruta = Texto(seccion, "Ruta").TrimStart('/');
            Url = ArmarUrl(host, puerto, ruta.Length == 0 ? "WSEureka.svc" : ruta);
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is JsonException || ex is KeyNotFoundException)
        {
            Url = "";
            Error = ex.Message;
        }
    }

    /// <summary>Valida host y puerto y arma la URL; lanza InvalidOperationException con el motivo.</summary>
    public static string ArmarUrl(string host, string puerto, string ruta)
    {
        host = host.Trim();
        puerto = puerto.Trim();
        Separar(ref host, ref puerto);
        if (host.Length == 0 || Uri.CheckHostName(host) == UriHostNameType.Unknown)
        {
            throw new InvalidOperationException(
                $"Host del servidor inválido: \"{host}\". Escriba la IP de la PC del servidor (ejemplo: 192.168.100.2) en Servidor SOAP o en appsettings.json.");
        }
        if (!int.TryParse(puerto.Trim(), out int numero) || numero < 1 || numero > 65535)
        {
            throw new InvalidOperationException(
                $"Puerto del servidor inválido: \"{puerto}\". Debe ser un número entre 1 y 65535 (el servidor usa 8087).");
        }
        return new UriBuilder("http", host, numero, ruta).Uri.ToString();
    }

    /// <summary>
    /// En "host" se acepta la IP sola (192.168.100.2), con puerto (192.168.100.2:8087) o la URL completa
    /// (http://192.168.100.2:8087/WSEureka.svc): de ahí se toman la IP y el puerto.
    /// </summary>
    private static void Separar(ref string host, ref string puerto)
    {
        if (host.Contains("://"))
        {
            if (Uri.TryCreate(host, UriKind.Absolute, out Uri? url))
            {
                host = url.Host;
                puerto = url.Port.ToString();
            }
            return;
        }
        int dosPuntos = host.LastIndexOf(':');
        if (dosPuntos > 0 && host.IndexOf(':') == dosPuntos) // "192.168.100.2:8087" (no una IPv6)
        {
            puerto = host[(dosPuntos + 1)..].Split('/')[0];
            host = host[..dosPuntos];
        }
        host = host.Split('/')[0];
    }

    private static string Texto(JsonElement seccion, string nombre)
    {
        JsonElement valor = seccion.GetProperty(nombre);
        return valor.ValueKind == JsonValueKind.Number ? valor.GetRawText() : valor.GetString() ?? "";
    }
}
