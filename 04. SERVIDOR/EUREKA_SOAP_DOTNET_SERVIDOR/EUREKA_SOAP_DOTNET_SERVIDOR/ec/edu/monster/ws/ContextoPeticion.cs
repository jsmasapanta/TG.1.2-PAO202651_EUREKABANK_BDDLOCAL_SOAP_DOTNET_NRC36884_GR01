using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Web;
using ec.edu.monster.modelo;

namespace ec.edu.monster.ws
{
    /// <summary>
    /// Lee de la petición en curso lo que no viaja como parámetro: el header HTTP X-Cliente
    /// (WEB, MOVIL, ESCRITORIO, CONSOLA, PANEL) y el header SOAP &lt;Token&gt; (solo WCF; ASMX usa SoapHeader).
    /// </summary>
    internal static class ContextoPeticion
    {
        public const string HeaderCliente = "X-Cliente";
        public const string HeaderToken = "Token";

        public static string Cliente()
        {
            OperationContext wcf = OperationContext.Current;
            if (wcf != null && wcf.IncomingMessageProperties.TryGetValue(HttpRequestMessageProperty.Name, out object propiedad))
            {
                return ((HttpRequestMessageProperty)propiedad).Headers[HeaderCliente];
            }
            return HttpContext.Current?.Request.Headers[HeaderCliente];
        }

        /// <summary>Contenido del header SOAP &lt;Token xmlns="http://espe.edu.ec/eureka/"&gt;, o null si no vino.</summary>
        public static string TokenWcf()
        {
            OperationContext wcf = OperationContext.Current;
            if (wcf == null)
            {
                return null;
            }
            int indice = wcf.IncomingMessageHeaders.FindHeader(HeaderToken, Constantes.NamespaceServicio);
            return indice < 0 ? null : wcf.IncomingMessageHeaders.GetHeader<string>(indice);
        }
    }
}
