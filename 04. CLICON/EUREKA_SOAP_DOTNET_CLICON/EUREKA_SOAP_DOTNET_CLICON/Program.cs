using System;
using ec.edu.monster.vista;

namespace ec.edu.monster
{
    internal static class Program
    {
        private static void Main()
        {
            var vista = new VistaConsola();
            try
            {
                vista.Iniciar();
            }
            catch (Exception ex)
            {
                // Por ejemplo, SoapHost o SoapPuerto inválidos en App.config.
                vista.MostrarError(ex.Message);
                vista.Pausa();
            }
        }
    }
}
