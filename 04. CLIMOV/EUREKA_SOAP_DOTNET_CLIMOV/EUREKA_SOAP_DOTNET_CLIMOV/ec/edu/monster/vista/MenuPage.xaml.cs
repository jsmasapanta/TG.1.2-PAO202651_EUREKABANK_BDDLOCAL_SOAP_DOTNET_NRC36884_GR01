using ec.edu.monster.controlador;
using ec.edu.monster.modelo;

namespace ec.edu.monster.vista;

/// <summary>Menú principal: las cuatro opciones del banco y las operaciones hechas en la sesión.</summary>
public partial class MenuPage : ContentPage
{
    private readonly ControladorEureka _controlador = ControladorEureka.Instancia;

    public MenuPage()
    {
        InitializeComponent();
        LblEmpleado.Text = $"{_controlador.Empleado?.Nombre} · Empleado {_controlador.Empleado?.Codigo}";
        LblServidor.Text = "Conectado a: " + _controlador.UrlServidor;
    }

    /// <summary>Al volver de una operación se actualiza la lista de la sesión.</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        ListaSesion.Children.Clear();
        foreach (string linea in _controlador.OperacionesSesion)
        {
            ListaSesion.Children.Add(new Label { Text = linea, FontSize = 13, TextColor = Color.FromArgb("#212529") });
        }
        if (_controlador.OperacionesSesion.Count == 0)
        {
            ListaSesion.Children.Add(new Label { Text = "Aún no hay operaciones.", FontSize = 13, TextColor = Color.FromArgb("#6C757D") });
        }
    }

    private async void OnMovimientosClicked(object? sender, EventArgs e) =>
        await Navigation.PushAsync(new MovimientosPage());

    private async void OnDepositoClicked(object? sender, EventArgs e) =>
        await Navigation.PushAsync(new OperacionPage(TipoOperacion.Deposito));

    private async void OnRetiroClicked(object? sender, EventArgs e) =>
        await Navigation.PushAsync(new OperacionPage(TipoOperacion.Retiro));

    private async void OnTransferenciaClicked(object? sender, EventArgs e) =>
        await Navigation.PushAsync(new OperacionPage(TipoOperacion.Transferencia));

    private async void OnCerrarSesionClicked(object? sender, EventArgs e)
    {
        if (await DisplayAlert("Cerrar sesión", "¿Desea salir de EurekaBank?", "Sí", "No"))
        {
            await _controlador.CerrarSesionAsync();
            Window.Page = new LoginPage();
        }
    }
}
