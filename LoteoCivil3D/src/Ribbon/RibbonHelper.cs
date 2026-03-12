using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;

namespace LoteoCivil3D.Ribbon
{
    /// <summary>
    /// Crea la pestana de Ribbon para Loteo Civil3D
    /// </summary>
    public static class RibbonHelper
    {
        private static RibbonTab _tab;

        public static void CreateRibbon()
        {
            try
            {
                var ribbonCtrl = ComponentManager.Ribbon;
                if (ribbonCtrl == null) return;

                // Verificar si ya existe
                foreach (var tab in ribbonCtrl.Tabs)
                {
                    if (tab.Id == "LOTEO_TAB")
                    {
                        tab.IsActive = true;
                        return;
                    }
                }

                _tab = new RibbonTab();
                _tab.Title = "Loteo Civil3D";
                _tab.Id = "LOTEO_TAB";

                // Panel: Grading
                var pnlGrading = new RibbonPanelSource();
                pnlGrading.Title = "Grading";

                var btnSettings = CreateButton("Configuracion", "Configurar parametros de grading",
                    "LOTEO_SETTINGS", RibbonItemSize.Large, "SETTINGS");
                var btnGrader = CreateButton("Lot Grader", "Ejecutar grading de lotes",
                    "LOTEO_GRADER", RibbonItemSize.Large, "GRADER");

                pnlGrading.Items.Add(btnSettings);
                pnlGrading.Items.Add(btnGrader);

                var rpGrading = new RibbonPanel();
                rpGrading.Source = pnlGrading;
                _tab.Panels.Add(rpGrading);

                // Panel: Etiquetado
                var pnlLabel = new RibbonPanelSource();
                pnlLabel.Title = "Etiquetado";

                var btnLabeler = CreateButton("Etiquetar", "Etiquetar elevaciones y pendientes",
                    "LOTEO_LABELER", RibbonItemSize.Large, "LABEL");

                pnlLabel.Items.Add(btnLabeler);

                var rpLabel = new RibbonPanel();
                rpLabel.Source = pnlLabel;
                _tab.Panels.Add(rpLabel);

                // Panel: Superficie
                var pnlSurface = new RibbonPanelSource();
                pnlSurface.Title = "Superficie";

                var btnUpdate = CreateButton("Actualizar", "Actualizar superficie de grading",
                    "LOTEO_UPDATE", RibbonItemSize.Large, "UPDATE");

                pnlSurface.Items.Add(btnUpdate);

                var rpSurface = new RibbonPanel();
                rpSurface.Source = pnlSurface;
                _tab.Panels.Add(rpSurface);

                // Panel: Info
                var pnlInfo = new RibbonPanelSource();
                pnlInfo.Title = "Informacion";

                var btnAbout = CreateButton("Acerca de", "Informacion del plugin",
                    "LOTEO_ABOUT", RibbonItemSize.Standard, "ABOUT");
                var btnHelp = CreateButton("Ayuda", "Mostrar comandos disponibles",
                    "LOTEO_HELP", RibbonItemSize.Standard, "HELP");

                pnlInfo.Items.Add(btnAbout);
                pnlInfo.Items.Add(btnHelp);

                var rpInfo = new RibbonPanel();
                rpInfo.Source = pnlInfo;
                _tab.Panels.Add(rpInfo);

                ribbonCtrl.Tabs.Add(_tab);
                _tab.IsActive = true;
            }
            catch (System.Exception ex)
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                doc?.Editor.WriteMessage($"\nError creando Ribbon: {ex.Message}");
            }
        }

        public static void RemoveRibbon()
        {
            if (_tab != null)
            {
                var ribbonCtrl = ComponentManager.Ribbon;
                ribbonCtrl?.Tabs.Remove(_tab);
                _tab = null;
            }
        }

        private static RibbonButton CreateButton(string text, string tooltip,
            string command, RibbonItemSize size, string iconKey)
        {
            var btn = new RibbonButton();
            btn.Text = text;
            btn.ShowText = true;
            btn.ShowImage = true;
            btn.Size = size;
            btn.CommandParameter = command;
            btn.CommandHandler = new RibbonCommandHandler();
            btn.ToolTip = new RibbonToolTip
            {
                Title = text,
                Content = tooltip,
                Command = command
            };

            // Generar iconos con texto (sin archivos externos)
            btn.LargeImage = CreateTextIcon(iconKey, 32);
            btn.Image = CreateTextIcon(iconKey, 16);

            return btn;
        }

        /// <summary>
        /// Crea un icono simple con texto (sin necesidad de archivos .ico)
        /// </summary>
        private static System.Windows.Media.ImageSource CreateTextIcon(string text, int size)
        {
            var bmp = new System.Drawing.Bitmap(size, size);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.Clear(System.Drawing.Color.FromArgb(0, 100, 180));
                var font = new System.Drawing.Font("Segoe UI", size * 0.35f, System.Drawing.FontStyle.Bold);
                var sf = new System.Drawing.StringFormat
                {
                    Alignment = System.Drawing.StringAlignment.Center,
                    LineAlignment = System.Drawing.StringAlignment.Center
                };

                string displayText = text.Length > 2 ? text.Substring(0, 2) : text;
                g.DrawString(displayText, font, System.Drawing.Brushes.White,
                    new System.Drawing.RectangleF(0, 0, size, size), sf);
            }

            var handle = bmp.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    handle, IntPtr.Zero, System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(handle);
                bmp.Dispose();
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
    }

    /// <summary>
    /// Handler para ejecutar comandos desde el Ribbon
    /// </summary>
    public class RibbonCommandHandler : System.Windows.Input.ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            if (parameter is RibbonButton btn)
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                    doc.SendStringToExecute($"{btn.CommandParameter} ", true, false, true);
            }
        }
    }
}
