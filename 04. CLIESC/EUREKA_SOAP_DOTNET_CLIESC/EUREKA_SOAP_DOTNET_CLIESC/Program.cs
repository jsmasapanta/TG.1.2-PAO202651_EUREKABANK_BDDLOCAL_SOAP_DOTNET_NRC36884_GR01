using System;
using System.Windows.Forms;
using ec.edu.monster.controlador;
using ec.edu.monster.vista;

namespace ec.edu.monster
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            ControladorEureka controlador;
            try
            {
                controlador = new ControladorEureka();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo leer la configuración del servidor en App.config:\n" + ex.Message,
                    "EurekaBank", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var bienvenida = new FormBienvenida())
            {
                bienvenida.ShowDialog();
            }

            // Login → Principal. "Cerrar sesión" vuelve al login; cerrar la ventana termina la aplicación.
            while (true)
            {
                using (var login = new FormLogin(controlador))
                {
                    if (login.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }
                }
                using (var principal = new FormPrincipal(controlador))
                {
                    if (principal.ShowDialog() != DialogResult.Retry)
                    {
                        return;
                    }
                }
            }
        }
    }
}
