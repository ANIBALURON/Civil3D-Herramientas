using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.DatabaseServices;

namespace LoteoCivil3D.Core
{
    public static class FeatureLineHelper
    {
        /// <summary>
        /// Obtiene todos los puntos (vertices) de un FeatureLine con sus elevaciones
        /// </summary>
        public static List<Point3d> GetVertices(FeatureLine fl)
        {
            var pts = new List<Point3d>();
            var ptCol = fl.GetPoints(Autodesk.Civil.FeatureLinePointType.AllPoints);
            foreach (Point3d pt in ptCol)
                pts.Add(pt);
            return pts;
        }

        /// <summary>
        /// Establece la elevacion de un vertice por indice
        /// </summary>
        public static void SetElevation(FeatureLine fl, int index, double elevation)
        {
            var pts = fl.GetPoints(Autodesk.Civil.FeatureLinePointType.AllPoints);
            if (index >= 0 && index < pts.Count)
                fl.SetPointElevation(index, elevation);
        }

        /// <summary>
        /// Establece la elevacion de todos los vertices de un FeatureLine
        /// </summary>
        public static void SetAllElevations(FeatureLine fl, double elevation)
        {
            int count = fl.PointsCount;
            for (int i = 0; i < count; i++)
                fl.SetPointElevation(i, elevation);
        }

        /// <summary>
        /// Establece elevacion con pendiente desde un punto de referencia
        /// </summary>
        public static void SetElevationsWithSlope(FeatureLine fl, Point3d refPoint,
            double refElevation, double slopePercent, bool slopeAway)
        {
            var pts = GetVertices(fl);
            for (int i = 0; i < pts.Count; i++)
            {
                var pt = pts[i];
                double dist = new Point2d(pt.X, pt.Y).GetDistanceTo(new Point2d(refPoint.X, refPoint.Y));
                double elevChange = dist * slopePercent / 100.0;
                double newElev = slopeAway ? refElevation - elevChange : refElevation + elevChange;
                fl.SetPointElevation(i, newElev);
            }
        }

        /// <summary>
        /// Interpola elevaciones linealmente entre el inicio y el fin
        /// </summary>
        public static void InterpolateElevations(FeatureLine fl, double startElev, double endElev)
        {
            double totalLen = fl.Length2D;
            if (totalLen < 0.001) return;

            var pts = GetVertices(fl);
            fl.SetPointElevation(0, startElev);

            double accumulated = 0;
            for (int i = 1; i < pts.Count; i++)
            {
                var prev = pts[i - 1];
                var curr = pts[i];
                accumulated += new Point2d(prev.X, prev.Y).GetDistanceTo(new Point2d(curr.X, curr.Y));
                double ratio = accumulated / totalLen;
                double elev = startElev + (endElev - startElev) * ratio;
                fl.SetPointElevation(i, elev);
            }
        }

        /// <summary>
        /// Calcula el centroide 2D de un feature line cerrado (lote o pad)
        /// </summary>
        public static Point3d GetCentroid(FeatureLine fl)
        {
            var pts = GetVertices(fl);
            if (pts.Count == 0) return Point3d.Origin;

            double cx = pts.Average(p => p.X);
            double cy = pts.Average(p => p.Y);
            double cz = pts.Average(p => p.Z);
            return new Point3d(cx, cy, cz);
        }

        /// <summary>
        /// Obtiene el punto mas alto del feature line
        /// </summary>
        public static Point3d GetHighPoint(FeatureLine fl)
        {
            var pts = GetVertices(fl);
            return pts.OrderByDescending(p => p.Z).First();
        }

        /// <summary>
        /// Obtiene el punto mas bajo del feature line
        /// </summary>
        public static Point3d GetLowPoint(FeatureLine fl)
        {
            var pts = GetVertices(fl);
            return pts.OrderBy(p => p.Z).First();
        }

        /// <summary>
        /// Determina si un feature line es cerrado (poligono)
        /// </summary>
        public static bool IsClosed(FeatureLine fl)
        {
            var pts = GetVertices(fl);
            if (pts.Count < 3) return false;
            var first = pts[0];
            var last = pts[pts.Count - 1];
            return new Point2d(first.X, first.Y).GetDistanceTo(new Point2d(last.X, last.Y)) < 0.01;
        }

        /// <summary>
        /// Calcula la pendiente entre dos puntos en porcentaje
        /// </summary>
        public static double CalcSlopePercent(Point3d p1, Point3d p2)
        {
            double hDist = new Point2d(p1.X, p1.Y).GetDistanceTo(new Point2d(p2.X, p2.Y));
            if (hDist < 0.001) return 0;
            return Math.Abs(p2.Z - p1.Z) / hDist * 100.0;
        }

        /// <summary>
        /// Calcula la pendiente promedio del feature line
        /// </summary>
        public static double GetAverageSlope(FeatureLine fl)
        {
            var pts = GetVertices(fl);
            if (pts.Count < 2) return 0;

            double totalSlope = 0;
            int count = 0;
            for (int i = 1; i < pts.Count; i++)
            {
                double s = CalcSlopePercent(pts[i - 1], pts[i]);
                if (s > 0) { totalSlope += s; count++; }
            }
            return count > 0 ? totalSlope / count : 0;
        }

        /// <summary>
        /// Obtiene la elevacion interpolada en el punto mas cercano de un FeatureLine
        /// a una posicion XY dada. Proyecta perpendicularmente al segmento mas cercano.
        /// Ideal para obtener elevacion de calle (borde sardinel) en la posicion de un vertice de lote.
        /// </summary>
        public static double GetElevationAtClosestPoint(FeatureLine fl, double x, double y)
        {
            var verts = GetVertices(fl);
            if (verts.Count == 0) return 0;
            if (verts.Count == 1) return verts[0].Z;

            var queryPt = new Point2d(x, y);
            double bestDist = double.MaxValue;
            double bestElev = verts[0].Z;

            for (int i = 0; i < verts.Count - 1; i++)
            {
                var a2d = new Point2d(verts[i].X, verts[i].Y);
                var b2d = new Point2d(verts[i + 1].X, verts[i + 1].Y);
                var segVec = b2d - a2d;
                double segLen = segVec.Length;
                if (segLen < 0.001) continue;

                // Proyeccion parametrica del punto sobre el segmento
                var toQuery = queryPt - a2d;
                double t = (toQuery.X * segVec.X + toQuery.Y * segVec.Y) / (segLen * segLen);
                t = Math.Max(0, Math.Min(1, t)); // Clampar al segmento

                var closestPt = a2d + segVec * t;
                double dist = queryPt.GetDistanceTo(closestPt);

                if (dist < bestDist)
                {
                    bestDist = dist;
                    // Interpolar elevacion linealmente sobre el segmento
                    bestElev = verts[i].Z + (verts[i + 1].Z - verts[i].Z) * t;
                }
            }

            return bestElev;
        }

        /// <summary>
        /// Crea un offset de un FeatureLine (para reveal)
        /// </summary>
        public static Point3dCollection CreateOffset(FeatureLine fl, double offsetDist)
        {
            var result = new Point3dCollection();
            var verts = GetVertices(fl);
            if (verts.Count < 2) return result;

            for (int i = 0; i < verts.Count; i++)
            {
                Vector3d dir;
                if (i == 0)
                    dir = (verts[1] - verts[0]).GetNormal();
                else if (i == verts.Count - 1)
                    dir = (verts[i] - verts[i - 1]).GetNormal();
                else
                    dir = ((verts[i + 1] - verts[i]).GetNormal() + (verts[i] - verts[i - 1]).GetNormal()).GetNormal();

                // Perpendicular 2D (90 grados)
                var perp = new Vector3d(-dir.Y, dir.X, 0).GetNormal();
                var offsetPt = verts[i] + perp * offsetDist;
                result.Add(offsetPt);
            }
            return result;
        }
    }
}
