namespace ec.edu.monster.vista;

/// <summary>Pantalla de bienvenida (1,5 s) con Sulley y Boo; después abre el login.</summary>
public class BienvenidaPage : ContentPage
{
    public BienvenidaPage()
    {
        NavigationPage.SetHasNavigationBar(this, false);
        Background = new LinearGradientBrush(new GradientStopCollection
        {
            new GradientStop(Color.FromArgb("#0B4F8A"), 0f),
            new GradientStop(Color.FromArgb("#082E54"), 1f)
        }, new Point(0, 0), new Point(1, 1));

        Content = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Spacing = 10,
            Padding = 24,
            Children =
            {
                new Border
                {
                    StrokeThickness = 3,
                    Stroke = Color.FromArgb("#29A8E0"),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 24 },
                    HeightRequest = 260,
                    WidthRequest = 300,
                    Content = new Image { Source = "monster.jpg", Aspect = Aspect.AspectFill }
                },
                new Label { Text = "MONSTER", FontSize = 34, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#29A8E0"), HorizontalTextAlignment = TextAlignment.Center, CharacterSpacing = 6, Margin = new Thickness(0, 14, 0, 0) },
                new Label { Text = "EurekaBank", FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center },
                new Label { Text = "SOAP .NET + SQL Server · ESPE", FontSize = 13, TextColor = Color.FromArgb("#C8DEF5"), HorizontalTextAlignment = TextAlignment.Center },
                new ActivityIndicator { IsRunning = true, Color = Color.FromArgb("#7AC943"), Margin = new Thickness(0, 18, 0, 0) }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.Delay(1500);
        if (Window != null)
        {
            Window.Page = new LoginPage();
        }
    }
}
