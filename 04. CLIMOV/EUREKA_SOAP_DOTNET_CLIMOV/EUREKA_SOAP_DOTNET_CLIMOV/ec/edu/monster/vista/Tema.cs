namespace ec.edu.monster.vista;

/// <summary>
/// Modo claro / oscuro. Las pantallas usan {DynamicResource Fondo, Tarjeta, Texto, TextoSuave, Borde}:
/// al cambiar esos recursos aquí, todas se repintan. La elección se guarda en el teléfono.
/// </summary>
public static class Tema
{
    private const string Clave = "modo_oscuro";

    public static bool Oscuro => Preferences.Default.Get(Clave, false);

    /// <summary>Aplica el modo guardado (se llama al abrir la app).</summary>
    public static void Aplicar() => Pintar(Oscuro);

    public static void Alternar()
    {
        Preferences.Default.Set(Clave, !Oscuro);
        Pintar(Oscuro);
    }

    /// <summary>Color actual de un recurso (para lo que se arma desde el código, como la lista de movimientos).</summary>
    public static Color Recurso(string clave) =>
        Application.Current?.Resources.TryGetValue(clave, out object? valor) == true && valor is Color c ? c : Colors.Gray;

    private static void Pintar(bool oscuro)
    {
        ResourceDictionary? r = Application.Current?.Resources;
        if (r == null) return;
        r["Fondo"] = Color.FromArgb(oscuro ? "#0B1426" : "#F2F5F9");
        r["Tarjeta"] = Color.FromArgb(oscuro ? "#13213B" : "#FFFFFF");
        r["Texto"] = Color.FromArgb(oscuro ? "#E8EEFB" : "#212529");
        r["TextoSuave"] = Color.FromArgb(oscuro ? "#8FA3C7" : "#6C757D");
        r["Borde"] = Color.FromArgb(oscuro ? "#24395F" : "#DEE2E6");
        r["Primary"] = Color.FromArgb(oscuro ? "#1A2B4B" : "#0B4F8A");
    }
}
