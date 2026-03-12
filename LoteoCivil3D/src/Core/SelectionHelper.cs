using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.Civil.DatabaseServices;

namespace LoteoCivil3D.Core
{
    /// <summary>
    /// Utilidades de seleccion de objetos Civil 3D
    /// </summary>
    public static class SelectionHelper
    {
        /// <summary>
        /// Selecciona Feature Lines por fence (linea de cruce)
        /// </summary>
        public static List<ObjectId> SelectFeatureLinesByFence(Editor ed, string prompt)
        {
            var result = new List<ObjectId>();

            ed.WriteMessage($"\n{prompt}");
            var ppo = new PromptPointOptions("\nPrimer punto del fence: ");
            ppo.AllowNone = true;
            var pt1Res = ed.GetPoint(ppo);
            if (pt1Res.Status != PromptStatus.OK) return result;

            var ppo2 = new PromptPointOptions("\nSegundo punto del fence: ");
            ppo2.BasePoint = pt1Res.Value;
            ppo2.UseBasePoint = true;
            var pt2Res = ed.GetPoint(ppo2);
            if (pt2Res.Status != PromptStatus.OK) return result;

            var pts = new Autodesk.AutoCAD.Geometry.Point3dCollection();
            pts.Add(pt1Res.Value);
            pts.Add(pt2Res.Value);

            var selRes = ed.SelectFence(pts);
            if (selRes.Status != PromptStatus.OK) return result;

            var doc = Application.DocumentManager.MdiActiveDocument;
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                foreach (var objId in selRes.Value.GetObjectIds())
                {
                    var ent = tr.GetObject(objId, OpenMode.ForRead);
                    if (ent is FeatureLine)
                        result.Add(objId);
                }
                tr.Commit();
            }

            return result;
        }

        /// <summary>
        /// Selecciona Feature Lines con seleccion estandar
        /// </summary>
        public static List<ObjectId> SelectFeatureLines(Editor ed, string prompt)
        {
            var result = new List<ObjectId>();
            var pso = new PromptSelectionOptions();
            pso.MessageForAdding = $"\n{prompt}";
            pso.AllowDuplicates = false;

            var selRes = ed.GetSelection(pso);
            if (selRes.Status != PromptStatus.OK) return result;

            var doc = Application.DocumentManager.MdiActiveDocument;
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                foreach (var objId in selRes.Value.GetObjectIds())
                {
                    var ent = tr.GetObject(objId, OpenMode.ForRead);
                    if (ent is FeatureLine)
                        result.Add(objId);
                }
                tr.Commit();
            }

            return result;
        }

        /// <summary>
        /// Selecciona Feature Lines filtrando por estilo
        /// </summary>
        public static List<ObjectId> SelectFeatureLinesByStyle(Editor ed, string styleName, string prompt)
        {
            var result = new List<ObjectId>();
            var pso = new PromptSelectionOptions();
            pso.MessageForAdding = $"\n{prompt}";

            var selRes = ed.GetSelection(pso);
            if (selRes.Status != PromptStatus.OK) return result;

            var doc = Application.DocumentManager.MdiActiveDocument;
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                foreach (var objId in selRes.Value.GetObjectIds())
                {
                    var fl = tr.GetObject(objId, OpenMode.ForRead) as FeatureLine;
                    if (fl != null)
                    {
                        if (string.IsNullOrEmpty(styleName))
                        {
                            result.Add(objId);
                        }
                        else
                        {
                            var style = tr.GetObject(fl.StyleId, OpenMode.ForRead)
                                as Autodesk.Civil.DatabaseServices.Styles.FeatureLineStyle;
                            if (style != null && style.Name.Equals(styleName, StringComparison.OrdinalIgnoreCase))
                                result.Add(objId);
                        }
                    }
                }
                tr.Commit();
            }

            return result;
        }

        /// <summary>
        /// Obtiene todos los Feature Lines en el dibujo actual
        /// </summary>
        public static List<ObjectId> GetAllFeatureLines(Document doc)
        {
            var result = new List<ObjectId>();
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId objId in ms)
                {
                    var ent = tr.GetObject(objId, OpenMode.ForRead);
                    if (ent is FeatureLine)
                        result.Add(objId);
                }
                tr.Commit();
            }
            return result;
        }

        /// <summary>
        /// Obtiene los nombres de estilos de Feature Line disponibles
        /// </summary>
        public static List<string> GetFeatureLineStyles(Document doc)
        {
            var styles = new List<string>();
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var civilDoc = Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument;
                var flStyles = civilDoc.Styles.FeatureLineStyles;

                for (int i = 0; i < flStyles.Count; i++)
                {
                    var style = tr.GetObject(flStyles[i], OpenMode.ForRead)
                        as Autodesk.Civil.DatabaseServices.Styles.FeatureLineStyle;
                    if (style != null) styles.Add(style.Name);
                }
                tr.Commit();
            }
            return styles;
        }
    }
}
