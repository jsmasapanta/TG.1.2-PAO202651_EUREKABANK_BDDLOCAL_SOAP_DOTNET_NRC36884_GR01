using System;
using System.Configuration;

namespace ec.edu.monster.servicio
{
    /// <summary>
    /// Arma la URL del servidor SOAP sin dejarla fija en el código. Primero mira las variables de entorno
    /// SOAP_HOST y SOAP_PORT; si no existen, usa SoapHost, SoapPuerto y SoapRuta de App.config.
    /// Desde la pantalla se puede cambiar el host y el puerto (Guardar): se escriben en el .exe.config
    /// para que la próxima vez arranque con ellos. Si el host o el puerto no son válidos lanza un error claro.
    /// </summary>
    public static class ConfiguracionServidor
    {
        public static string Host => (Valor("SOAP_HOST", "SoapHost") ?? string.Empty).Trim();

        public static string Puerto => (Valor("SOAP_PORT", "SoapPuerto") ?? string.Empty).Trim();

        public static string LeerUrl() => ArmarUrl(Host, Puerto);

        /// <summary>
        /// Valida host y puerto y devuelve http://host:puerto/WSEureka.svc. En "host" se acepta la IP sola
        /// (192.168.100.2), con puerto (192.168.100.2:8087) o la URL completa (http://192.168.100.2:8087/WSEureka.svc):
        /// de ahí se toman la IP y el puerto.
        /// </summary>
        public static string ArmarUrl(string host, string puerto)
        {
            host = (host ?? string.Empty).Trim();
            puerto = (puerto ?? string.Empty).Trim();
            Separar(ref host, ref puerto);
            string ruta = (ConfigurationManager.AppSettings["SoapRuta"] ?? "WSEureka.svc").Trim().TrimStart('/');

            if (host.Length == 0 || Uri.CheckHostName(host) == UriHostNameType.Unknown)
            {
                throw new InvalidOperationException(
                    "Host del servidor inválido: \"" + host + "\". Escriba la IP de la PC del servidor (ejemplo: 192.168.1.10) "
                    + "en SoapHost de App.config o en la variable SOAP_HOST.");
            }
            if (!int.TryParse(puerto, out int numero) || numero < 1 || numero > 65535)
            {
                throw new InvalidOperationException(
                    "Puerto del servidor inválido: \"" + puerto + "\". Debe ser un número entre 1 y 65535 (el servidor usa 8087).");
            }
            return new UriBuilder("http", host, numero, ruta).Uri.ToString();
        }

        /// <summary>Si escribieron "IP:puerto" o la URL completa en el host, separa la IP y el puerto.</summary>
        private static void Separar(ref string host, ref string puerto)
        {
            if (host.IndexOf("://", StringComparison.Ordinal) >= 0)
            {
                if (Uri.TryCreate(host, UriKind.Absolute, out Uri url))
                {
                    host = url.Host;
                    puerto = url.Port.ToString();
                }
                return;
            }
            int dosPuntos = host.LastIndexOf(':');
            if (dosPuntos > 0 && host.IndexOf(':') == dosPuntos) // "192.168.100.2:8087" (no una IPv6)
            {
                puerto = host.Substring(dosPuntos + 1).Split('/')[0];
                host = host.Substring(0, dosPuntos);
            }
            host = host.Split('/')[0];
        }

        /// <summary>Guarda host y puerto en el .exe.config (appSettings SoapHost y SoapPuerto).</summary>
        public static void Guardar(string host, string puerto)
        {
            Configuration archivo = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            Poner(archivo, "SoapHost", host.Trim());
            Poner(archivo, "SoapPuerto", puerto.Trim());
            archivo.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
            // Si había variables de entorno, se quitan en este proceso para que manden los valores nuevos.
            Environment.SetEnvironmentVariable("SOAP_HOST", null);
            Environment.SetEnvironmentVariable("SOAP_PORT", null);
        }

        private static void Poner(Configuration archivo, string clave, string valor)
        {
            if (archivo.AppSettings.Settings[clave] == null)
            {
                archivo.AppSettings.Settings.Add(clave, valor);
            }
            else
            {
                archivo.AppSettings.Settings[clave].Value = valor;
            }
        }

        private static string Valor(string variable, string clave)
        {
            string valor = Environment.GetEnvironmentVariable(variable);
            return string.IsNullOrWhiteSpace(valor) ? ConfigurationManager.AppSettings[clave] : valor;
        }
    }
}
