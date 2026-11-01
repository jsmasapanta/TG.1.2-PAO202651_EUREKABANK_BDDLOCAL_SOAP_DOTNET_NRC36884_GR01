using ec.edu.monster.controlador;
using ec.edu.monster.modelo;
using ec.edu.monster.servicio;
using Microsoft.Maui.Controls.Shapes;

namespace ec.edu.monster.vista;

/// <summary>Consulta de una cuenta: datos, saldo y movimientos (el más reciente primero).</summary>
public partial class MovimientosPage : ContentPage
{
    private readonly ControladorEureka _controlador = ControladorEureka.Instancia;
    private readonly GraficoCuenta _grafico = new();

    public MovimientosPage()
    {
        InitializeComponent();
        Grafico.Drawable = _grafico;
    }

    private async void OnConsultarClicked(object? sender, EventArgs e)
    {
        string cuenta = (TxtCuenta.Text ?? "").Trim();
        TxtCuenta.Unfocus(); // oculta el teclado para que se vea la lista
        Ocupado(true);
        try
        {
            Cuenta datos = await _controlador.ConsultarCuentaAsync(cuenta);
            Movimiento[] movimientos = await _controlador.TraerMovimientosAsync(cuenta);
            LblTipoCuenta.Text = "AHORROS · " + datos.Moneda.ToUpperInvariant();
            LblNumero.Text = Formato.NumeroTarjeta(datos.Codigo);
            LblCliente.Text = datos.Cliente.ToUpperInvariant();
            LblDetalle.Text = $"{datos.Sucursal} · {datos.Estado}";
            LblSaldo.Text = Formato.Simbolo(datos.Moneda) + " " + Formato.Dinero(datos.Saldo);
            TarjetaCuenta.Opacity = datos.Estado == "ACTIVO" ? 1 : 0.6;
            TarjetaCuenta.IsVisible = true;
            _grafico.Mostrar(datos, movimientos);
            CajaGrafico.IsVisible = movimientos.Length > 0;
            Grafico.Invalidate();
            MostrarMovimientos(movimientos);
        }
        catch (ErrorServicioException ex)
        {
            TarjetaCuenta.IsVisible = false;
            CajaGrafico.IsVisible = false;
            LblTituloLista.IsVisible = false;
            ListaMovimientos.Children.Clear();
            MostrarError(ex.Message);
        }
        finally
        {
            Ocupado(false);
        }
    }

    private void MostrarMovimientos(Movimiento[] movimientos)
    {
        LblTituloLista.Text = $"Movimientos ({movimientos.Length})";
        LblTituloLista.IsVisible = true;
        ListaMovimientos.Children.Clear();
        foreach (Movimiento m in movimientos)
        {
            var fila = new Grid { ColumnDefinitions = new ColumnDefinitionCollection(new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)) };
            var izquierda = new VerticalStackLayout { Spacing = 2 };
            izquierda.Children.Add(new Label { Text = $"#{m.Numero}  {m.Tipo}", FontAttributes = FontAttributes.Bold, TextColor = Tema.Recurso("Texto") });
            string referencia = string.IsNullOrEmpty(m.CuentaReferencia) ? "" : $" · ref. {m.CuentaReferencia}";
            izquierda.Children.Add(new Label { Text = $"{m.Fecha:dd/MM/yyyy HH:mm} · emp. {m.Empleado}{referencia}", FontSize = 12, TextColor = Tema.Recurso("TextoSuave") });
            var importe = new Label
            {
                Text = (Formato.EsIngreso(m) ? "+" : "−") + Formato.Dinero(m.Importe),
                FontAttributes = FontAttributes.Bold,
                FontSize = 16,
                VerticalOptions = LayoutOptions.Center,
                TextColor = Color.FromArgb(Formato.EsIngreso(m) ? "#198754" : "#C0392B")
            };
            fila.Add(izquierda, 0, 0);
            fila.Add(importe, 1, 0);
            ListaMovimientos.Children.Add(new Border
            {
                Content = fila,
                Padding = new Thickness(14, 10),
                BackgroundColor = Tema.Recurso("Tarjeta"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 10 }
            });
        }
        if (movimientos.Length == 0)
        {
            ListaMovimientos.Children.Add(new Label { Text = "La cuenta no tiene movimientos.", TextColor = Tema.Recurso("TextoSuave") });
        }
    }

    private void MostrarError(string mensaje)
    {
        LblError.Text = mensaje;
        LblError.IsVisible = true;
    }

    private void Ocupado(bool ocupado)
    {
        BtnConsultar.IsEnabled = !ocupado;
        Cargando.IsRunning = Cargando.IsVisible = ocupado;
        if (ocupado)
        {
            LblError.IsVisible = false;
        }
    }
}
