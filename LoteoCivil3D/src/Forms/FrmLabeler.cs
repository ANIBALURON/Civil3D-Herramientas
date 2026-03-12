using System;
using System.Drawing;
using System.Windows.Forms;
using LoteoCivil3D.Core;

namespace LoteoCivil3D.Forms
{
    public class FrmLabeler : Form
    {
        private readonly LabelerSettings _settings;

        private CheckBox chkLotElev, chkHighPoints, chkPadElev, chkRevealElev, chkSlopes, chkLotNumbers;
        private NumericUpDown nudTextHeight, nudDecimals;
        private ComboBox cmbTextStyle;
        private Button btnOk, btnCancel;

        public FrmLabeler(LabelerSettings settings)
        {
            _settings = settings;
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            Text = "Loteo Civil3D - Etiquetado";
            Size = new Size(420, 420);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 9);

            // Grupo: Que etiquetar
            var grpWhat = new GroupBox { Text = "Elementos a etiquetar", Location = new Point(10, 10), Size = new Size(380, 190) };

            chkLotElev = new CheckBox { Text = "Elevaciones de vertices del lote", Location = new Point(20, 25), AutoSize = true };
            chkHighPoints = new CheckBox { Text = "Puntos altos (HP)", Location = new Point(20, 50), AutoSize = true };
            chkPadElev = new CheckBox { Text = "Elevaciones de pad", Location = new Point(20, 75), AutoSize = true };
            chkRevealElev = new CheckBox { Text = "Elevaciones de reveal", Location = new Point(20, 100), AutoSize = true };
            chkSlopes = new CheckBox { Text = "Pendientes entre vertices (%)", Location = new Point(20, 125), AutoSize = true };
            chkLotNumbers = new CheckBox { Text = "Numeros de lote", Location = new Point(20, 150), AutoSize = true };

            grpWhat.Controls.AddRange(new Control[] { chkLotElev, chkHighPoints, chkPadElev, chkRevealElev, chkSlopes, chkLotNumbers });
            Controls.Add(grpWhat);

            // Grupo: Formato
            var grpFormat = new GroupBox { Text = "Formato", Location = new Point(10, 210), Size = new Size(380, 120) };

            var lblHeight = new Label { Text = "Altura de texto (m):", Location = new Point(20, 30), AutoSize = true };
            nudTextHeight = new NumericUpDown
            {
                Location = new Point(250, 27), Size = new Size(100, 25),
                DecimalPlaces = 3, Minimum = 0.01m, Maximum = 10m, Increment = 0.01m, Value = 0.10m
            };

            var lblDec = new Label { Text = "Decimales:", Location = new Point(20, 60), AutoSize = true };
            nudDecimals = new NumericUpDown
            {
                Location = new Point(250, 57), Size = new Size(100, 25),
                Minimum = 0, Maximum = 6, Value = 2
            };

            var lblStyle = new Label { Text = "Estilo de texto:", Location = new Point(20, 90), AutoSize = true };
            cmbTextStyle = new ComboBox
            {
                Location = new Point(250, 87), Size = new Size(100, 25),
                DropDownStyle = ComboBoxStyle.DropDown, Text = "Standard"
            };
            cmbTextStyle.Items.AddRange(new[] { "Standard", "Annotative", "ROMANS", "ARIAL" });

            grpFormat.Controls.AddRange(new Control[] { lblHeight, nudTextHeight, lblDec, nudDecimals, lblStyle, cmbTextStyle });
            Controls.Add(grpFormat);

            // Botones
            btnOk = new Button { Text = "Etiquetar", DialogResult = DialogResult.OK, Location = new Point(200, 345), Size = new Size(90, 30) };
            btnCancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(300, 345), Size = new Size(90, 30) };

            btnOk.Click += BtnOk_Click;
            Controls.AddRange(new Control[] { btnOk, btnCancel });

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void LoadSettings()
        {
            chkLotElev.Checked = _settings.LabelLotElevations;
            chkHighPoints.Checked = _settings.LabelHighPoints;
            chkPadElev.Checked = _settings.LabelPadElevations;
            chkRevealElev.Checked = _settings.LabelRevealElevations;
            chkSlopes.Checked = _settings.LabelSlopes;
            chkLotNumbers.Checked = _settings.LabelLotNumbers;
            nudTextHeight.Value = (decimal)_settings.TextHeight;
            nudDecimals.Value = _settings.DecimalPlaces;
            cmbTextStyle.Text = _settings.TextStyle;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            _settings.LabelLotElevations = chkLotElev.Checked;
            _settings.LabelHighPoints = chkHighPoints.Checked;
            _settings.LabelPadElevations = chkPadElev.Checked;
            _settings.LabelRevealElevations = chkRevealElev.Checked;
            _settings.LabelSlopes = chkSlopes.Checked;
            _settings.LabelLotNumbers = chkLotNumbers.Checked;
            _settings.TextHeight = (double)nudTextHeight.Value;
            _settings.DecimalPlaces = (int)nudDecimals.Value;
            _settings.TextStyle = cmbTextStyle.Text;

            SettingsManager.SaveLabeler();
        }
    }
}
