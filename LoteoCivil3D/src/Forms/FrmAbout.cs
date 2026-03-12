using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace LoteoCivil3D.Forms
{
    public class FrmAbout : Form
    {
        public FrmAbout()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Acerca de Loteo Civil3D";
            Size = new Size(400, 280);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 10);

            var version = Assembly.GetExecutingAssembly().GetName().Version;

            var lblTitle = new Label
            {
                Text = "Loteo Civil3D",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Location = new Point(20, 20),
                AutoSize = true,
                ForeColor = Color.FromArgb(0, 100, 180)
            };

            var lblVersion = new Label
            {
                Text = $"Version {version}",
                Location = new Point(22, 60),
                AutoSize = true
            };

            var lblDesc = new Label
            {
                Text = "Herramientas de grading y etiquetado\npara loteos residenciales en Civil 3D.\n\nDesarrollado para AutoCAD Civil 3D 2024.",
                Location = new Point(22, 90),
                Size = new Size(340, 80)
            };

            var lblAuthor = new Label
            {
                Text = "Autor: Anibal Uron",
                Location = new Point(22, 175),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };

            var btnOk = new Button
            {
                Text = "Aceptar",
                DialogResult = DialogResult.OK,
                Location = new Point(280, 200),
                Size = new Size(90, 30)
            };

            Controls.AddRange(new Control[] { lblTitle, lblVersion, lblDesc, lblAuthor, btnOk });
            AcceptButton = btnOk;
        }
    }
}
