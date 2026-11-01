using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ec.edu.monster.vista
{
    /// <summary>Pantalla de bienvenida (1,5 s) con la imagen de Sulley y Boo antes del login.</summary>
    public class FormBienvenida : Form
    {
        private readonly Image _monster = Formato.Monster();

        public FormBienvenida()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(620, 340);
            ShowInTaskbar = false;
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.Dpi;

            var reloj = new Timer { Interval = 1500 };
            reloj.Tick += (s, e) => { reloj.Stop(); Close(); };
            Shown += (s, e) => reloj.Start();
            Click += (s, e) => Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var todo = ClientRectangle;
            using (var fondo = new LinearGradientBrush(todo, Tema.Primario, Tema.PrimarioOscuro, 35f))
            {
                g.FillRectangle(fondo, todo);
            }
            using (var brillo = new SolidBrush(Color.FromArgb(60, Tema.Boo)))
            {
                g.FillEllipse(brillo, 420, 180, 300, 260);
            }
            if (_monster != null)
            {
                g.DrawImage(_monster, new Rectangle(0, 0, 300, 340));
            }
            TextRenderer.DrawText(g, "MONSTER", Tema.Fuente(30f, FontStyle.Bold), new Point(318, 84), Tema.Sulley);
            TextRenderer.DrawText(g, "EurekaBank", Tema.Fuente(20f, FontStyle.Bold), new Point(322, 140), Color.White);
            TextRenderer.DrawText(g, "SOAP .NET + SQL Server · ESPE", Tema.Fuente(9.5f), new Point(324, 182), Color.FromArgb(200, 222, 245));
            TextRenderer.DrawText(g, "Cargando cliente de escritorio…", Tema.Fuente(8.5f), new Point(324, 290), Tema.Mike);
        }
    }
}
