using System.Drawing;
using System.Windows.Forms;
using ec.edu.monster.modelo;

namespace ec.edu.monster.vista
{
    /// <summary>Colores, fuentes y controles con el mismo estilo en todas las ventanas.</summary>
    internal static class Tema
    {
        public static readonly Color Primario = Color.FromArgb(11, 79, 138);
        public static readonly Color PrimarioOscuro = Color.FromArgb(8, 46, 84);
        public static readonly Color Fondo = Color.FromArgb(242, 245, 249);
        public static readonly Color Tarjeta = Color.White;
        public static readonly Color Texto = Color.FromArgb(33, 37, 41);
        public static readonly Color TextoSuave = Color.FromArgb(108, 117, 125);
        public static readonly Color Borde = Color.FromArgb(222, 226, 230);
        public static readonly Color Error = Color.FromArgb(192, 57, 43);
        public static readonly Color Ingreso = Color.FromArgb(25, 135, 84);
        public static readonly Color Salida = Color.FromArgb(192, 57, 43);
        public static readonly Color Consulta = Color.FromArgb(13, 110, 253);
        public static readonly Color Transferencia = Color.FromArgb(111, 66, 193);

        // Paleta Monsters Inc.: Sulley (azul), Mike (verde), Boo (morado).
        public static readonly Color Sulley = Color.FromArgb(41, 168, 224);
        public static readonly Color Mike = Color.FromArgb(122, 201, 67);
        public static readonly Color Boo = Color.FromArgb(160, 92, 200);

        public static Font Fuente(float tamano, FontStyle estilo = FontStyle.Regular)
        {
            return new Font("Segoe UI", tamano, estilo);
        }

        public static Color ColorDe(TipoOperacion tipo)
        {
            switch (tipo)
            {
                case TipoOperacion.Deposito: return Ingreso;
                case TipoOperacion.Retiro: return Salida;
                default: return Transferencia;
            }
        }

        public static string IconoDe(TipoOperacion tipo)
        {
            switch (tipo)
            {
                case TipoOperacion.Deposito: return "+";
                case TipoOperacion.Retiro: return "−";
                default: return "⇄";
            }
        }

        public static Button Boton(string texto, Color fondo)
        {
            var boton = new Button
            {
                Text = texto,
                BackColor = fondo,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = Fuente(10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Height = 38
            };
            boton.FlatAppearance.BorderSize = 0;
            boton.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(fondo, 0.1f);
            return boton;
        }

        public static Label Etiqueta(string texto, float tamano = 9.5f, FontStyle estilo = FontStyle.Regular, Color? color = null)
        {
            return new Label
            {
                Text = texto,
                AutoSize = true,
                Font = Fuente(tamano, estilo),
                ForeColor = color ?? Texto,
                BackColor = Color.Transparent
            };
        }

        public static TextBox CajaTexto(int x, int y, int ancho)
        {
            return new TextBox { Location = new Point(x, y), Width = ancho, Font = Fuente(12f) };
        }
    }
}
