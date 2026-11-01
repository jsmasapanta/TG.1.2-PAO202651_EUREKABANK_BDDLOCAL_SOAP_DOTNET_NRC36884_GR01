using System;
using System.Configuration;

namespace ec.edu.monster.servicio
{
    /// <summary>
    /// Arma la URL del servidor SOAP sin dejarla fija en el código. Primero mira las variables de entorno
    /// SOAP_HOST y SOAP_PORT; si no existen, usa SoapHost, SoapPuerto y SoapRuta de App.config.
    /// Si el host o el puerto no son válidos lanza un error con un mensaje claro.
    /// </summary>
    public static class ConfiguracionServidor
    {
        public static string LeerUrl()
        {
            string host = (Valor("SOAP_HOST", "SoapHost") ?? string.Empty).Trim();
            string puerto = (Valor("SOAP_PORT", "SoapPuerto") ?? string.Empty).Trim();
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

        private static string Valor(string variable, string clave)
        {
            string valor = Environment.GetEnvironmentVariable(variable);
            return string.IsNullOrWhiteSpace(valor) ? ConfigurationManager.AppSettings[clave] : valor;
        }
    }
}
