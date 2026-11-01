using ec.edu.monster.controlador;
using ec.edu.monster.servicio;

namespace ec.edu.monster.vista;

/// <summary>
/// Pantalla de inicio de sesión. La URL del servidor viene de appsettings.json (incrustado); aquí se puede
/// cambiar solo para esta ejecución. Los 3 intentos y el bloqueo los controla el servidor: aquí solo se muestra su mensaje.
/// </summary>
public partial class LoginPage : ContentPage
{
    private readonly ControladorEureka _controlador = ControladorEureka.Instancia;

    public LoginPage()
    {
        InitializeComponent();
        TxtUrl.Text = _controlador.UrlServidor;
        if (ConfiguracionServidor.Error != null)
        {
            MostrarError(ConfiguracionServidor.Error);
        }
    }

    private async void OnIngresarClicked(object? sender, EventArgs e)
    {
        string usuario = TxtUsuario.Text ?? "";
        string clave = TxtClave.Text ?? "";
        if (string.IsNullOrWhiteSpace(TxtUrl.Text))
        {
            MostrarError("Ingrese la URL del servidor.");
            return;
        }

        _controlador.UrlServidor = TxtUrl.Text;
        Ocupado(true);
        try
        {
            await _controlador.IniciarSesionAsync(usuario, clave);
            Window.Page = new NavigationPage(new MenuPage())
            {
                BarBackgroundColor = Color.FromArgb("#0B4F8A"),
                BarTextColor = Colors.White
            };
        }
        catch (ErrorServicioException ex)
        {
            // Mensaje del servidor: clave incorrecta con intentos restantes, usuario bloqueado o falla de conexión.
            TxtClave.Text = "";
            MostrarError(ex.Message);
        }
        finally
        {
            Ocupado(false);
        }
    }

    private void MostrarError(string mensaje)
    {
        LblError.Text = mensaje;
        LblError.IsVisible = true;
    }

    private void Ocupado(bool ocupado)
    {
        BtnIngresar.IsEnabled = !ocupado;
        Cargando.IsRunning = Cargando.IsVisible = ocupado;
        if (ocupado)
        {
            LblError.IsVisible = false;
        }
    }
}
