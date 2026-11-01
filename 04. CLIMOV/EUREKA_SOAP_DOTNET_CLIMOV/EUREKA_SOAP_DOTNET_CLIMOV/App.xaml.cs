using ec.edu.monster.vista;

namespace ec.edu.monster;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		UserAppTheme = AppTheme.Light;
		Tema.Aplicar(); // modo claro u oscuro guardado en el teléfono
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new BienvenidaPage());
	}
}
