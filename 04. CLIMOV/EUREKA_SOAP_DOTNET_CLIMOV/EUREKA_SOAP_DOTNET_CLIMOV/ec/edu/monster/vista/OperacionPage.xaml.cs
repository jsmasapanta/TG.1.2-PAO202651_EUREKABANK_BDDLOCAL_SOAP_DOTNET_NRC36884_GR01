using ec.edu.monster.controlador;
using ec.edu.monster.modelo;
using ec.edu.monster.servicio;

namespace ec.edu.monster.vista;

/// <summary>Depósito, retiro o transferencia (la misma pantalla; la transferencia pide además la cuenta de destino).</summary>
public partial class OperacionPage : ContentPage
{
    private readonly ControladorEureka _controlador = ControladorEureka.Instancia;
    private readonly TipoOperacion _tipo;
    private readonly Color _color;

    public OperacionPage(TipoOperacion tipo)
    {
        InitializeComponent();
        PermitirDecimales();
        _tipo = tipo;
        _color = Formato.ColorDe(tipo);

        Title = Formato.Titulo(tipo);
        BtnRegistrar.Text = "REGISTRAR " + Title.ToUpperInvariant();
        BtnRegistrar.BackgroundColor = _color;
        Cargando.Color = _color;

        bool esTransferencia = tipo == TipoOperacion.Transferencia;
        LblCuenta.Text = esTransferencia ? "Cuenta de origen" : "Cuenta";
        LblDestino.IsVisible = TxtDestino.IsVisible = esTransferencia;
        LblAyuda.Text = tipo switch
        {
            TipoOperacion.Deposito => "Suma el importe al saldo de la cuenta.",
            TipoOperacion.Retiro => "Resta el importe del saldo; la cuenta debe tener saldo suficiente.",
            _ => "Pasa el importe de una cuenta a otra de la misma moneda."
        };
    }

    private async void OnRegistrarClicked(object? sender, EventArgs e)
    {
        string cuenta = (TxtCuenta.Text ?? "").Trim();
        string? destino = _tipo == TipoOperacion.Transferencia ? (TxtDestino.Text ?? "").Trim() : null;
        decimal? importe = Formato.LeerImporte(TxtImporte.Text);
        TxtCuenta.Unfocus(); TxtDestino.Unfocus(); TxtImporte.Unfocus(); // oculta el teclado para ver el resultado

        // Las reglas (formato de cuenta, importe, cuentas distintas, saldo) las valida el servidor;
        // aquí solo se comprueba que el importe sea un número para poder enviarlo.
        if (importe == null)
        {
            MostrarError("Escriba el importe con números (ejemplo: 150 o 75,50).");
            return;
        }

        string pregunta = destino != null
            ? $"¿Transferir {Formato.Dinero(importe!.Value)} de la cuenta {cuenta} a la cuenta {destino}?"
            : $"¿Registrar {Title.ToLowerInvariant()} de {Formato.Dinero(importe!.Value)} en la cuenta {cuenta}?";
        if (!await DisplayAlert("Confirmar", pregunta, "Sí", "No"))
        {
            return;
        }

        Ocupado(true);
        try
        {
            decimal saldo = _tipo switch
            {
                TipoOperacion.Deposito => await _controlador.RegistrarDepositoAsync(cuenta, importe.Value),
                TipoOperacion.Retiro => await _controlador.RegistrarRetiroAsync(cuenta, importe.Value),
                _ => await _controlador.RegistrarTransferenciaAsync(cuenta, destino!, importe.Value)
            };
            _controlador.AnotarOperacion($"{DateTime.Now:HH:mm:ss} · {Title} · {cuenta}"
                + (destino != null ? $" → {destino}" : "")
                + $" · {Formato.Dinero(importe.Value)} (saldo {Formato.Dinero(saldo)})");
            LblResultado.TextColor = _color;
            LblResultado.Text = $"{Title} registrad{(destino != null ? "a" : "o")}";
            LblDetalle.Text = $"Importe: {Formato.Dinero(importe.Value)}\nNuevo saldo de {cuenta}: {Formato.Dinero(saldo)}";
            TxtImporte.Text = "";
            await MostrarComprobanteAsync(cuenta, destino, importe.Value, saldo);
        }
        catch (ErrorServicioException ex)
        {
            MostrarError(ex.Message);
        }
        finally
        {
            Ocupado(false);
        }
    }

    /// <summary>
    /// Recibo de la operación. El número de movimiento y el titular se piden al servidor (consultarCuenta):
    /// el movimiento recién grabado es el último de la cuenta. Si esa consulta falla, el recibo sale sin ellos.
    /// </summary>
    private async Task MostrarComprobanteAsync(string cuenta, string? destino, decimal importe, decimal saldo)
    {
        Cuenta? datos = null;
        try
        {
            datos = await _controlador.ConsultarCuentaAsync(cuenta);
        }
        catch (ErrorServicioException)
        {
        }
        string empleado = $"{_controlador.Empleado?.Codigo} · {_controlador.Empleado?.Nombre}";
        await Navigation.PushModalAsync(new ComprobantePage(_tipo, cuenta, destino, datos, datos?.NumeroMovimientos ?? 0, importe, saldo, empleado));
    }

    /// <summary>
    /// En Android el teclado numérico no deja escribir punto ni coma. Para el importe se aceptan los dos
    /// separadores (75.50 o 75,50); Formato.LeerImporte los convierte.
    /// El filtro de teclas va primero y luego SetRawInputType: así se muestra el teclado de números con decimales
    /// sin que Android cambie el filtro por el de su idioma (que deja solo el punto).
    /// Se agrega al final de los mapeos de MAUI que tocan el teclado; si no, MAUI vuelve a poner el suyo.
    /// </summary>
    private void PermitirDecimales()
    {
#if ANDROID
        if (!_decimalesRegistrados)
        {
            _decimalesRegistrados = true;
            foreach (string propiedad in new[] { nameof(Entry.Keyboard), nameof(Entry.IsReadOnly), nameof(Entry.IsPassword) })
            {
                Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(propiedad, (handler, vista) =>
                {
                    if (vista is Entry entrada && entrada.ClassId == "importe")
                    {
                        handler.PlatformView.KeyListener = Android.Text.Method.DigitsKeyListener.GetInstance("0123456789.,");
                        handler.PlatformView.SetRawInputType(Android.Text.InputTypes.ClassNumber | Android.Text.InputTypes.NumberFlagDecimal);
                    }
                });
            }
        }
#endif
    }

    private static bool _decimalesRegistrados;

    private void MostrarError(string mensaje)
    {
        LblResultado.TextColor = Color.FromArgb("#C0392B");
        LblResultado.Text = "No se pudo registrar";
        LblDetalle.Text = mensaje;
    }

    private void Ocupado(bool ocupado)
    {
        BtnRegistrar.IsEnabled = !ocupado;
        Cargando.IsRunning = Cargando.IsVisible = ocupado;
    }
}
