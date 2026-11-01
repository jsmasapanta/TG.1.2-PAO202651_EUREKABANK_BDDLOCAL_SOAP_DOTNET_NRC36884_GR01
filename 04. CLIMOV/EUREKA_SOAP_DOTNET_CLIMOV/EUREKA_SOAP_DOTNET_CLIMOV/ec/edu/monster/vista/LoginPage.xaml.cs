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
        MostrarServidor();
        if (ConfiguracionServidor.Error != null)
        {
            MostrarError(ConfiguracionServidor.Error);
        }
        PintarIntentos(3);
    }

    /// <summary>Al abrir el login se comprueba si el servidor responde.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ComprobarServidorAsync();
    }

    private void MostrarServidor()
    {
        TxtHost.Text = _controlador.HostServidor;
        TxtPuerto.Text = _controlador.PuertoServidor;
        LblUrl.Text = "Actual: " + _controlador.UrlServidor;
    }

    private async void OnGuardarServidorClicked(object? sender, EventArgs e)
    {
        try
        {
            _controlador.CambiarServidor(TxtHost.Text ?? "", TxtPuerto.Text ?? "");
            CajaError.IsVisible = false;
        }
        catch (InvalidOperationException ex)
        {
            MostrarError(ex.Message);
            return;
        }
        MostrarServidor();
        await ComprobarServidorAsync();
    }

    private async void OnRestablecerServidorClicked(object? sender, EventArgs e)
    {
        _controlador.RestablecerServidor();
        MostrarServidor();
        await ComprobarServidorAsync();
    }

    private async Task ComprobarServidorAsync()
    {
        LblServidorEstado.Text = "Comprobando…";
        bool activo = await _controlador.ServidorActivoAsync();
        PuntoServidor.Fill = Color.FromArgb(activo ? "#22C55E" : "#C0392B");
        LblServidorEstado.Text = activo ? "Servidor activo" : "Sin respuesta";
        LblServidorEstado.TextColor = Color.FromArgb(activo ? "#146C43" : "#A12622");
        Indicador.BackgroundColor = Color.FromArgb(activo ? "#E9F7EF" : "#FDECEA");
        Indicador.Stroke = Color.FromArgb(activo ? "#B7E4C7" : "#F5C2C0");
    }

    /// <summary>Las caritas solo dibujan lo que dice el servidor ("Intentos restantes: N"); él cuenta los intentos.</summary>
    private void PintarIntentos(int restantes)
    {
        Border[] caritas = { Carita1, Carita2, Carita3 };
        for (int i = 0; i < caritas.Length; i++)
        {
            bool viva = i < restantes;
            caritas[i].BackgroundColor = Color.FromArgb(viva ? "#E8F6DC" : "#EEEEEE");
            caritas[i].Stroke = Color.FromArgb(viva ? "#7AC943" : "#CCCCCC");
            caritas[i].Opacity = viva ? 1 : 0.45;
        }
    }

    private async void OnIngresarClicked(object? sender, EventArgs e)
    {
        string usuario = TxtUsuario.Text ?? "";
        string clave = TxtClave.Text ?? "";
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
            PintarIntentos(Formato.IntentosRestantes(ex.Message));
        }
        finally
        {
            Ocupado(false);
        }
    }

    private void MostrarError(string mensaje)
    {
        LblError.Text = "✖ " + mensaje;
        CajaError.IsVisible = true;
    }

    private void Ocupado(bool ocupado)
    {
        BtnIngresar.IsEnabled = !ocupado;
        Cargando.IsRunning = Cargando.IsVisible = ocupado;
        if (ocupado)
        {
            CajaError.IsVisible = false;
        }
    }
}
