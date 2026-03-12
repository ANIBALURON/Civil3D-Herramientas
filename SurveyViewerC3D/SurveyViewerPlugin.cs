// ============================================================
// Survey Viewer - Plugin para Civil 3D
// Visualizador de bases de datos Survey SQLite
// Comando: SURVEYVIEWER
// ============================================================

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using SysDataTable = System.Data.DataTable;
using DataColumn = System.Data.DataColumn;
using DataRow = System.Data.DataRow;
using Exception = System.Exception;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;

[assembly: CommandClass(typeof(SurveyViewerC3D.Commands))]

namespace SurveyViewerC3D
{
    public class Commands
    {
        [CommandMethod("SURVEYVIEWER")]
        public void ShowSurveyViewer()
        {
            var win = new SurveyViewerWindow();
            Autodesk.AutoCAD.ApplicationServices.Application.ShowModelessWindow(win);
        }
    }

    public class SurveyViewerWindow : Window
    {
        // Colores del tema
        static readonly SolidColorBrush BgDark = BrushFromHex("#1a1a2e");
        static readonly SolidColorBrush BgMedium = BrushFromHex("#16213e");
        static readonly SolidColorBrush BgLight = BrushFromHex("#0f3460");
        static readonly SolidColorBrush Accent = BrushFromHex("#e94560");
        static readonly SolidColorBrush AccentHover = BrushFromHex("#ff6b81");
        static readonly SolidColorBrush TextWhite = BrushFromHex("#ffffff");
        static readonly SolidColorBrush TextGray = BrushFromHex("#a0a0b0");
        static readonly SolidColorBrush TextLight = BrushFromHex("#d0d0e0");
        static readonly SolidColorBrush TableBg = BrushFromHex("#0d1b2a");
        static readonly SolidColorBrush TableStripe = BrushFromHex("#1b2838");
        static readonly SolidColorBrush EntryBg = BrushFromHex("#1b2838");
        static readonly SolidColorBrush SuccessGreen = BrushFromHex("#2ecc71");
        static readonly SolidColorBrush BtnGreen = BrushFromHex("#27ae60");
        static readonly SolidColorBrush BtnGreenHover = BrushFromHex("#2ecc71");
        static readonly SolidColorBrush HeaderBg = BrushFromHex("#0f3460");
        static readonly SolidColorBrush BorderColor = BrushFromHex("#2a2a4a");

        // Columnas visibles para tabla Point
        static readonly string[] PointVisibleCols = { "Id", "Number", "Easting", "Northing", "Elevation", "Description", "ImportEventId" };
        static readonly string[] PointExportCols = { "Number", "Easting", "Northing", "Elevation", "Description" };
        static readonly string[] PointExportHeaders = { "PUNTO", "ESTE (X)", "NORTE (Y)", "ELEVACION (Z)", "DESCRIPCION" };

        // Controles
        private DataGrid dataGrid;
        private TextBlock lblFile, lblStatus, lblCount, lblRows, lblCols;
        private TextBlock lblStatElev, lblStatNorth, lblStatEast;
        private ComboBox comboTables, comboDesc, comboGroup;
        private TextBox txtSearch;
        private ListBox tableListBox;
        private StackPanel descSummaryPanel;

        // Datos
        private string dbPath;
        private SQLiteConnection conn;
        private SysDataTable fullData;
        private string currentTable;
        private List<string> tableNames = new List<string>();
        private Dictionary<int, string> importEventNames = new Dictionary<int, string>();
        private bool suppressTableEvents = false;

        public SurveyViewerWindow()
        {
            Title = "Survey Database Viewer - Civil 3D";
            Width = 1350;
            Height = 800;
            MinWidth = 700;
            MinHeight = 550;
            Background = BgDark;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            BuildUI();
        }

        // --------------------------------------------------------
        // CONSTRUIR INTERFAZ
        // --------------------------------------------------------
        private void BuildUI()
        {
            var mainStack = new DockPanel();
            Content = mainStack;

            // === HEADER ===
            var header = new Border
            {
                Background = BgMedium, Height = 70,
                Padding = new Thickness(20, 10, 20, 10)
            };
            DockPanel.SetDock(header, Dock.Top);
            mainStack.Children.Add(header);

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.Child = headerGrid;

            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            titleStack.Children.Add(new TextBlock
            {
                Text = "SURVEY DATABASE VIEWER",
                FontSize = 20, FontWeight = FontWeights.Bold, Foreground = TextWhite
            });
            titleStack.Children.Add(new TextBlock
            {
                Text = "Civil 3D - Visualizador de Levantamiento",
                FontSize = 11, Foreground = TextGray
            });
            Grid.SetColumn(titleStack, 0);
            headerGrid.Children.Add(titleStack);

            var btnOpen = CreateButton("📂  ABRIR BASE DE DATOS", Accent, AccentHover);
            btnOpen.Click += (s, e) => OpenDatabase();
            btnOpen.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(btnOpen, 1);
            headerGrid.Children.Add(btnOpen);

            // === TOOLBAR (2 filas) ===
            var toolbarOuter = new StackPanel
            {
                Background = BgDark,
                Margin = new Thickness(10, 6, 10, 0)
            };
            DockPanel.SetDock(toolbarOuter, Dock.Top);
            mainStack.Children.Add(toolbarOuter);

            // --- Fila 1: Archivo + Filtros + Buscar ---
            var row1 = new WrapPanel
            {
                Margin = new Thickness(5, 4, 5, 2),
                VerticalAlignment = VerticalAlignment.Center
            };
            toolbarOuter.Children.Add(row1);

            lblFile = new TextBlock
            {
                Text = "Ningún archivo",
                Foreground = Accent, FontSize = 11, FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            row1.Children.Add(lblFile);

            AddToolbarLabel(row1, "TABLA:");
            comboTables = CreateDarkCombo(120);
            comboTables.SelectionChanged += OnTableComboChanged;
            row1.Children.Add(comboTables);

            AddToolbarLabel(row1, "GRUPO:");
            comboGroup = CreateDarkCombo(140);
            comboGroup.SelectionChanged += OnGroupFilterChanged;
            row1.Children.Add(comboGroup);

            AddToolbarLabel(row1, "DESC:");
            comboDesc = CreateDarkCombo(110);
            comboDesc.IsEditable = true;
            comboDesc.IsTextSearchEnabled = false;
            comboDesc.Items.Add("TODAS");
            comboDesc.SelectedIndex = 0;
            comboDesc.SelectionChanged += OnDescFilterChanged;
            // Aplicar estilo al TextBox interno del ComboBox editable
            comboDesc.Loaded += (s, ev) =>
            {
                var tb = comboDesc.Template.FindName("PART_EditableTextBox", comboDesc) as TextBox;
                if (tb != null)
                {
                    tb.Background = EntryBg;
                    tb.Foreground = TextWhite;
                    tb.CaretBrush = TextWhite;
                }
            };
            comboDesc.KeyUp += OnDescTextKeyUp;
            row1.Children.Add(comboDesc);

            var lblSearch = new TextBlock
            {
                Text = "🔍", FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 3, 0)
            };
            row1.Children.Add(lblSearch);

            txtSearch = new TextBox
            {
                Width = 120, FontSize = 11,
                Background = EntryBg, Foreground = TextWhite,
                CaretBrush = TextWhite,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(6, 4, 6, 4),
                VerticalAlignment = VerticalAlignment.Center
            };
            txtSearch.TextChanged += OnSearchChanged;
            row1.Children.Add(txtSearch);

            // --- Fila 2: Botones de accion ---
            var row2 = new WrapPanel
            {
                Margin = new Thickness(5, 2, 5, 4),
                VerticalAlignment = VerticalAlignment.Center
            };
            toolbarOuter.Children.Add(row2);

            var btnZoom = CreateButton("🎯 ZOOM", BrushFromHex("#8e44ad"), BrushFromHex("#9b59b6"));
            btnZoom.Click += (s, e) => ZoomToSelectedPoint();
            btnZoom.Margin = new Thickness(0, 0, 3, 0);
            row2.Children.Add(btnZoom);

            var btnDraw = CreateButton("✏ DIBUJAR", BrushFromHex("#2980b9"), BrushFromHex("#3498db"));
            btnDraw.Click += (s, e) => DrawPointsOnScreen();
            btnDraw.Margin = new Thickness(0, 0, 3, 0);
            row2.Children.Add(btnDraw);

            var btnRemoveMarks = CreateButton("🗑 BORRAR", BrushFromHex("#c0392b"), BrushFromHex("#e74c3c"));
            btnRemoveMarks.Click += (s, e) => RemoveDrawnPoints();
            btnRemoveMarks.Margin = new Thickness(0, 0, 3, 0);
            row2.Children.Add(btnRemoveMarks);

            var btnExportPoints = CreateButton("📍 EXPORTAR", BtnGreen, BtnGreenHover);
            btnExportPoints.Click += (s, e) => ExportPointsCsv();
            btnExportPoints.Margin = new Thickness(0, 0, 3, 0);
            row2.Children.Add(btnExportPoints);

            var btnExportCsv = CreateButton("📊 CSV", Accent, AccentHover);
            btnExportCsv.Click += (s, e) => ExportFullCsv();
            btnExportCsv.Margin = new Thickness(0, 0, 3, 0);
            row2.Children.Add(btnExportCsv);

            // === STATUS BAR ===
            var statusBar = new Border
            {
                Background = BgMedium, Height = 30,
                Padding = new Thickness(10, 0, 15, 0)
            };
            DockPanel.SetDock(statusBar, Dock.Bottom);
            mainStack.Children.Add(statusBar);

            var statusGrid = new Grid();
            statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            statusBar.Child = statusGrid;

            lblStatus = new TextBlock
            {
                Text = "Listo — Comando: SURVEYVIEWER",
                Foreground = TextGray, FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lblStatus, 0);
            statusGrid.Children.Add(lblStatus);

            lblCount = new TextBlock
            {
                Text = "", Foreground = Accent,
                FontSize = 10, FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lblCount, 1);
            statusGrid.Children.Add(lblCount);

            // === CONTENIDO PRINCIPAL ===
            var contentGrid = new Grid { Margin = new Thickness(15, 10, 15, 10) };
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mainStack.Children.Add(contentGrid);

            // --- PANEL IZQUIERDO ---
            var leftPanel = new Border { Background = BgMedium, CornerRadius = new CornerRadius(4) };
            Grid.SetColumn(leftPanel, 0);
            contentGrid.Children.Add(leftPanel);

            var leftScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            leftPanel.Child = leftScroll;

            var leftStack = new StackPanel { Margin = new Thickness(10) };
            leftScroll.Content = leftStack;

            // Titulo tablas
            leftStack.Children.Add(new TextBlock
            {
                Text = "TABLAS", FontSize = 12, FontWeight = FontWeights.Bold,
                Foreground = Accent, Margin = new Thickness(0, 5, 0, 10)
            });

            tableListBox = new ListBox
            {
                Background = BgMedium, Foreground = TextLight,
                BorderThickness = new Thickness(0), FontSize = 11,
                MaxHeight = 180
            };
            tableListBox.SelectionChanged += OnTableListChanged;
            leftStack.Children.Add(tableListBox);

            // Info
            leftStack.Children.Add(CreatePanelTitle("INFORMACIÓN"));
            lblRows = new TextBlock { Text = "Filas: -", Foreground = TextGray, FontSize = 10, Margin = new Thickness(8, 2, 0, 0) };
            lblCols = new TextBlock { Text = "Columnas: -", Foreground = TextGray, FontSize = 10, Margin = new Thickness(8, 2, 0, 0) };
            leftStack.Children.Add(lblRows);
            leftStack.Children.Add(lblCols);

            // Estadisticas
            leftStack.Children.Add(CreatePanelTitle("ESTADÍSTICAS"));
            lblStatElev = new TextBlock { Text = "Z: -", Foreground = TextGray, FontSize = 10, Margin = new Thickness(8, 2, 0, 0) };
            lblStatNorth = new TextBlock { Text = "N: -", Foreground = TextGray, FontSize = 10, Margin = new Thickness(8, 2, 0, 0) };
            lblStatEast = new TextBlock { Text = "E: -", Foreground = TextGray, FontSize = 10, Margin = new Thickness(8, 2, 0, 0) };
            leftStack.Children.Add(lblStatElev);
            leftStack.Children.Add(lblStatNorth);
            leftStack.Children.Add(lblStatEast);

            // Resumen por tipo
            leftStack.Children.Add(CreatePanelTitle("RESUMEN POR TIPO"));
            descSummaryPanel = new StackPanel();
            leftStack.Children.Add(descSummaryPanel);

            // --- PANEL DERECHO (DataGrid) ---
            var rightPanel = new Border { Background = TableBg, CornerRadius = new CornerRadius(4) };
            Grid.SetColumn(rightPanel, 2);
            contentGrid.Children.Add(rightPanel);

            dataGrid = new DataGrid
            {
                AutoGenerateColumns = false,  // Manual para controlar columnas visibles
                IsReadOnly = true,
                Background = TableBg, Foreground = TextLight,
                BorderThickness = new Thickness(0),
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = BorderColor,
                RowBackground = TableBg,
                AlternatingRowBackground = TableStripe,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                SelectionMode = DataGridSelectionMode.Single,
                FontSize = 11,
                CanUserSortColumns = true,
                ColumnHeaderHeight = 32,
                RowHeight = 28
            };

            // Estilo encabezados
            var headerStyle = new Style(typeof(DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, HeaderBg));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, TextWhite));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontSizeProperty, 11.0));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.PaddingProperty, new Thickness(8, 4, 8, 4)));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, BorderColor));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
            dataGrid.ColumnHeaderStyle = headerStyle;

            // Estilo celdas
            var cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(DataGridCell.PaddingProperty, new Thickness(8, 2, 8, 2)));
            cellStyle.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(0)));
            var selTrig = new Trigger { Property = DataGridCell.IsSelectedProperty, Value = true };
            selTrig.Setters.Add(new Setter(DataGridCell.BackgroundProperty, Accent));
            selTrig.Setters.Add(new Setter(DataGridCell.ForegroundProperty, TextWhite));
            cellStyle.Triggers.Add(selTrig);
            dataGrid.CellStyle = cellStyle;

            // Menu contextual
            var ctxMenu = new ContextMenu
            {
                Background = BgMedium, Foreground = TextWhite, BorderBrush = BorderColor
            };
            var miCopyCell = new MenuItem { Header = "Copiar celda" };
            miCopyCell.Click += CopyCell;
            var miCopyRow = new MenuItem { Header = "Copiar fila completa" };
            miCopyRow.Click += CopyRow;
            var miCopyCoords = new MenuItem { Header = "Copiar N, E, Z" };
            miCopyCoords.Click += CopyCoords;
            var miZoom = new MenuItem { Header = "Zoom al punto en Civil 3D" };
            miZoom.Click += (s, e) => ZoomToSelectedPoint();
            ctxMenu.Items.Add(miCopyCell);
            ctxMenu.Items.Add(miCopyRow);
            ctxMenu.Items.Add(miCopyCoords);
            ctxMenu.Items.Add(new Separator());
            ctxMenu.Items.Add(miZoom);
            dataGrid.ContextMenu = ctxMenu;

            // Doble clic = zoom
            dataGrid.MouseDoubleClick += (s, e) => ZoomToSelectedPoint();

            rightPanel.Child = dataGrid;
        }

        // --------------------------------------------------------
        // HELPERS UI
        // --------------------------------------------------------
        static SolidColorBrush BrushFromHex(string hex)
        {
            var c = (Color)ColorConverter.ConvertFromString(hex);
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        Button CreateButton(string text, SolidColorBrush bg, SolidColorBrush hoverBg)
        {
            var btn = new Button
            {
                Content = text, FontSize = 11, FontWeight = FontWeights.Bold,
                Foreground = TextWhite, Background = bg,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(12, 6, 12, 6),
                Cursor = Cursors.Hand
            };
            btn.MouseEnter += (s, e) => btn.Background = hoverBg;
            btn.MouseLeave += (s, e) => btn.Background = bg;
            return btn;
        }

        Border CreateSeparator()
        {
            return new Border { Width = 2, Background = BorderColor, Margin = new Thickness(5, 4, 5, 4) };
        }

        void AddToolbarLabel(Panel parent, string text)
        {
            var lbl = new TextBlock
            {
                Text = text, FontWeight = FontWeights.Bold,
                Foreground = TextGray, FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 3, 0)
            };
            parent.Children.Add(lbl);
        }

        ComboBox CreateDarkCombo(double width)
        {
            var combo = new ComboBox
            {
                Width = width, FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0),
                Background = EntryBg,
                Foreground = TextWhite,
                BorderBrush = BorderColor,
                BorderThickness = new Thickness(1)
            };

            // Estilo para items del dropdown (fondo oscuro, texto claro)
            var itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, BgMedium));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, TextLight));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.PaddingProperty, new Thickness(6, 4, 6, 4)));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.BorderThicknessProperty, new Thickness(0)));
            // Hover
            var hoverTrigger = new Trigger { Property = ComboBoxItem.IsHighlightedProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, Accent));
            hoverTrigger.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, TextWhite));
            itemStyle.Triggers.Add(hoverTrigger);
            // Selected
            var selTrigger = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
            selTrigger.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, BgLight));
            selTrigger.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, TextWhite));
            itemStyle.Triggers.Add(selTrigger);

            combo.ItemContainerStyle = itemStyle;

            // Forzar fondo oscuro en el popup del dropdown
            combo.Resources.Add(SystemColors.WindowBrushKey, BgMedium);
            combo.Resources.Add(SystemColors.HighlightBrushKey, Accent);
            combo.Resources.Add(SystemColors.HighlightTextBrushKey, TextWhite);

            return combo;
        }

        TextBlock CreatePanelTitle(string text)
        {
            return new TextBlock
            {
                Text = text, FontWeight = FontWeights.Bold,
                Foreground = Accent, FontSize = 10,
                Margin = new Thickness(0, 12, 0, 5)
            };
        }

        // --------------------------------------------------------
        // ABRIR BASE DE DATOS
        // --------------------------------------------------------
        private void OpenDatabase()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar base de datos SQLite",
                Filter = "SQLite Database|*.sqlite;*.db;*.sqlite3|Todos|*.*",
                InitialDirectory = @"C:\Users\Public\Documents\Autodesk"
            };
            if (dlg.ShowDialog() != true) return;
            LoadDatabase(dlg.FileName);
        }

        private void LoadDatabase(string path)
        {
            if (conn != null) { conn.Close(); conn.Dispose(); }

            try
            {
                conn = new SQLiteConnection($"Data Source={path};Read Only=False;");
                conn.Open();
                dbPath = path;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo abrir:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string filename = Path.GetFileName(path);
            lblFile.Text = $"📁 {filename}";
            lblFile.Foreground = SuccessGreen;
            Title = $"Survey Database Viewer — {filename}";

            // Cargar nombres de ImportEvent
            LoadImportEventNames();

            // Cargar tablas
            tableNames.Clear();
            comboTables.Items.Clear();
            tableListBox.Items.Clear();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) tableNames.Add(reader.GetString(0));
                }
            }

            foreach (var t in tableNames)
            {
                int count = 0;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT COUNT(*) FROM \"{t}\"";
                    count = Convert.ToInt32(cmd.ExecuteScalar());
                }
                comboTables.Items.Add(t);
                tableListBox.Items.Add($"  {t}  ({count})");
            }

            SetStatus($"Base de datos cargada: {tableNames.Count} tablas");

            int idx = tableNames.IndexOf("Point");
            if (idx >= 0) comboTables.SelectedIndex = idx;
            else if (tableNames.Count > 0) comboTables.SelectedIndex = 0;
        }

        // --------------------------------------------------------
        // CARGAR NOMBRES DE IMPORTEVENT (grupos)
        // --------------------------------------------------------
        private void LoadImportEventNames()
        {
            importEventNames.Clear();
            if (conn == null) return;

            try
            {
                // Leer tabla ImportEvent para obtener nombres
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='ImportEvent'";
                    if (cmd.ExecuteScalar() == null) return;
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, Name FROM ImportEvent ORDER BY Id";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            string name = reader.IsDBNull(1) ? $"Grupo {id}" : reader.GetString(1);
                            if (string.IsNullOrWhiteSpace(name)) name = $"Grupo {id}";
                            importEventNames[id] = name;
                        }
                    }
                }
            }
            catch { }
        }

        // --------------------------------------------------------
        // CARGAR TABLA
        // --------------------------------------------------------
        private void LoadTable(string tableName)
        {
            if (conn == null || string.IsNullOrEmpty(tableName)) return;

            currentTable = tableName;
            txtSearch.Text = "";

            try
            {
                fullData = new SysDataTable();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT * FROM \"{tableName}\"";
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(fullData);
                    }
                }

                // Configurar columnas del DataGrid
                dataGrid.Columns.Clear();

                bool isPointTable = fullData.Columns.Contains("Northing")
                    && fullData.Columns.Contains("Easting")
                    && fullData.Columns.Contains("Elevation");

                if (isPointTable)
                {
                    // Solo mostrar columnas importantes + nombre de grupo
                    foreach (string colName in PointVisibleCols)
                    {
                        if (!fullData.Columns.Contains(colName)) continue;

                        string header = colName;
                        if (colName == "ImportEventId") header = "GRUPO";
                        else if (colName == "Number") header = "PUNTO";

                        var dgCol = new DataGridTextColumn
                        {
                            Header = header,
                            Binding = new Binding(colName),
                            Width = colName == "Description" ? new DataGridLength(1, DataGridLengthUnitType.Star)
                                : colName == "Id" ? new DataGridLength(60)
                                : colName == "Number" ? new DataGridLength(80)
                                : colName == "ImportEventId" ? new DataGridLength(80)
                                : new DataGridLength(130)
                        };
                        dataGrid.Columns.Add(dgCol);
                    }
                }
                else
                {
                    // Para otras tablas, mostrar todas
                    foreach (DataColumn col in fullData.Columns)
                    {
                        var dgCol = new DataGridTextColumn
                        {
                            Header = col.ColumnName,
                            Binding = new Binding(col.ColumnName)
                        };
                        dataGrid.Columns.Add(dgCol);
                    }
                }

                dataGrid.ItemsSource = fullData.DefaultView;

                lblRows.Text = $"Filas: {fullData.Rows.Count:N0}";
                lblCols.Text = $"Columnas: {fullData.Columns.Count}";
                lblCount.Text = $"{fullData.Rows.Count:N0} registros";
                SetStatus($"Tabla '{tableName}' — {fullData.Rows.Count:N0} registros");

                // Reset filtros
                suppressTableEvents = true;
                comboDesc.Items.Clear();
                comboDesc.Items.Add("TODAS");
                comboDesc.SelectedIndex = 0;

                comboGroup.Items.Clear();
                comboGroup.Items.Add("TODOS");
                comboGroup.SelectedIndex = 0;
                suppressTableEvents = false;

                UpdateGroupCombo();
                UpdateStats();
                UpdateDescSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al leer tabla:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --------------------------------------------------------
        // LLENAR COMBO DE GRUPOS (ImportEvent)
        // --------------------------------------------------------
        private void UpdateGroupCombo()
        {
            if (fullData == null || !fullData.Columns.Contains("ImportEventId")) return;

            var groups = fullData.AsEnumerable()
                .Where(r => r["ImportEventId"] != DBNull.Value)
                .Select(r => Convert.ToInt32(r["ImportEventId"]))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            suppressTableEvents = true;
            foreach (int gId in groups)
            {
                string name = importEventNames.ContainsKey(gId) ? importEventNames[gId] : $"Grupo {gId}";
                // Contar puntos en este grupo
                int count = fullData.AsEnumerable()
                    .Count(r => r["ImportEventId"] != DBNull.Value && Convert.ToInt32(r["ImportEventId"]) == gId);
                comboGroup.Items.Add($"{gId}: {name} ({count})");
            }
            suppressTableEvents = false;
        }

        // --------------------------------------------------------
        // ESTADISTICAS
        // --------------------------------------------------------
        private void UpdateStats()
        {
            if (fullData == null) return;
            UpdateStatLabel(lblStatElev, "Elevation", "Z");
            UpdateStatLabel(lblStatNorth, "Northing", "N");
            UpdateStatLabel(lblStatEast, "Easting", "E");
        }

        private void UpdateStatLabel(TextBlock lbl, string colName, string prefix)
        {
            if (!fullData.Columns.Contains(colName)) { lbl.Text = $"{prefix}: N/A"; return; }

            var vals = fullData.AsEnumerable()
                .Where(r => r[colName] != DBNull.Value)
                .Select(r => Convert.ToDouble(r[colName]))
                .ToList();

            if (vals.Count == 0) { lbl.Text = $"{prefix}: -"; return; }

            if (prefix == "Z")
                lbl.Text = $"Z: {vals.Min():F2} ~ {vals.Max():F2}\n   Prom: {vals.Average():F2}";
            else
                lbl.Text = $"{prefix}: {vals.Min():F2} ~ {vals.Max():F2}";
        }

        // --------------------------------------------------------
        // RESUMEN POR DESCRIPCION
        // --------------------------------------------------------
        private void UpdateDescSummary()
        {
            descSummaryPanel.Children.Clear();
            if (fullData == null || !fullData.Columns.Contains("Description")) return;

            var groups = fullData.AsEnumerable()
                .GroupBy(r => r["Description"] == DBNull.Value ? "(vacío)" : r["Description"].ToString().Trim())
                .Select(g => new { Desc = g.Key == "" ? "(vacío)" : g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            foreach (var g in groups)
            {
                descSummaryPanel.Children.Add(new TextBlock
                {
                    Text = $"  {g.Desc,-14} {g.Count,5}",
                    Foreground = TextGray, FontSize = 10,
                    FontFamily = new FontFamily("Consolas")
                });
                if (!comboDesc.Items.Contains(g.Desc))
                    comboDesc.Items.Add(g.Desc);
            }
        }

        // --------------------------------------------------------
        // APLICAR FILTROS
        // --------------------------------------------------------
        private void ApplyFilters()
        {
            if (fullData == null || suppressTableEvents) return;

            // Obtener texto del combo DESC (puede ser seleccion o texto escrito)
            string descFilter = comboDesc.Text?.Trim() ?? "TODAS";
            if (string.IsNullOrEmpty(descFilter)) descFilter = "TODAS";
            string groupFilter = comboGroup.SelectedItem?.ToString() ?? "TODOS";
            string searchText = txtSearch.Text?.Trim().ToLower() ?? "";

            var view = fullData.DefaultView;
            var filterParts = new List<string>();

            // Filtro por grupo (ImportEventId)
            if (groupFilter != "TODOS" && fullData.Columns.Contains("ImportEventId"))
            {
                string idStr = groupFilter.Split(':')[0].Trim();
                int groupId;
                if (int.TryParse(idStr, out groupId))
                    filterParts.Add($"ImportEventId = {groupId}");
            }

            // Filtro por descripcion (soporta prefijos: escribir "TB" filtra TB100, TB205, etc.)
            if (descFilter != "TODAS" && fullData.Columns.Contains("Description"))
            {
                if (descFilter == "(vacío)")
                    filterParts.Add("(Description IS NULL OR Description = '')");
                else
                {
                    string escaped = descFilter.Replace("'", "''");
                    // Si es un valor exacto del combo, filtrar exacto. Si no, usar LIKE prefijo
                    bool isExactMatch = false;
                    foreach (var item in comboDesc.Items)
                    {
                        if (item.ToString() == descFilter) { isExactMatch = true; break; }
                    }
                    if (isExactMatch)
                        filterParts.Add($"Description = '{escaped}'");
                    else
                        filterParts.Add($"Description LIKE '{escaped}%'");
                }
            }

            // Filtro por busqueda
            if (!string.IsNullOrEmpty(searchText))
            {
                var searchParts = new List<string>();
                foreach (DataColumn col in fullData.Columns)
                {
                    if (col.DataType == typeof(string))
                        searchParts.Add($"{col.ColumnName} LIKE '%{searchText.Replace("'", "''")}%'");
                    else
                        searchParts.Add($"Convert({col.ColumnName}, 'System.String') LIKE '%{searchText.Replace("'", "''")}%'");
                }
                filterParts.Add("(" + string.Join(" OR ", searchParts) + ")");
            }

            string rowFilter = string.Join(" AND ", filterParts);

            try { view.RowFilter = rowFilter; }
            catch { view.RowFilter = ""; }

            dataGrid.ItemsSource = view;
            lblCount.Text = $"{view.Count:N0} registros";
            SetStatus($"Mostrando {view.Count:N0} de {fullData.Rows.Count:N0} registros");
        }

        // --------------------------------------------------------
        // EVENTOS
        // --------------------------------------------------------
        private void OnTableComboChanged(object sender, SelectionChangedEventArgs e)
        {
            if (suppressTableEvents || comboTables.SelectedItem == null) return;
            string table = comboTables.SelectedItem.ToString();
            LoadTable(table);
            int idx = tableNames.IndexOf(table);
            if (idx >= 0 && tableListBox.SelectedIndex != idx)
            {
                suppressTableEvents = true;
                tableListBox.SelectedIndex = idx;
                suppressTableEvents = false;
            }
        }

        private void OnTableListChanged(object sender, SelectionChangedEventArgs e)
        {
            if (suppressTableEvents || tableListBox.SelectedIndex < 0) return;
            string text = tableListBox.SelectedItem.ToString().Trim();
            string tableName = text.Split('(')[0].Trim();
            int idx = tableNames.IndexOf(tableName);
            if (idx >= 0 && comboTables.SelectedIndex != idx)
                comboTables.SelectedIndex = idx;
        }

        private void OnGroupFilterChanged(object sender, SelectionChangedEventArgs e) { ApplyFilters(); }
        private void OnDescFilterChanged(object sender, SelectionChangedEventArgs e) { ApplyFilters(); }
        private void OnDescTextKeyUp(object sender, KeyEventArgs e)
        {
            // Cuando el usuario escribe en el ComboBox editable, filtrar por prefijo
            if (e.Key == Key.Enter || e.Key == Key.Return)
                ApplyFilters();
        }
        private void OnSearchChanged(object sender, TextChangedEventArgs e) { ApplyFilters(); }

        // --------------------------------------------------------
        // COPIAR
        // --------------------------------------------------------
        private void CopyCell(object sender, RoutedEventArgs e)
        {
            if (dataGrid.CurrentCell.Item == null) return;
            var row = dataGrid.CurrentCell.Item as DataRowView;
            if (row == null) return;
            string colName = dataGrid.CurrentCell.Column?.Header?.ToString();
            // Mapear header a nombre real de columna
            var binding = (dataGrid.CurrentCell.Column as DataGridTextColumn)?.Binding as Binding;
            string bindPath = binding?.Path?.Path ?? colName;
            if (bindPath != null && row.Row.Table.Columns.Contains(bindPath))
            {
                Clipboard.SetText(row[bindPath]?.ToString() ?? "");
                SetStatus("Celda copiada al portapapeles");
            }
        }

        private void CopyRow(object sender, RoutedEventArgs e)
        {
            var row = dataGrid.SelectedItem as DataRowView;
            if (row == null) return;
            // Copiar solo las columnas visibles
            var vals = new List<string>();
            foreach (var col in dataGrid.Columns)
            {
                var binding = (col as DataGridTextColumn)?.Binding as Binding;
                string bindPath = binding?.Path?.Path;
                if (bindPath != null && row.Row.Table.Columns.Contains(bindPath))
                    vals.Add(row[bindPath]?.ToString() ?? "");
            }
            Clipboard.SetText(string.Join("\t", vals));
            SetStatus("Fila copiada al portapapeles");
        }

        private void CopyCoords(object sender, RoutedEventArgs e)
        {
            var row = dataGrid.SelectedItem as DataRowView;
            if (row == null) return;
            var dt = row.Row.Table;
            string num = dt.Columns.Contains("Number") ? row["Number"]?.ToString() : "?";
            string n = dt.Columns.Contains("Northing") ? row["Northing"]?.ToString() : "?";
            string east = dt.Columns.Contains("Easting") ? row["Easting"]?.ToString() : "?";
            string z = dt.Columns.Contains("Elevation") ? row["Elevation"]?.ToString() : "?";
            Clipboard.SetText($"Pto: {num}, N: {n}, E: {east}, Z: {z}");
            SetStatus("Coordenadas copiadas al portapapeles");
        }

        // Nombre del grupo de puntos temporal
        private const string TempGroupName = "SURVEY_VIEWER_TEMP";
        // Lista de ObjectIds de puntos COGO creados por esta sesion
        private List<ObjectId> createdCogoPointIds = new List<ObjectId>();

        // Mapeo de código de descripción → layer (prefijo, se busca el más largo que coincida)
        static readonly Dictionary<string, string> DescToLayer = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"AC", "C3D_PTS_LINEA_AGUA"},
            {"APA", "C3D_PTS_AUTOPISTA"},
            {"AR", "C3D_PTS_ARROYOS"},
            {"CA", "C3D_PTS_CAMINOS"},
            {"CATA", "C3D_CATAS"},
            {"CC", "C3D_PTS_CANALES"},
            {"CDI", "C3D_PTS_CAMINOS"},
            {"CD", "C3D_PTS_LINEA_EXISTENTE"},
            {"CES", "C3D_PTS_CAMINOS"},
            {"CEV", "C3D_PTS_CAMINOS"},
            {"CE", "C3D_PTS_CERCAS"},
            {"CM", "C3D_PTS_CAMINOS"},
            {"CTV", "C3D_PTS_CAMINOS"},
            {"CT", "C3D_PTS_CANALES"},
            {"CU", "C3D_PTS_CUNETA"},
            {"DR", "C3D_PTS_LINEA_AGUA"},
            {"ES", "C3D_PTS_ARROYOS"},
            {"FC", "C3D_PTS_FERROCARRIL"},
            {"HB", "C3D_HOMBRO_DDV"},
            {"FS", "C3D_PTS_FIBRA_OPTICA"},
            {"LES", "C3D_PTS_FIBRA_OPTICA"},
            {"MJ", "C3D_PTS_ESTACION"},
            {"PG", "C3D_PTS_CAMINOS"},
            {"RI", "C3D_PTS_RIO"},
            {"TB", "C3D_TUBO_TENDIDO"},
            {"TE", "C3D_PTS_TUBERIA_EXISTENTE"},
            {"TN", "C3D_PUNTOS_TERRENO_NATURAL"},
            {"VP", "C3D_PTS_VIA_PAVIMENTADA"},
        };

        /// Busca el layer correspondiente a una descripción (por prefijo más largo)
        static string GetLayerForDescription(string desc)
        {
            if (string.IsNullOrEmpty(desc)) return null;
            desc = desc.Trim();
            // Buscar el prefijo más largo que coincida
            string bestMatch = null;
            int bestLen = 0;
            foreach (var kvp in DescToLayer)
            {
                if (desc.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase) && kvp.Key.Length > bestLen)
                {
                    bestMatch = kvp.Value;
                    bestLen = kvp.Key.Length;
                }
            }
            return bestMatch;
        }

        /// Asegura que un layer exista en el dibujo, lo crea si no existe
        static ObjectId EnsureLayer(Autodesk.AutoCAD.DatabaseServices.Database database,
            Autodesk.AutoCAD.DatabaseServices.Transaction tr, string layerName)
        {
            var lt = (LayerTable)tr.GetObject(database.LayerTableId, OpenMode.ForRead);
            if (lt.Has(layerName))
                return lt[layerName];

            // Crear el layer
            lt.UpgradeOpen();
            var lr = new LayerTableRecord { Name = layerName };
            var id = lt.Add(lr);
            tr.AddNewlyCreatedDBObject(lr, true);
            return id;
        }

        // --------------------------------------------------------
        // ZOOM AL PUNTO - usa SendStringToExecute (funciona desde modeless)
        // --------------------------------------------------------
        private void ZoomToSelectedPoint()
        {
            var row = dataGrid.SelectedItem as DataRowView;
            if (row == null)
            {
                MessageBox.Show("Seleccione un punto en la tabla.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dt = row.Row.Table;
            if (!dt.Columns.Contains("Easting") || !dt.Columns.Contains("Northing"))
            {
                MessageBox.Show("La tabla no tiene coordenadas.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                double x = Convert.ToDouble(row["Easting"]);
                double y = Convert.ToDouble(row["Northing"]);
                string num = dt.Columns.Contains("Number") ? row["Number"]?.ToString() : "";

                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (doc == null) return;

                // Zoom Window 30m x 30m centrado en el punto
                double h = 15;
                string zoomCmd = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "_.ZOOM _W {0},{1} {2},{3}\n", x - h, y - h, x + h, y + h);
                doc.SendStringToExecute(zoomCmd, true, false, false);

                SetStatus($"Zoom al punto {num}: E={x:F3}, N={y:F3}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al hacer zoom:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --------------------------------------------------------
        // DIBUJAR PUNTOS COGO de Civil 3D
        // --------------------------------------------------------
        private void DrawPointsOnScreen()
        {
            if (fullData == null || !fullData.Columns.Contains("Easting"))
            {
                MessageBox.Show("No hay puntos para dibujar.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var view = dataGrid.ItemsSource as DataView;
            if (view == null || view.Count == 0)
            {
                MessageBox.Show("No hay puntos visibles para dibujar.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirmResult = MessageBox.Show(
                $"Se crearán {view.Count:N0} puntos COGO en Civil 3D.\n\n" +
                $"Se asignarán al grupo '{TempGroupName}'.\n" +
                "Use 'BORRAR MARCAS' para eliminarlos.\n\n¿Continuar?",
                "Crear Puntos COGO", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmResult != MessageBoxResult.Yes) return;

            try
            {
                var db = doc.Database;
                createdCogoPointIds.Clear();

                double minE = double.MaxValue, maxE = double.MinValue;
                double minN = double.MaxValue, maxN = double.MinValue;

                using (var docLock = doc.LockDocument())
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    // Obtener CivilDocument y coleccion de puntos COGO
                    var civilDoc = CivilDocument.GetCivilDocument(db);
                    var cogoPoints = civilDoc.CogoPoints;

                    int drawn = 0;
                    foreach (DataRowView drv in view)
                    {
                        if (drv["Easting"] == DBNull.Value || drv["Northing"] == DBNull.Value)
                            continue;

                        double x = Convert.ToDouble(drv["Easting"]);
                        double y = Convert.ToDouble(drv["Northing"]);
                        double z = fullData.Columns.Contains("Elevation") && drv["Elevation"] != DBNull.Value
                            ? Convert.ToDouble(drv["Elevation"]) : 0;

                        uint ptNum = 0;
                        if (fullData.Columns.Contains("Number") && drv["Number"] != DBNull.Value)
                        {
                            try { ptNum = Convert.ToUInt32(drv["Number"]); } catch { }
                        }

                        string desc = fullData.Columns.Contains("Description") && drv["Description"] != DBNull.Value
                            ? drv["Description"].ToString() : "";

                        // Crear punto COGO
                        var pt3d = new Point3d(x, y, z);
                        ObjectId ptId;

                        if (ptNum > 0)
                        {
                            try
                            {
                                ptId = cogoPoints.Add(pt3d, false);
                                var cogoPt = (CogoPoint)tr.GetObject(ptId, OpenMode.ForWrite);
                                try { cogoPt.PointNumber = ptNum; } catch { }
                                cogoPt.RawDescription = desc;
                            }
                            catch
                            {
                                ptId = cogoPoints.Add(pt3d, true);
                                var cogoPt = (CogoPoint)tr.GetObject(ptId, OpenMode.ForWrite);
                                cogoPt.RawDescription = desc;
                            }
                        }
                        else
                        {
                            ptId = cogoPoints.Add(pt3d, true);
                            var cogoPt = (CogoPoint)tr.GetObject(ptId, OpenMode.ForWrite);
                            cogoPt.RawDescription = desc;
                        }

                        // Asignar layer según descripción
                        string targetLayer = GetLayerForDescription(desc);
                        if (targetLayer != null)
                        {
                            var layerId = EnsureLayer(db, tr, targetLayer);
                            var entity = (Autodesk.AutoCAD.DatabaseServices.Entity)tr.GetObject(ptId, OpenMode.ForWrite);
                            entity.LayerId = layerId;
                        }

                        createdCogoPointIds.Add(ptId);

                        if (x < minE) minE = x; if (x > maxE) maxE = x;
                        if (y < minN) minN = y; if (y > maxN) maxN = y;
                        drawn++;
                    }

                    // Crear o actualizar grupo de puntos
                    try
                    {
                        var pointGroups = civilDoc.PointGroups;
                        ObjectId groupId = ObjectId.Null;

                        // Buscar si ya existe el grupo
                        foreach (ObjectId gId in pointGroups)
                        {
                            var pg = (PointGroup)tr.GetObject(gId, OpenMode.ForRead);
                            if (pg.Name == TempGroupName)
                            {
                                groupId = gId;
                                break;
                            }
                        }

                        if (groupId == ObjectId.Null)
                        {
                            groupId = pointGroups.Add(TempGroupName);
                        }

                        // Configurar el grupo para incluir los puntos creados
                        var group = (PointGroup)tr.GetObject(groupId, OpenMode.ForWrite);
                        // Construir lista de numeros de punto para el query
                        var ptNums = new List<uint>();
                        foreach (ObjectId pid in createdCogoPointIds)
                        {
                            try
                            {
                                var cp = (CogoPoint)tr.GetObject(pid, OpenMode.ForRead);
                                ptNums.Add(cp.PointNumber);
                            }
                            catch { }
                        }

                        if (ptNums.Count > 0)
                        {
                            // Incluir por rango de numeros
                            var stdQuery = new StandardPointGroupQuery();
                            var ranges = BuildPointRanges(ptNums);
                            stdQuery.IncludeNumbers = ranges;
                            group.SetQuery(stdQuery);
                            group.Update();
                        }
                    }
                    catch { } // Si falla el grupo, los puntos ya estan creados

                    tr.Commit();

                    // Zoom a los puntos
                    if (drawn > 0 && minE < maxE && minN < maxN)
                    {
                        double extent = Math.Max(maxE - minE, maxN - minN);
                        double margin = extent * 0.1;
                        if (margin < 20) margin = 20;
                        string zoomCmd = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                            "_.ZOOM _W {0},{1} {2},{3}\n",
                            minE - margin, minN - margin, maxE + margin, maxN + margin);
                        doc.SendStringToExecute(zoomCmd, true, false, false);
                    }

                    SetStatus($"{drawn:N0} puntos COGO creados en grupo '{TempGroupName}'");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear puntos COGO:\n{ex.Message}\n\n{ex.StackTrace}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Construir string de rangos para PointGroup query (ej: "1-5,8,10-15")
        private string BuildPointRanges(List<uint> numbers)
        {
            if (numbers.Count == 0) return "";
            numbers.Sort();

            var ranges = new List<string>();
            uint start = numbers[0];
            uint end = numbers[0];

            for (int i = 1; i < numbers.Count; i++)
            {
                if (numbers[i] == end + 1)
                {
                    end = numbers[i];
                }
                else
                {
                    ranges.Add(start == end ? start.ToString() : $"{start}-{end}");
                    start = numbers[i];
                    end = numbers[i];
                }
            }
            ranges.Add(start == end ? start.ToString() : $"{start}-{end}");
            return string.Join(",", ranges);
        }

        // --------------------------------------------------------
        // BORRAR PUNTOS COGO TEMPORALES
        // --------------------------------------------------------
        private void RemoveDrawnPoints()
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            if (createdCogoPointIds.Count == 0)
            {
                MessageBox.Show("No hay puntos COGO temporales para borrar.\n\n" +
                    "Solo se pueden borrar puntos creados en esta sesión del visor.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirmResult = MessageBox.Show(
                $"Se eliminarán {createdCogoPointIds.Count:N0} puntos COGO del grupo '{TempGroupName}'.\n\n" +
                "Esto NO borra nada de la base de datos SQLite.\n¿Continuar?",
                "Borrar Puntos COGO", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmResult != MessageBoxResult.Yes) return;

            try
            {
                var db = doc.Database;
                int deleted = 0;

                using (var docLock = doc.LockDocument())
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    // Borrar los puntos COGO
                    foreach (ObjectId ptId in createdCogoPointIds)
                    {
                        try
                        {
                            if (!ptId.IsNull && !ptId.IsErased)
                            {
                                var ent = tr.GetObject(ptId, OpenMode.ForWrite);
                                ent.Erase();
                                deleted++;
                            }
                        }
                        catch { }
                    }

                    // Intentar borrar el grupo de puntos
                    try
                    {
                        var civilDoc = CivilDocument.GetCivilDocument(db);
                        var pointGroups = civilDoc.PointGroups;
                        foreach (ObjectId gId in pointGroups)
                        {
                            var pg = (PointGroup)tr.GetObject(gId, OpenMode.ForRead);
                            if (pg.Name == TempGroupName)
                            {
                                pg.UpgradeOpen();
                                pg.Erase();
                                break;
                            }
                        }
                    }
                    catch { }

                    tr.Commit();
                }

                createdCogoPointIds.Clear();
                SetStatus($"{deleted:N0} puntos COGO eliminados");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al borrar puntos:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --------------------------------------------------------
        // EXPORTAR PUNTOS (solo PUNTO, X, Y, Z, DESC)
        // --------------------------------------------------------
        private void ExportPointsCsv()
        {
            if (fullData == null || fullData.Rows.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var missing = PointExportCols.Where(c => !fullData.Columns.Contains(c)).ToList();
            if (missing.Count > 0)
            {
                MessageBox.Show($"La tabla no tiene columnas de puntos.\nFaltan: {string.Join(", ", missing)}",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string dbName = Path.GetFileNameWithoutExtension(dbPath ?? "puntos");
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Exportar Puntos (PUNTO, X, Y, Z, DESC)",
                Filter = "CSV (Excel)|*.csv|Texto tabulado|*.txt|Todos|*.*",
                FileName = $"Puntos_{dbName}.csv",
                InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive", "Escritorio")
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var view = dataGrid.ItemsSource as DataView;
                string sep = dlg.FileName.EndsWith(".txt") ? "\t" : ",";

                using (var writer = new StreamWriter(dlg.FileName, false, System.Text.Encoding.UTF8))
                {
                    writer.Write('\uFEFF');
                    writer.WriteLine(string.Join(sep, PointExportHeaders));

                    int count = 0;
                    if (view != null)
                    {
                        foreach (DataRowView drv in view)
                        {
                            var vals = PointExportCols.Select(c => drv[c]?.ToString() ?? "");
                            writer.WriteLine(string.Join(sep, vals));
                            count++;
                        }
                    }

                    SetStatus($"Puntos exportados: {dlg.FileName}");
                    MessageBox.Show($"Se exportaron {count:N0} puntos a:\n{dlg.FileName}\n\nColumnas: PUNTO, ESTE(X), NORTE(Y), ELEVACION(Z), DESCRIPCION",
                        "Exportación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --------------------------------------------------------
        // EXPORTAR CSV COMPLETO
        // --------------------------------------------------------
        private void ExportFullCsv()
        {
            if (fullData == null || fullData.Rows.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Exportar tabla completa como CSV",
                Filter = "CSV|*.csv|Todos|*.*",
                FileName = $"{currentTable}_completo.csv",
                InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive", "Escritorio")
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                using (var writer = new StreamWriter(dlg.FileName, false, System.Text.Encoding.UTF8))
                {
                    writer.Write('\uFEFF');
                    var colNames = fullData.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
                    writer.WriteLine(string.Join(",", colNames));
                    foreach (DataRow row in fullData.Rows)
                    {
                        var vals = row.ItemArray.Select(v => $"\"{v?.ToString()?.Replace("\"", "\"\"")}\"");
                        writer.WriteLine(string.Join(",", vals));
                    }
                }
                SetStatus($"Exportado: {dlg.FileName}");
                MessageBox.Show($"Se exportaron {fullData.Rows.Count:N0} registros a:\n{dlg.FileName}",
                    "Exportación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --------------------------------------------------------
        // STATUS
        // --------------------------------------------------------
        private void SetStatus(string text) { lblStatus.Text = "  " + text; }

        // --------------------------------------------------------
        // CERRAR
        // --------------------------------------------------------
        protected override void OnClosed(EventArgs e)
        {
            if (conn != null) { conn.Close(); conn.Dispose(); }
            base.OnClosed(e);
        }
    }
}
