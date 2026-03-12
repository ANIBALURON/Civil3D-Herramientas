using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LoteoCivil3D.Forms
{
    public class FrmConsole : Form
    {
        private ListBox lstLog;
        private Button btnClose, btnCopy;

        public FrmConsole(string title, List<string> logEntries)
        {
            InitializeComponent();
            Text = "Loteo Civil3D - " + title;
            foreach (var entry in logEntries)
                lstLog.Items.Add(entry);
        }

        private void InitializeComponent()
        {
            Text = "Loteo Civil3D - Consola";
            Size = new Size(600, 450);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Consolas", 9);

            lstLog = new ListBox
            {
                Dock = DockStyle.Fill,
                HorizontalScrollbar = true,
                SelectionMode = SelectionMode.MultiExtended
            };
            Controls.Add(lstLog);

            var pnl = new Panel { Dock = DockStyle.Bottom, Height = 40 };
            btnClose = new Button { Text = "Cerrar", Location = new Point(490, 5), Size = new Size(80, 30) };
            btnCopy = new Button { Text = "Copiar Log", Location = new Point(10, 5), Size = new Size(100, 30) };
            btnClose.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            btnClose.Click += (s, e) => Close();
            btnCopy.Click += (s, e) =>
            {
                var lines = new List<string>();
                foreach (var item in lstLog.Items) lines.Add(item.ToString());
                if (lines.Count > 0)
                    Clipboard.SetText(string.Join(Environment.NewLine, lines));
            };
            pnl.Controls.AddRange(new Control[] { btnClose, btnCopy });
            Controls.Add(pnl);
        }
    }
}
