using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Text;

namespace Civil3D_TubeInfo
{
    public partial class TubeInfoForm : Form
    {
        private List<TubeData> _tubesData;
        private string _alignmentName;
        private DataGridView dataGridView;
        private Button btnExportar;
        private Button btnCerrar;
        private Button btnEstadisticas;
        private Button btnCopiar;
        private Label lblTitulo;
        private Label lblInfo;
        private Panel panelHeader;
        private Panel panelButtons;
        private TubeStatistics _statistics;

        public TubeInfoForm(List<TubeData> tubesData, string alignmentName)
        {
            _tubesData = tubesData;
            _alignmentName = alignmentName;
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Información de Tubos - Civil 3D";
            this.Size = new Size(900, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(700, 400);
            this.BackColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Panel de encabezado
            panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(0, 102, 204),
                Padding = new Padding(20, 10, 20, 10)
            };
            this.Controls.Add(panelHeader);

            // Label de título
            lblTitulo = new Label
            {
                Text = "INFORMACIÓN DE TUBOS",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 15),
                ForeColor = Color.White
            };
            panelHeader.Controls.Add(lblTitulo);

            // Label de información
            lblInfo = new Label
            {
                Text = $"Alineamiento: {_alignmentName} | Total de tubos: {_tubesData.Count}",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(20, 45),
                ForeColor = Color.FromArgb(220, 235, 255)
            };
            panelHeader.Controls.Add(lblInfo);

            // DataGridView
            dataGridView = new DataGridView
            {
                Location = new Point(20, 100),
                Size = new Size(840, 470),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                RowHeadersVisible = false,
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(240, 248, 255)
                },
                GridColor = Color.FromArgb(200, 200, 200)
            };

            // Estilo de las celdas
            dataGridView.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            dataGridView.DefaultCellStyle.ForeColor = Color.FromArgb(50, 50, 50);
            dataGridView.DefaultCellStyle.Padding = new Padding(5);
            dataGridView.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
            dataGridView.DefaultCellStyle.SelectionForeColor = Color.White;
            dataGridView.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            dataGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(45, 45, 48);
            dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dataGridView.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView.ColumnHeadersHeight = 40;
            dataGridView.RowTemplate.Height = 35;
            dataGridView.EnableHeadersVisualStyles = false;

            this.Controls.Add(dataGridView);

            // Panel de botones
            panelButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(20, 15, 20, 15)
            };
            this.Controls.Add(panelButtons);

            // Botón Estadísticas
            btnEstadisticas = new Button
            {
                Text = "📊 Estadísticas",
                Size = new Size(130, 40),
                Location = new Point(20, 20),
                BackColor = Color.FromArgb(51, 153, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnEstadisticas.FlatAppearance.BorderSize = 0;
            btnEstadisticas.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 180, 230);
            btnEstadisticas.Click += BtnEstadisticas_Click;
            panelButtons.Controls.Add(btnEstadisticas);

            // Botón Copiar
            btnCopiar = new Button
            {
                Text = "📋 Copiar",
                Size = new Size(130, 40),
                Location = new Point(160, 20),
                BackColor = Color.FromArgb(102, 102, 102),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCopiar.FlatAppearance.BorderSize = 0;
            btnCopiar.FlatAppearance.MouseOverBackColor = Color.FromArgb(130, 130, 130);
            btnCopiar.Click += BtnCopiar_Click;
            panelButtons.Controls.Add(btnCopiar);

            // Botón Exportar
            btnExportar = new Button
            {
                Text = "📄 Exportar a CSV",
                Size = new Size(160, 40),
                Location = new Point(540, 20),
                BackColor = Color.FromArgb(0, 153, 76),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExportar.FlatAppearance.BorderSize = 0;
            btnExportar.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 180, 90);
            btnExportar.Click += BtnExportar_Click;
            panelButtons.Controls.Add(btnExportar);

            // Botón Cerrar
            btnCerrar = new Button
            {
                Text = "✖ Cerrar",
                Size = new Size(130, 40),
                Location = new Point(710, 20),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.FlatAppearance.MouseOverBackColor = Color.FromArgb(200, 35, 51);
            btnCerrar.Click += (s, e) => this.Close();
            panelButtons.Controls.Add(btnCerrar);
        }

        private void LoadData()
        {
            // Configurar columnas - NUMERO, X, Y, Z
            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "NÚMERO",
                Name = "Numero",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle 
                { 
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                }
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "X (m)",
                Name = "CoordX",
                DefaultCellStyle = new DataGridViewCellStyle 
                { 
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Format = "N3"
                }
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Y (m)",
                Name = "CoordY",
                DefaultCellStyle = new DataGridViewCellStyle 
                { 
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Format = "N3"
                }
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Z (m)",
                Name = "CoordZ",
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Format = "N3"
                }
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Estación (PK)",
                Name = "Station",
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            });

            // Agregar filas con datos
            foreach (var tube in _tubesData)
            {
                dataGridView.Rows.Add(
                    tube.NumeroDisplay,
                    tube.XFormatted,
                    tube.YFormatted,
                    tube.ZFormatted,
                    tube.StationFormatted
                );
            }

            // Calcular estadísticas
            _statistics = new TubeStatistics(_tubesData);

            // Actualizar label de información con estadísticas
            lblInfo.Text = $"Alineamiento: {_alignmentName} | Total de tubos: {_tubesData.Count} | Z mín: {_statistics.MinZ:F2}m | Z máx: {_statistics.MaxZ:F2}m";
        }

        private void BtnExportar_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Archivo CSV|*.csv|Todos los archivos|*.*",
                    Title = "Guardar información de tubos",
                    FileName = $"Tubos_{_alignmentName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                    DefaultExt = "csv"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(
                        saveDialog.FileName, false, System.Text.Encoding.UTF8))
                    {
                        // Escribir encabezados
                        writer.WriteLine("NUMERO;X;Y;Z;ALINEAMIENTO");

                        // Escribir datos
                        foreach (var tube in _tubesData)
                        {
                            writer.WriteLine($"{tube.NumeroDisplay};{tube.X:F3};{tube.Y:F3};{tube.Z:F3};{_alignmentName}");
                        }
                    }

                    MessageBox.Show(
                        $"Datos exportados correctamente.\n\nArchivo: {saveDialog.FileName}\nRegistros: {_tubesData.Count}", 
                        "Exportación Exitosa",
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al exportar los datos:\n\n{ex.Message}",
                    "Error de Exportación",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnEstadisticas_Click(object sender, EventArgs e)
        {
            string estadisticas = _statistics.GetSummary();
            MessageBox.Show(estadisticas, "Estadísticas de Tubos", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnCopiar_Click(object sender, EventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                // Encabezados
                foreach (DataGridViewColumn col in dataGridView.Columns)
                {
                    sb.Append(col.HeaderText).Append("\t");
                }
                sb.AppendLine();

                // Filas visibles
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    if (row.Visible)
                    {
                        foreach (DataGridViewCell cell in row.Cells)
                        {
                            sb.Append(cell.Value ?? "").Append("\t");
                        }
                        sb.AppendLine();
                    }
                }

                System.Windows.Forms.Clipboard.SetText(sb.ToString());
                int rowCount = (dataGridView.SelectedRows.Count > 0) ? dataGridView.SelectedRows.Count : dataGridView.Rows.Count;
                MessageBox.Show($"Se copiaron {rowCount} filas al portapapeles.",
                    "Copiar Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al copiar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
