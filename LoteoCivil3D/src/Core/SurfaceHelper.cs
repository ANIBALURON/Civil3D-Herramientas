using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;

namespace LoteoCivil3D.Core
{
    public static class SurfaceHelper
    {
        /// <summary>
        /// Obtiene la elevacion de una superficie existente en un punto XY
        /// </summary>
        public static double GetElevationAtPoint(TinSurface surface, double x, double y)
        {
            try
            {
                return surface.FindElevationAtXY(x, y);
            }
            catch
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Busca una superficie por nombre en el documento actual
        /// </summary>
        public static TinSurface FindSurfaceByName(Document doc, string surfaceName)
        {
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var civilDoc = Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument;
                var surfIds = civilDoc.GetSurfaceIds();

                foreach (ObjectId id in surfIds)
                {
                    var surf = tr.GetObject(id, OpenMode.ForRead) as TinSurface;
                    if (surf != null && surf.Name.Equals(surfaceName, StringComparison.OrdinalIgnoreCase))
                    {
                        tr.Commit();
                        return surf;
                    }
                }
                tr.Commit();
                return null;
            }
        }

        /// <summary>
        /// Obtiene lista de nombres de superficies en el documento
        /// </summary>
        public static List<string> GetSurfaceNames(Document doc)
        {
            var names = new List<string>();
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var civilDoc = Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument;
                var surfIds = civilDoc.GetSurfaceIds();

                foreach (ObjectId id in surfIds)
                {
                    var surf = tr.GetObject(id, OpenMode.ForRead) as Autodesk.Civil.DatabaseServices.Surface;
                    if (surf != null) names.Add(surf.Name);
                }
                tr.Commit();
            }
            return names;
        }

        /// <summary>
        /// Agrega Feature Lines como breaklines a una superficie TIN
        /// </summary>
        public static void AddBreaklines(Document doc, string surfaceName,
            List<ObjectId> featureLineIds, string groupName)
        {
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var civilDoc = Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument;
                var surfIds = civilDoc.GetSurfaceIds();

                TinSurface targetSurf = null;
                foreach (ObjectId id in surfIds)
                {
                    var surf = tr.GetObject(id, OpenMode.ForWrite) as TinSurface;
                    if (surf != null && surf.Name.Equals(surfaceName, StringComparison.OrdinalIgnoreCase))
                    {
                        targetSurf = surf;
                        break;
                    }
                }

                if (targetSurf == null)
                {
                    doc.Editor.WriteMessage($"\nSuperficie '{surfaceName}' no encontrada.");
                    tr.Commit();
                    return;
                }

                var idCol = new ObjectIdCollection();
                foreach (var id in featureLineIds)
                    idCol.Add(id);

                targetSurf.BreaklinesDefinition.AddStandardBreaklines(
                    idCol, 1.0, 1.0, 0.0, 0.0);

                doc.Editor.WriteMessage($"\n{featureLineIds.Count} breakline(s) agregados a '{surfaceName}'.");
                tr.Commit();
            }
        }

        /// <summary>
        /// Crea una nueva superficie TIN si no existe
        /// </summary>
        public static ObjectId CreateSurfaceIfNotExists(Document doc, string surfaceName, string styleName = "")
        {
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var civilDoc = Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument;
                var surfIds = civilDoc.GetSurfaceIds();

                foreach (ObjectId id in surfIds)
                {
                    var surf = tr.GetObject(id, OpenMode.ForRead) as Autodesk.Civil.DatabaseServices.Surface;
                    if (surf != null && surf.Name.Equals(surfaceName, StringComparison.OrdinalIgnoreCase))
                    {
                        tr.Commit();
                        return id;
                    }
                }

                // Crear nueva superficie
                ObjectId styleId = ObjectId.Null;
                var styles = civilDoc.Styles.SurfaceStyles;
                if (!string.IsNullOrEmpty(styleName))
                {
                    // Intentar encontrar el estilo
                    for (int i = 0; i < styles.Count; i++)
                    {
                        var style = tr.GetObject(styles[i], OpenMode.ForRead) as Autodesk.Civil.DatabaseServices.Styles.SurfaceStyle;
                        if (style != null && style.Name == styleName)
                        {
                            styleId = styles[i];
                            break;
                        }
                    }
                }

                if (styleId == ObjectId.Null && styles.Count > 0)
                    styleId = styles[0];

                var newId = TinSurface.Create(surfaceName, styleId);
                doc.Editor.WriteMessage($"\nSuperficie '{surfaceName}' creada.");
                tr.Commit();
                return newId;
            }
        }

        /// <summary>
        /// Remueve y reagrega breaklines de lotes para refrescar la superficie
        /// </summary>
        public static void RefreshSurface(Document doc, string surfaceName, string breaklineGroupName)
        {
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var civilDoc = Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument;
                var surfIds = civilDoc.GetSurfaceIds();

                TinSurface targetSurf = null;
                foreach (ObjectId id in surfIds)
                {
                    var surf = tr.GetObject(id, OpenMode.ForWrite) as TinSurface;
                    if (surf != null && surf.Name.Equals(surfaceName, StringComparison.OrdinalIgnoreCase))
                    {
                        targetSurf = surf;
                        break;
                    }
                }

                if (targetSurf == null)
                {
                    doc.Editor.WriteMessage($"\nSuperficie '{surfaceName}' no encontrada.");
                    tr.Commit();
                    return;
                }

                // Rebuild de la superficie
                targetSurf.Rebuild();
                doc.Editor.WriteMessage($"\nSuperficie '{surfaceName}' actualizada.");
                tr.Commit();
            }
        }

        /// <summary>
        /// Obtiene la elevacion de la calle (superficie existente) en puntos del lote frontal
        /// </summary>
        public static double GetStreetElevation(TinSurface egSurface, Point3d lotFrontPoint)
        {
            try
            {
                return egSurface.FindElevationAtXY(lotFrontPoint.X, lotFrontPoint.Y);
            }
            catch
            {
                return lotFrontPoint.Z;
            }
        }
    }
}
