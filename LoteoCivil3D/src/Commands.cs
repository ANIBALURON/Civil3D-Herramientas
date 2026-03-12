using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using LoteoCivil3D.Core;
using LoteoCivil3D.Forms;

[assembly: CommandClass(typeof(LoteoCivil3D.Commands))]
[assembly: ExtensionApplication(typeof(LoteoCivil3D.LoteoApp))]

namespace LoteoCivil3D
{
    /// <summary>
    /// Clase de inicializacion del plugin
    /// </summary>
    public class LoteoApp : IExtensionApplication
    {
        public void Initialize()
        {
            SettingsManager.LoadAll();
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc != null)
            {
                doc.Editor.WriteMessage("\n=============================================");
                doc.Editor.WriteMessage("\n  LOTEO CIVIL3D v1.0 cargado exitosamente");
                doc.Editor.WriteMessage("\n  Comandos: LOTEO_SETTINGS, LOTEO_GRADER,");
                doc.Editor.WriteMessage("\n            LOTEO_LABELER, LOTEO_UPDATE,");
                doc.Editor.WriteMessage("\n            LOTEO_ABOUT");
                doc.Editor.WriteMessage("\n=============================================\n");
            }
        }

        public void Terminate()
        {
        }
    }

    /// <summary>
    /// Comandos de AutoCAD Civil 3D
    /// </summary>
    public class Commands
    {
        /// <summary>
        /// Abre la configuracion de grading
        /// </summary>
        [CommandMethod("LOTEO_SETTINGS")]
        public void LoteoSettings()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            try
            {
                var surfNames = SurfaceHelper.GetSurfaceNames(doc);
                var flStyles = SelectionHelper.GetFeatureLineStyles(doc);

                var frm = new FrmGraderSettings(SettingsManager.CurrentGrader, surfNames, flStyles);
                var result = Application.ShowModalDialog(frm);

                if (result == System.Windows.Forms.DialogResult.OK)
                    ed.WriteMessage("\nConfiguracion guardada.");
                else
                    ed.WriteMessage("\nConfiguracion cancelada.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// Ejecuta el grading de lotes
        /// </summary>
        [CommandMethod("LOTEO_GRADER")]
        public void LoteoGrader()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            try
            {
                ed.WriteMessage("\n=== LOTEO GRADER ===");

                // Seleccionar lot lines
                ed.WriteMessage("\nSeleccione las LINEAS DE LOTE (Feature Lines de contorno):");
                var lotIds = SelectionHelper.SelectFeatureLines(ed,
                    "Seleccione lineas de lote [Enter para terminar]: ");

                if (lotIds.Count == 0)
                {
                    // Intentar con fence
                    ed.WriteMessage("\nO use seleccion por FENCE:");
                    lotIds = SelectionHelper.SelectFeatureLinesByFence(ed,
                        "Seleccione lineas de lote con fence:");
                }

                if (lotIds.Count == 0)
                {
                    ed.WriteMessage("\nNo se seleccionaron lineas de lote. Comando cancelado.");
                    return;
                }

                ed.WriteMessage($"\n{lotIds.Count} linea(s) de lote seleccionadas.");

                var padIds = new List<Autodesk.AutoCAD.DatabaseServices.ObjectId>();
                var streetLineIds = new List<Autodesk.AutoCAD.DatabaseServices.ObjectId>();

                if (SettingsManager.CurrentGrader.LayoutType == LotLayoutType.BackToBack)
                {
                    ed.WriteMessage("\nModo: Espalda-Espalda (sin pad).");
                }
                else
                {
                    // Seleccionar pads (opcional)
                    ed.WriteMessage("\nSeleccione los PADS (Feature Lines de plataforma) [Enter para omitir]:");
                    padIds = SelectionHelper.SelectFeatureLines(ed,
                        "Seleccione pads [Enter para omitir]: ");
                    ed.WriteMessage($"\n{padIds.Count} pad(s) seleccionados.");
                }

                // Seleccionar feature lines de calle si esa es la fuente configurada
                if (SettingsManager.CurrentGrader.StreetSource == StreetElevationSource.FeatureLine)
                {
                    ed.WriteMessage("\nSeleccione el/los FEATURE LINE(S) DE CALLE (borde sardinel/pavimento):");
                    ed.WriteMessage("\n  (Estos FL tienen las elevaciones de la rasante de la calle)");
                    streetLineIds = SelectionHelper.SelectFeatureLines(ed,
                        "Seleccione FL de calle [Enter para terminar]: ");

                    if (streetLineIds.Count == 0)
                    {
                        ed.WriteMessage("\nADVERTENCIA: No se selecciono FL de calle. Se usaran elevaciones existentes.");
                    }
                    else
                    {
                        ed.WriteMessage($"\n{streetLineIds.Count} FL de calle seleccionado(s).");
                    }
                }

                // Confirmar
                string confirmMsg = SettingsManager.CurrentGrader.StreetSource == StreetElevationSource.FeatureLine
                    ? $"\nProcesar {lotIds.Count} lote(s) con {streetLineIds.Count} FL de calle? [Si/No]"
                    : $"\nProcesar {lotIds.Count} lote(s) y {padIds.Count} pad(s)? [Si/No]";
                var confirm = ed.GetKeywords(
                    new PromptKeywordOptions(confirmMsg, "Si No") { AllowNone = true });

                if (confirm.Status != PromptStatus.OK || confirm.StringResult == "No")
                {
                    ed.WriteMessage("\nComando cancelado.");
                    return;
                }

                // Ejecutar grading
                var grader = new LotGrader(doc, SettingsManager.CurrentGrader);
                bool success = grader.Execute(lotIds, padIds, streetLineIds);

                // Mostrar log
                var frm = new FrmConsole("Resultado del Grading", grader.Log);
                Application.ShowModelessDialog(frm);

                if (success)
                    ed.WriteMessage($"\nGrading completado exitosamente.");
                else
                    ed.WriteMessage($"\nGrading completado con errores. Revise el log.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// Ejecuta el etiquetado de lotes
        /// </summary>
        [CommandMethod("LOTEO_LABELER")]
        public void LoteoLabeler()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            try
            {
                // Mostrar dialogo de configuracion del labeler
                var frm = new FrmLabeler(SettingsManager.CurrentLabeler);
                var result = Application.ShowModalDialog(frm);

                if (result != System.Windows.Forms.DialogResult.OK)
                {
                    ed.WriteMessage("\nEtiquetado cancelado.");
                    return;
                }

                // Seleccionar feature lines a etiquetar
                ed.WriteMessage("\nSeleccione los Feature Lines a etiquetar:");
                var flIds = SelectionHelper.SelectFeatureLines(ed,
                    "Seleccione feature lines [Enter para terminar]: ");

                if (flIds.Count == 0)
                {
                    ed.WriteMessage("\nNo se seleccionaron feature lines. Comando cancelado.");
                    return;
                }

                // Ejecutar
                var labeler = new LotLabeler(doc, SettingsManager.CurrentLabeler);
                bool success = labeler.Execute(flIds);

                // Mostrar log
                var logFrm = new FrmConsole("Resultado del Etiquetado", labeler.Log);
                Application.ShowModelessDialog(logFrm);

                if (success)
                    ed.WriteMessage($"\nEtiquetado completado.");
                else
                    ed.WriteMessage($"\nEtiquetado completado con errores.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza/refresca la superficie de grading
        /// </summary>
        [CommandMethod("LOTEO_UPDATE")]
        public void LoteoUpdate()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            try
            {
                string surfName = SettingsManager.CurrentGrader.GradingSurfaceName;

                var pso = new PromptStringOptions($"\nNombre de superficie a actualizar [{surfName}]: ");
                pso.AllowSpaces = true;
                pso.DefaultValue = surfName;
                pso.UseDefaultValue = true;

                var result = ed.GetString(pso);
                if (result.Status != PromptStatus.OK) return;

                string name = string.IsNullOrEmpty(result.StringResult) ? surfName : result.StringResult;

                SurfaceHelper.RefreshSurface(doc, name, SettingsManager.CurrentGrader.BreaklineGroupName);
                ed.WriteMessage($"\nSuperficie '{name}' actualizada.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// Muestra informacion del plugin
        /// </summary>
        [CommandMethod("LOTEO_ABOUT")]
        public void LoteoAbout()
        {
            var frm = new FrmAbout();
            Application.ShowModalDialog(frm);
        }

        /// <summary>
        /// Ayuda rapida en linea de comandos
        /// </summary>
        [CommandMethod("LOTEO_HELP")]
        public void LoteoHelp()
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage("\n=============================================");
            ed.WriteMessage("\n  LOTEO CIVIL3D - Comandos disponibles:");
            ed.WriteMessage("\n---------------------------------------------");
            ed.WriteMessage("\n  LOTEO_SETTINGS  - Configuracion de grading");
            ed.WriteMessage("\n  LOTEO_GRADER    - Ejecutar grading de lotes");
            ed.WriteMessage("\n  LOTEO_LABELER   - Etiquetar elevaciones");
            ed.WriteMessage("\n  LOTEO_UPDATE    - Actualizar superficie");
            ed.WriteMessage("\n  LOTEO_ABOUT     - Acerca de");
            ed.WriteMessage("\n  LOTEO_HELP      - Esta ayuda");
            ed.WriteMessage("\n=============================================\n");
        }
    }
}
