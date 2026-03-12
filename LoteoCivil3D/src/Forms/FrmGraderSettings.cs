using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LoteoCivil3D.Core;

namespace LoteoCivil3D.Forms
{
    public class FrmGraderSettings : Form
    {
        private readonly GraderSettings _settings;
        private readonly List<string> _surfaceNames;
        private readonly List<string> _featureLineStyles;

        // Controles - Tipo de lote
        private RadioButton rbWithPad, rbBackToBack;
        private GroupBox grpPadTab, grpRevealTab;
        private GroupBox grpBackToBack;
        private NumericUpDown nudB2BSlope, nudRidgeExtra;

        // Controles - Pendientes de lote
        private NumericUpDown nudLotSlopeMin, nudLotSlopeMax, nudLotSlopeDefault;

        // Controles - Pad
        private NumericUpDown nudPadOffset, nudPadElevOffset, nudPadSlope;
        private CheckBox chkPadSlopeAway;

        // Controles - Reveal
        private CheckBox chkUseReveal;
        private NumericUpDown nudRevealDist, nudRevealDrop;

        // Controles - Calle frontal
        private NumericUpDown nudFrontSetback, nudFrontSlope;
        private CheckBox chkMatchStreet;
        private RadioButton rbSourceSurface, rbSourceFeatureLine;

        // Controles - Drenaje posterior
        private NumericUpDown nudRearSlope, nudRearPatioSlope;
        private CheckBox chkDrainToRear;
        private RadioButton rbFrontToBack, rbBackToFront, rbHighToLow;

        // Controles - Superficie
        private ComboBox cmbEgSurface, cmbFgSurface;
        private CheckBox chkAddToSurface;
        private TextBox txtBreaklineGroup;

        // Controles - Estilos
        private ComboBox cmbLotStyle, cmbPadStyle;

        // Botones
        private Button btnOk, btnCancel, btnDefaults, btnExport, btnImport;

        public FrmGraderSettings(GraderSettings settings, List<string> surfaceNames, List<string> flStyles)
        {
            _settings = settings;
            _surfaceNames = surfaceNames;
            _featureLineStyles = flStyles;
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            Text = "Loteo Civil3D - Configuracion de Grading";
            Size = new Size(580, 800);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 9);

            var tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Padding = new Point(8, 4);

            // Tab 1: Pendientes y Grading
            var tabGrading = new TabPage("Grading");
            BuildGradingTab(tabGrading);
            tabControl.TabPages.Add(tabGrading);

            // Tab 2: Pad y Reveal
            var tabPad = new TabPage("Pad / Reveal");
            BuildPadTab(tabPad);
            tabControl.TabPages.Add(tabPad);

            // Tab 3: Superficie y Estilos
            var tabSurface = new TabPage("Superficie");
            BuildSurfaceTab(tabSurface);
            tabControl.TabPages.Add(tabSurface);

            // Panel de botones
            var pnlButtons = new Panel();
            pnlButtons.Dock = DockStyle.Bottom;
            pnlButtons.Height = 50;
            pnlButtons.Padding = new Padding(10);

            btnOk = new Button { Text = "Aceptar", DialogResult = DialogResult.OK, Size = new Size(90, 30) };
            btnCancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Size = new Size(90, 30) };
            btnDefaults = new Button { Text = "Defaults", Size = new Size(90, 30) };
            btnExport = new Button { Text = "Exportar", Size = new Size(80, 30) };
            btnImport = new Button { Text = "Importar", Size = new Size(80, 30) };

            btnOk.Location = new Point(pnlButtons.Width - 200, 10);
            btnCancel.Location = new Point(pnlButtons.Width - 100, 10);
            btnDefaults.Location = new Point(10, 10);
            btnExport.Location = new Point(110, 10);
            btnImport.Location = new Point(200, 10);

            btnOk.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            btnCancel.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            btnDefaults.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;

            btnOk.Click += BtnOk_Click;
            btnDefaults.Click += BtnDefaults_Click;
            btnExport.Click += BtnExport_Click;
            btnImport.Click += BtnImport_Click;

            pnlButtons.Controls.AddRange(new Control[] { btnOk, btnCancel, btnDefaults, btnExport, btnImport });

            Controls.Add(tabControl);
            Controls.Add(pnlButtons);
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void BuildGradingTab(TabPage tab)
        {
            tab.AutoScroll = true;
            int y = 15;

            // Grupo: Tipo de distribucion
            var grpLayout = CreateGroupBox(tab, "Tipo de Distribucion de Lotes", 10, y, 530, 75);
            rbWithPad = new RadioButton
            {
                Text = "Con Pad (casa interior, retiros, reveal)",
                Location = new Point(20, 22),
                AutoSize = true
            };
            rbBackToBack = new RadioButton
            {
                Text = "Espalda-Espalda (sin pad, lotes pegados, calle al frente)",
                Location = new Point(20, 46),
                AutoSize = true
            };
            rbWithPad.CheckedChanged += (s, e) => UpdateLayoutVisibility();
            rbBackToBack.CheckedChanged += (s, e) => UpdateLayoutVisibility();
            grpLayout.Controls.Add(rbWithPad);
            grpLayout.Controls.Add(rbBackToBack);

            y += 85;

            // Grupo: Pendientes de lote
            var grpSlopes = CreateGroupBox(tab, "Pendientes del Lote (%)", 10, y, 530, 120);
            y += 10;
            AddLabelAndNumeric(grpSlopes, "Pendiente minima:", 2, out nudLotSlopeMin, 0, 20, 1, 20, 30);
            AddLabelAndNumeric(grpSlopes, "Pendiente maxima:", 2, out nudLotSlopeMax, 0, 20, 1, 20, 60);
            AddLabelAndNumeric(grpSlopes, "Pendiente default:", 2, out nudLotSlopeDefault, 0, 20, 1, 20, 90);

            y += 130;

            // Grupo: Calle frontal
            var grpFront = CreateGroupBox(tab, "Calle Frontal - Fuente de Elevacion", 10, y, 530, 170);
            chkMatchStreet = new CheckBox { Text = "Empatar con elevacion de calle", Location = new Point(20, 25), AutoSize = true };
            grpFront.Controls.Add(chkMatchStreet);

            AddLabel(grpFront, "Fuente de elevacion:", 20, 52);
            rbSourceSurface = new RadioButton
            {
                Text = "Superficie (EG, corredor, etc.)",
                Location = new Point(200, 50),
                AutoSize = true
            };
            rbSourceFeatureLine = new RadioButton
            {
                Text = "Feature Line de calle (borde sardinel/pavimento)",
                Location = new Point(200, 72),
                AutoSize = true
            };
            grpFront.Controls.Add(rbSourceSurface);
            grpFront.Controls.Add(rbSourceFeatureLine);

            var lblSourceNote = new Label
            {
                Text = "Con FL de calle: al ejecutar LOTEO_GRADER le pedira seleccionar el borde de calle.",
                Location = new Point(20, 97),
                AutoSize = true,
                ForeColor = System.Drawing.Color.Gray,
                Font = new Font(Font.FontFamily, 8f)
            };
            grpFront.Controls.Add(lblSourceNote);

            AddLabelAndNumeric(grpFront, "Retiro frontal (m):", 2, out nudFrontSetback, 0, 50, 2, 20, 115);
            AddLabelAndNumeric(grpFront, "Pendiente a calle (%):", 2, out nudFrontSlope, 0, 20, 1, 20, 140);

            y += 180;

            // Grupo: Drenaje
            var grpDrain = CreateGroupBox(tab, "Drenaje y Direccion", 10, y, 530, 150);
            chkDrainToRear = new CheckBox { Text = "Drenar hacia atras", Location = new Point(20, 25), AutoSize = true };
            grpDrain.Controls.Add(chkDrainToRear);
            AddLabelAndNumeric(grpDrain, "Pendiente posterior (%):", 2, out nudRearSlope, 0, 20, 1, 20, 55);
            AddLabelAndNumeric(grpDrain, "Pendiente patio (%):", 2, out nudRearPatioSlope, 0, 20, 1, 20, 85);

            rbFrontToBack = new RadioButton { Text = "Frente a atras", Location = new Point(20, 115), AutoSize = true };
            rbBackToFront = new RadioButton { Text = "Atras a frente", Location = new Point(170, 115), AutoSize = true };
            rbHighToLow = new RadioButton { Text = "Alto a bajo", Location = new Point(320, 115), AutoSize = true };
            grpDrain.Controls.AddRange(new Control[] { rbFrontToBack, rbBackToFront, rbHighToLow });

            y += 160;

            // Grupo: Laterales
            var grpSide = CreateGroupBox(tab, "Laterales", 10, y, 530, 60);
            AddLabelAndNumeric(grpSide, "Pendiente lateral (%):", 2, out var nudSideSlope, 0, 20, 1, 20, 25);

            y += 70;

            // Grupo: Configuracion Espalda-Espalda
            grpBackToBack = CreateGroupBox(tab, "Espalda-Espalda (divisoria trasera)", 10, y, 530, 100);
            AddLabelAndNumeric(grpBackToBack, "Pendiente a divisoria (%):", 2, out nudB2BSlope, 0, 20, (decimal)0.5, 20, 25);
            AddLabelAndNumeric(grpBackToBack, "Elevacion extra en divisoria (m):", 2, out nudRidgeExtra, 0, 5, (decimal)0.05, 20, 55);
            var lblB2BNote = new Label
            {
                Text = "La divisoria es el punto mas alto entre lotes vecinos de atras.",
                Location = new Point(20, 80),
                AutoSize = true,
                ForeColor = System.Drawing.Color.Gray
            };
            grpBackToBack.Controls.Add(lblB2BNote);
        }

        private void BuildPadTab(TabPage tab)
        {
            tab.AutoScroll = true;
            int y = 15;

            // Grupo: Pad
            grpPadTab = CreateGroupBox(tab, "Plataforma (Pad)", 10, y, 530, 150);
            AddLabelAndNumeric(grpPadTab, "Offset desde lote (m):", 2, out nudPadOffset, 0, 20, 2, 20, 25);
            AddLabelAndNumeric(grpPadTab, "Elevacion sobre terreno (m):", 2, out nudPadElevOffset, 0, 5, 2, 20, 55);
            chkPadSlopeAway = new CheckBox { Text = "Pendiente desde centro del pad", Location = new Point(20, 90), AutoSize = true };
            grpPadTab.Controls.Add(chkPadSlopeAway);
            AddLabelAndNumeric(grpPadTab, "Pendiente del pad (%):", 2, out nudPadSlope, 0, 10, 1, 20, 115);

            y += 160;

            // Grupo: Reveal
            grpRevealTab = CreateGroupBox(tab, "Reveal (Retiro del Pad)", 10, y, 530, 120);
            chkUseReveal = new CheckBox { Text = "Crear reveal alrededor del pad", Location = new Point(20, 25), AutoSize = true };
            grpRevealTab.Controls.Add(chkUseReveal);
            AddLabelAndNumeric(grpRevealTab, "Distancia reveal (m):", 2, out nudRevealDist, 0, 5, 2, 20, 55);
            AddLabelAndNumeric(grpRevealTab, "Caida reveal (m):", 2, out nudRevealDrop, 0, 2, 2, 20, 85);

            y += 130;

            // Nota informativa para modo Back-to-Back
            var lblPadNote = new Label
            {
                Text = "Nota: En modo Espalda-Espalda estas opciones no aplican (no hay pad ni reveal).",
                Location = new Point(15, y),
                AutoSize = true,
                ForeColor = System.Drawing.Color.FromArgb(180, 80, 80),
                Font = new Font(Font, FontStyle.Italic)
            };
            tab.Controls.Add(lblPadNote);
        }

        private void UpdateLayoutVisibility()
        {
            bool isBackToBack = rbBackToBack.Checked;

            // Habilitar/deshabilitar controles de Pad y Reveal
            if (grpPadTab != null) grpPadTab.Enabled = !isBackToBack;
            if (grpRevealTab != null) grpRevealTab.Enabled = !isBackToBack;

            // Mostrar/ocultar grupo Back-to-Back en tab Grading
            if (grpBackToBack != null) grpBackToBack.Enabled = isBackToBack;
        }

        private void BuildSurfaceTab(TabPage tab)
        {
            tab.AutoScroll = true;
            int y = 15;

            // Grupo: Superficies
            var grpSurf = CreateGroupBox(tab, "Superficies", 10, y, 530, 140);
            AddLabel(grpSurf, "Superficie existente (EG):", 20, 25);
            cmbEgSurface = new ComboBox { Location = new Point(220, 22), Size = new Size(280, 25), DropDownStyle = ComboBoxStyle.DropDown };
            cmbEgSurface.Items.AddRange(_surfaceNames.ToArray());
            grpSurf.Controls.Add(cmbEgSurface);

            AddLabel(grpSurf, "Superficie de grading (FG):", 20, 55);
            cmbFgSurface = new ComboBox { Location = new Point(220, 52), Size = new Size(280, 25), DropDownStyle = ComboBoxStyle.DropDown };
            cmbFgSurface.Items.AddRange(_surfaceNames.ToArray());
            grpSurf.Controls.Add(cmbFgSurface);

            chkAddToSurface = new CheckBox { Text = "Agregar breaklines a superficie automaticamente", Location = new Point(20, 85), AutoSize = true };
            grpSurf.Controls.Add(chkAddToSurface);

            AddLabel(grpSurf, "Nombre grupo breaklines:", 20, 112);
            txtBreaklineGroup = new TextBox { Location = new Point(220, 109), Size = new Size(280, 25) };
            grpSurf.Controls.Add(txtBreaklineGroup);

            y += 150;

            // Grupo: Estilos de Feature Line
            var grpStyles = CreateGroupBox(tab, "Estilos de Feature Line", 10, y, 530, 100);
            AddLabel(grpStyles, "Estilo Lot Line:", 20, 25);
            cmbLotStyle = new ComboBox { Location = new Point(220, 22), Size = new Size(280, 25), DropDownStyle = ComboBoxStyle.DropDown };
            cmbLotStyle.Items.AddRange(_featureLineStyles.ToArray());
            grpStyles.Controls.Add(cmbLotStyle);

            AddLabel(grpStyles, "Estilo Pad:", 20, 55);
            cmbPadStyle = new ComboBox { Location = new Point(220, 52), Size = new Size(280, 25), DropDownStyle = ComboBoxStyle.DropDown };
            cmbPadStyle.Items.AddRange(_featureLineStyles.ToArray());
            grpStyles.Controls.Add(cmbPadStyle);
        }

        #region Helpers para UI

        private GroupBox CreateGroupBox(Control parent, string text, int x, int y, int w, int h)
        {
            var grp = new GroupBox { Text = text, Location = new Point(x, y), Size = new Size(w, h) };
            parent.Controls.Add(grp);
            return grp;
        }

        private void AddLabel(Control parent, string text, int x, int y)
        {
            var lbl = new Label { Text = text, Location = new Point(x, y), AutoSize = true };
            parent.Controls.Add(lbl);
        }

        private void AddLabelAndNumeric(Control parent, string labelText, int decimals,
            out NumericUpDown nud, decimal min, decimal max, decimal increment, int x, int y)
        {
            var lbl = new Label { Text = labelText, Location = new Point(x, y + 3), AutoSize = true };
            nud = new NumericUpDown
            {
                Location = new Point(350, y),
                Size = new Size(100, 25),
                DecimalPlaces = decimals,
                Minimum = min,
                Maximum = max,
                Increment = increment
            };
            parent.Controls.Add(lbl);
            parent.Controls.Add(nud);
        }

        #endregion

        private void LoadSettings()
        {
            // Tipo de distribucion
            if (_settings.LayoutType == LotLayoutType.BackToBack)
                rbBackToBack.Checked = true;
            else
                rbWithPad.Checked = true;

            // Back-to-back params
            nudB2BSlope.Value = (decimal)_settings.BackToBackSlope;
            nudRidgeExtra.Value = (decimal)_settings.RidgeExtraElevation;

            nudLotSlopeMin.Value = (decimal)_settings.LotSlopeMin;
            nudLotSlopeMax.Value = (decimal)_settings.LotSlopeMax;
            nudLotSlopeDefault.Value = (decimal)_settings.LotSlopeDefault;

            nudPadOffset.Value = (decimal)_settings.PadOffsetFromLot;
            nudPadElevOffset.Value = (decimal)_settings.PadElevationOffset;
            nudPadSlope.Value = (decimal)_settings.PadSlope;
            chkPadSlopeAway.Checked = _settings.PadSlopeAway;

            chkUseReveal.Checked = _settings.UseReveal;
            nudRevealDist.Value = (decimal)_settings.RevealDistance;
            nudRevealDrop.Value = (decimal)_settings.RevealDrop;

            nudFrontSetback.Value = (decimal)_settings.FrontSetback;
            nudFrontSlope.Value = (decimal)_settings.FrontSlopeToStreet;
            chkMatchStreet.Checked = _settings.MatchStreetElevation;

            if (_settings.StreetSource == StreetElevationSource.FeatureLine)
                rbSourceFeatureLine.Checked = true;
            else
                rbSourceSurface.Checked = true;

            nudRearSlope.Value = (decimal)_settings.RearSlope;
            nudRearPatioSlope.Value = (decimal)_settings.RearPatioSlope;
            chkDrainToRear.Checked = _settings.DrainToRear;

            switch (_settings.Direction)
            {
                case GradeDirection.FrontToBack: rbFrontToBack.Checked = true; break;
                case GradeDirection.BackToFront: rbBackToFront.Checked = true; break;
                case GradeDirection.HighToLow: rbHighToLow.Checked = true; break;
                default: rbFrontToBack.Checked = true; break;
            }

            cmbEgSurface.Text = _settings.SurfaceName;
            cmbFgSurface.Text = _settings.GradingSurfaceName;
            chkAddToSurface.Checked = _settings.AddToSurface;
            txtBreaklineGroup.Text = _settings.BreaklineGroupName;
            cmbLotStyle.Text = _settings.LotLineStyle;
            cmbPadStyle.Text = _settings.PadStyle;
        }

        private void SaveSettings()
        {
            // Tipo de distribucion
            _settings.LayoutType = rbBackToBack.Checked ? LotLayoutType.BackToBack : LotLayoutType.WithPad;
            _settings.BackToBackSlope = (double)nudB2BSlope.Value;
            _settings.RidgeExtraElevation = (double)nudRidgeExtra.Value;

            _settings.LotSlopeMin = (double)nudLotSlopeMin.Value;
            _settings.LotSlopeMax = (double)nudLotSlopeMax.Value;
            _settings.LotSlopeDefault = (double)nudLotSlopeDefault.Value;

            _settings.PadOffsetFromLot = (double)nudPadOffset.Value;
            _settings.PadElevationOffset = (double)nudPadElevOffset.Value;
            _settings.PadSlope = (double)nudPadSlope.Value;
            _settings.PadSlopeAway = chkPadSlopeAway.Checked;

            _settings.UseReveal = chkUseReveal.Checked;
            _settings.RevealDistance = (double)nudRevealDist.Value;
            _settings.RevealDrop = (double)nudRevealDrop.Value;

            _settings.FrontSetback = (double)nudFrontSetback.Value;
            _settings.FrontSlopeToStreet = (double)nudFrontSlope.Value;
            _settings.MatchStreetElevation = chkMatchStreet.Checked;
            _settings.StreetSource = rbSourceFeatureLine.Checked
                ? StreetElevationSource.FeatureLine
                : StreetElevationSource.Surface;

            _settings.RearSlope = (double)nudRearSlope.Value;
            _settings.RearPatioSlope = (double)nudRearPatioSlope.Value;
            _settings.DrainToRear = chkDrainToRear.Checked;

            if (rbFrontToBack.Checked) _settings.Direction = GradeDirection.FrontToBack;
            else if (rbBackToFront.Checked) _settings.Direction = GradeDirection.BackToFront;
            else if (rbHighToLow.Checked) _settings.Direction = GradeDirection.HighToLow;

            _settings.SurfaceName = cmbEgSurface.Text;
            _settings.GradingSurfaceName = cmbFgSurface.Text;
            _settings.AddToSurface = chkAddToSurface.Checked;
            _settings.BreaklineGroupName = txtBreaklineGroup.Text;
            _settings.LotLineStyle = cmbLotStyle.Text;
            _settings.PadStyle = cmbPadStyle.Text;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            SaveSettings();
            SettingsManager.SaveGrader();
        }

        private void BtnDefaults_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("¿Restaurar valores por defecto?", "Loteo Civil3D",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var defaults = new GraderSettings();
                // Copiar valores default
                _settings.LotSlopeMin = defaults.LotSlopeMin;
                _settings.LotSlopeMax = defaults.LotSlopeMax;
                _settings.LotSlopeDefault = defaults.LotSlopeDefault;
                _settings.PadOffsetFromLot = defaults.PadOffsetFromLot;
                _settings.PadElevationOffset = defaults.PadElevationOffset;
                _settings.PadSlope = defaults.PadSlope;
                _settings.PadSlopeAway = defaults.PadSlopeAway;
                _settings.UseReveal = defaults.UseReveal;
                _settings.RevealDistance = defaults.RevealDistance;
                _settings.RevealDrop = defaults.RevealDrop;
                _settings.FrontSetback = defaults.FrontSetback;
                _settings.FrontSlopeToStreet = defaults.FrontSlopeToStreet;
                _settings.MatchStreetElevation = defaults.MatchStreetElevation;
                _settings.RearSlope = defaults.RearSlope;
                _settings.RearPatioSlope = defaults.RearPatioSlope;
                _settings.DrainToRear = defaults.DrainToRear;
                _settings.Direction = defaults.Direction;
                _settings.LayoutType = defaults.LayoutType;
                _settings.BackToBackSlope = defaults.BackToBackSlope;
                _settings.RidgeExtraElevation = defaults.RidgeExtraElevation;
                _settings.StreetSource = defaults.StreetSource;
                LoadSettings();
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            SaveSettings();
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "XML Settings|*.xml";
                sfd.FileName = "LoteoSettings.xml";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    SettingsManager.ExportSettings(sfd.FileName);
                    MessageBox.Show("Configuracion exportada.", "Loteo Civil3D");
                }
            }
        }

        private void BtnImport_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "XML Settings|*.xml";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    SettingsManager.ImportSettings(ofd.FileName);
                    LoadSettings();
                    MessageBox.Show("Configuracion importada.", "Loteo Civil3D");
                }
            }
        }
    }
}
