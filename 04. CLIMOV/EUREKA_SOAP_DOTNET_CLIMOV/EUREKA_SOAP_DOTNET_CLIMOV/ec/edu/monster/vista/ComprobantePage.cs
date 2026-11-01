using ec.edu.monster.modelo;
using Microsoft.Maui.Controls.Shapes;

namespace ec.edu.monster.vista;

/// <summary>
/// Recibo de la operación (ventana modal): importe, sello APROBADO, movimiento, cuenta, fecha, empleado y nuevo saldo.
/// "Compartir" lo envía como texto (WhatsApp, correo…); "Nueva operación" lo cierra.
/// </summary>
public class ComprobantePage : ContentPage
{
    /// <param name="numeroMovimiento">0 si no se pudo consultar.</param>
    public ComprobantePage(TipoOperacion tipo, string cuenta, string? destino, Cuenta? datos, int numeroMovimiento,
                           decimal importe, decimal saldo, string empleado)
    {
        Color color = Formato.ColorDe(tipo);
        string simbolo = Formato.Simbolo(datos?.Moneda);
        string titulo = Formato.Titulo(tipo) + (tipo == TipoOperacion.Transferencia ? " registrada" : " registrado");
        DateTime fecha = DateTime.Now;
        BackgroundColor = Color.FromRgba(8, 46, 84, 140);

        var filas = new List<(string, string)>();
        if (numeroMovimiento > 0) filas.Add(("Movimiento N.º", numeroMovimiento.ToString()));
        filas.Add((destino != null ? "Cuenta origen" : "Cuenta", Formato.NumeroTarjeta(cuenta).Replace("  ", " ")));
        if (datos != null) filas.Add(("Titular", datos.Cliente));
        if (destino != null) filas.Add(("Cuenta destino", Formato.NumeroTarjeta(destino).Replace("  ", " ")));
        filas.Add(("Fecha y hora", fecha.ToString("dd/MM/yyyy · HH:mm:ss")));
        filas.Add(("Empleado", empleado));

        var detalle = new VerticalStackLayout { Spacing = 0, Padding = new Thickness(20, 6) };
        foreach (var (etiqueta, valor) in filas)
        {
            var g = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) }, Padding = new Thickness(0, 8) };
            g.Add(new Label { Text = etiqueta, TextColor = Tema.Recurso("TextoSuave"), FontSize = 13 }, 0, 0);
            g.Add(new Label { Text = valor, TextColor = Tema.Recurso("Texto"), FontSize = 13, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.End, LineBreakMode = LineBreakMode.TailTruncation }, 1, 0);
            detalle.Add(g);
            detalle.Add(new BoxView { HeightRequest = 1, Color = Tema.Recurso("Borde") });
        }
        var saldoFila = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) }, Padding = new Thickness(0, 10) };
        saldoFila.Add(new Label { Text = "Nuevo saldo", TextColor = Tema.Recurso("TextoSuave"), FontSize = 14, VerticalOptions = LayoutOptions.Center }, 0, 0);
        saldoFila.Add(new Label { Text = simbolo + " " + Formato.Dinero(saldo), TextColor = Tema.Recurso("Texto"), FontSize = 20, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.End }, 1, 0);
        detalle.Add(saldoFila);

        var compartir = new Button { Text = "Compartir", BackgroundColor = Tema.Recurso("Fondo"), TextColor = Color.FromArgb("#0B4F8A"), FontAttributes = FontAttributes.Bold, CornerRadius = 10 };
        compartir.Clicked += async (s, e) => await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Comprobante EurekaBank",
            Text = $"MONSTER · EUREKA BANK\n{titulo}\nImporte: {simbolo} {Formato.Dinero(importe)}\n"
                 + string.Join("\n", filas.Select(f => f.Item1 + ": " + f.Item2))
                 + $"\nNuevo saldo: {simbolo} {Formato.Dinero(saldo)}"
        });
        var cerrar = new Button { Text = "Nueva operación", BackgroundColor = color, TextColor = Colors.White, FontAttributes = FontAttributes.Bold, CornerRadius = 10 };
        cerrar.Clicked += async (s, e) => await Navigation.PopModalAsync();
        var botones = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 10, Padding = new Thickness(20, 8, 20, 20) };
        botones.Add(compartir, 0, 0);
        botones.Add(cerrar, 1, 0);

        var cabecera = new VerticalStackLayout
        {
            BackgroundColor = color,
            Padding = new Thickness(16, 18, 16, 34),
            Spacing = 4,
            Children =
            {
                new Border
                {
                    BackgroundColor = Colors.White, StrokeThickness = 0, StrokeShape = new Ellipse(), WidthRequest = 54, HeightRequest = 54,
                    HorizontalOptions = LayoutOptions.Center,
                    Content = new Label { Text = "✓", FontSize = 28, FontAttributes = FontAttributes.Bold, TextColor = color, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
                },
                new Label { Text = titulo, FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center },
                new Label { Text = "MONSTER · EUREKA BANK", FontSize = 11, TextColor = Color.FromRgba(255, 255, 255, 210), HorizontalTextAlignment = TextAlignment.Center, CharacterSpacing = 1 }
            }
        };
        var monto = new Border
        {
            BackgroundColor = Tema.Recurso("Tarjeta"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.15f, Radius = 10, Offset = new Point(0, 4) },
            Margin = new Thickness(60, -22, 60, 0),
            Padding = 10,
            Content = new VerticalStackLayout
            {
                Children =
                {
                    new Label { Text = "IMPORTE", FontSize = 11, TextColor = Tema.Recurso("TextoSuave"), HorizontalTextAlignment = TextAlignment.Center },
                    new Label { Text = (tipo == TipoOperacion.Deposito ? "+ " : "− ") + simbolo + " " + Formato.Dinero(importe), FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = color, HorizontalTextAlignment = TextAlignment.Center }
                }
            }
        };
        var sello = new Border
        {
            Stroke = Color.FromArgb("#198754"), StrokeThickness = 3, StrokeShape = new RoundRectangle { CornerRadius = 6 },
            Padding = new Thickness(8, 2), Rotation = -14, HorizontalOptions = LayoutOptions.End, Margin = new Thickness(0, -14, 18, 0), Opacity = 0.8,
            Content = new Label { Text = "APROBADO", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#198754"), CharacterSpacing = 1 }
        };

        Content = new Border
        {
            BackgroundColor = Tema.Recurso("Tarjeta"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            Margin = new Thickness(18),
            VerticalOptions = LayoutOptions.Center,
            Content = new VerticalStackLayout { Children = { cabecera, monto, sello, detalle, botones } }
        };
    }
}
