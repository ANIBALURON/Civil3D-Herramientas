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
    /// <summary>
    /// Motor principal de grading para lotes con calle frontal
    /// </summary>
    public class LotGrader
    {
        private readonly Document _doc;
        private readonly Editor _ed;
        private readonly GraderSettings _settings;
        private readonly List<string> _log = new List<string>();

        public List<string> Log => _log;
        public List<ObjectId> ProcessedFeatureLines { get; } = new List<ObjectId>();

        public LotGrader(Document doc, GraderSettings settings)
        {
            _doc = doc;
            _ed = doc.Editor;
            _settings = settings;
        }

        /// <summary>
        /// Ejecuta el grading completo en los feature lines seleccionados
        /// </summary>
        /// <summary>
        /// Feature lines de calle (borde sardinel/pavimento) para obtener elevacion del frente
        /// </summary>
        private List<FeatureLine> _streetFeatureLines = new List<FeatureLine>();

        public bool Execute(List<ObjectId> lotLineIds, List<ObjectId> padIds)
        {
            return Execute(lotLineIds, padIds, new List<ObjectId>());
        }

        public bool Execute(List<ObjectId> lotLineIds, List<ObjectId> padIds, List<ObjectId> streetLineIds)
        {
            if (lotLineIds.Count == 0)
            {
                _log.Add("ERROR: No se seleccionaron lineas de lote.");
                return false;
            }

            string modeLabel = _settings.LayoutType == LotLayoutType.BackToBack
                ? "Espalda-Espalda" : "Con Pad";
            string sourceLabel = _settings.StreetSource == StreetElevationSource.FeatureLine
                ? "Feature Line de calle" : "Superficie";
            _log.Add($"Modo: {modeLabel} | Fuente elevacion: {sourceLabel}");
            _log.Add($"Procesando {lotLineIds.Count} linea(s) de lote y {padIds.Count} pad(s)...");

            using (var tr = _doc.Database.TransactionManager.StartTransaction())
            {
                try
                {
                    TinSurface egSurface = null;
                    if (_settings.StreetSource == StreetElevationSource.Surface
                        && !string.IsNullOrEmpty(_settings.SurfaceName))
                        egSurface = GetSurface(tr, _settings.SurfaceName);

                    // Cargar feature lines de calle si se usa esa fuente
                    _streetFeatureLines.Clear();
                    if (_settings.StreetSource == StreetElevationSource.FeatureLine)
                    {
                        foreach (var slId in streetLineIds)
                        {
                            var sfl = tr.GetObject(slId, OpenMode.ForRead) as FeatureLine;
                            if (sfl != null) _streetFeatureLines.Add(sfl);
                        }
                        _log.Add($"  {_streetFeatureLines.Count} feature line(s) de calle cargados.");
                        if (_streetFeatureLines.Count == 0 && streetLineIds.Count > 0)
                            _log.Add("  ADVERTENCIA: No se pudieron leer los feature lines de calle.");
                    }

                    if (_settings.AddToSurface && !string.IsNullOrEmpty(_settings.GradingSurfaceName))
                        SurfaceHelper.CreateSurfaceIfNotExists(_doc, _settings.GradingSurfaceName);

                    int lotCount = 0;
                    foreach (var lotId in lotLineIds)
                    {
                        var lotFL = tr.GetObject(lotId, OpenMode.ForWrite) as FeatureLine;
                        if (lotFL == null) continue;

                        lotCount++;
                        _log.Add($"\n--- Lote #{lotCount} ---");

                        if (_settings.LayoutType == LotLayoutType.BackToBack)
                        {
                            ProcessBackToBackLot(tr, lotFL, egSurface);
                        }
                        else
                        {
                            FeatureLine padFL = FindAssociatedPad(tr, lotFL, padIds);
                            ProcessLot(tr, lotFL, padFL, egSurface);
                            if (padFL != null)
                                ProcessedFeatureLines.Add(padFL.ObjectId);
                        }

                        ProcessedFeatureLines.Add(lotId);
                    }

                    // Agregar breaklines a superficie
                    if (_settings.AddToSurface && ProcessedFeatureLines.Count > 0)
                    {
                        AddBreaklinesToSurface(tr);
                    }

                    tr.Commit();
                    _log.Add($"\nGrading completado: {lotCount} lote(s) procesados.");
                    return true;
                }
                catch (Exception ex)
                {
                    _log.Add($"ERROR: {ex.Message}");
                    tr.Abort();
                    return false;
                }
            }
        }

        private void AddBreaklinesToSurface(Transaction tr)
        {
            var civilDoc = Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument;
            var surfIds = civilDoc.GetSurfaceIds();
            TinSurface fgSurface = null;

            foreach (ObjectId id in surfIds)
            {
                var surf = tr.GetObject(id, OpenMode.ForWrite) as TinSurface;
                if (surf != null && surf.Name.Equals(_settings.GradingSurfaceName, StringComparison.OrdinalIgnoreCase))
                {
                    fgSurface = surf;
                    break;
                }
            }

            if (fgSurface != null)
            {
                var idCol = new ObjectIdCollection();
                foreach (var id in ProcessedFeatureLines)
                    idCol.Add(id);

                fgSurface.BreaklinesDefinition.AddStandardBreaklines(idCol, 1.0, 1.0, 0.0, 0.0);
                _log.Add($"\nBreaklines agregados a superficie '{_settings.GradingSurfaceName}'.");
            }
        }

        /// <summary>
        /// Procesa un lote en modo Espalda-Espalda (sin pad, drenaje a calle frontal)
        /// Los vertices del frente empatan con la calle, los traseros suben con pendiente
        /// formando una divisoria de aguas con el lote vecino de atras
        /// </summary>
        private void ProcessBackToBackLot(Transaction tr, FeatureLine lotFL, TinSurface egSurface)
        {
            var verts = FeatureLineHelper.GetVertices(lotFL);
            if (verts.Count < 3)
            {
                _log.Add("  ADVERTENCIA: Lote con menos de 3 vertices, omitido.");
                return;
            }

            var frontIndices = IdentifyFrontVertices(lotFL, egSurface, verts);
            var rearIndices = new List<int>();
            for (int i = 0; i < verts.Count; i++)
                if (!frontIndices.Contains(i)) rearIndices.Add(i);

            _log.Add($"  Vertices: {verts.Count} | Frente: {frontIndices.Count} | Trasero (divisoria): {rearIndices.Count}");

            // 1. Frente: empatar con elevacion de calle (superficie o feature line)
            double frontAvgElev = 0;
            foreach (int idx in frontIndices)
            {
                double elev;
                if (_settings.MatchStreetElevation)
                {
                    elev = GetStreetElevation(verts[idx].X, verts[idx].Y, egSurface, verts[idx].Z);
                    string src = _settings.StreetSource == StreetElevationSource.FeatureLine ? "FL calle" : "superficie";
                    _log.Add($"  Frente [{idx}]: Elev = {elev:F3} (de {src})");
                }
                else
                {
                    elev = verts[idx].Z;
                    _log.Add($"  Frente [{idx}]: Elev = {elev:F3} (existente)");
                }
                lotFL.SetPointElevation(idx, elev);
                frontAvgElev += elev;
            }
            frontAvgElev /= frontIndices.Count;

            // 2. Trasero (divisoria): calcular elevacion basada en pendiente desde el frente
            // Centro del frente para medir distancias
            double fcx = 0, fcy = 0;
            foreach (int idx in frontIndices) { fcx += verts[idx].X; fcy += verts[idx].Y; }
            fcx /= frontIndices.Count;
            fcy /= frontIndices.Count;

            foreach (int idx in rearIndices)
            {
                double dist = new Point2d(verts[idx].X, verts[idx].Y).GetDistanceTo(new Point2d(fcx, fcy));
                double elevChange = dist * _settings.BackToBackSlope / 100.0;
                double rearElev = frontAvgElev + elevChange + _settings.RidgeExtraElevation;

                // Si hay superficie EG, no bajar por debajo del terreno existente
                if (egSurface != null)
                {
                    double egElev = SurfaceHelper.GetElevationAtPoint(egSurface, verts[idx].X, verts[idx].Y);
                    if (rearElev < egElev)
                        rearElev = egElev + _settings.RidgeExtraElevation;
                }

                lotFL.SetPointElevation(idx, rearElev);
                _log.Add($"  Divisoria [{idx}]: Elev = {rearElev:F3} (dist={dist:F2}m, pend={_settings.BackToBackSlope}%)");
            }

            // 3. Interpolar vertices laterales (si los hay)
            InterpolateSideElevations(lotFL, frontIndices, rearIndices, verts);

            // Resumen
            verts = FeatureLineHelper.GetVertices(lotFL);
            var highPt = verts.OrderByDescending(p => p.Z).First();
            var lowPt = verts.OrderBy(p => p.Z).First();
            _log.Add($"  Elev min: {lowPt.Z:F3} (calle) | Elev max: {highPt.Z:F3} (divisoria)");
            _log.Add($"  Drenaje: divisoria trasera -> calle frontal");
        }

        private void ProcessLot(Transaction tr, FeatureLine lotFL, FeatureLine padFL, TinSurface egSurface)
        {
            var verts = FeatureLineHelper.GetVertices(lotFL);
            if (verts.Count < 3)
            {
                _log.Add("  ADVERTENCIA: Lote con menos de 3 vertices, omitido.");
                return;
            }

            var frontIndices = IdentifyFrontVertices(lotFL, egSurface, verts);
            var rearIndices = new List<int>();
            for (int i = 0; i < verts.Count; i++)
                if (!frontIndices.Contains(i)) rearIndices.Add(i);

            _log.Add($"  Vertices: {verts.Count} | Frente: {frontIndices.Count} | Posterior: {rearIndices.Count}");

            SetFrontElevations(lotFL, frontIndices, verts, egSurface);
            SetRearElevations(lotFL, frontIndices, rearIndices, verts);
            InterpolateSideElevations(lotFL, frontIndices, rearIndices, verts);

            if (padFL != null)
                ProcessPad(tr, lotFL, padFL, egSurface);

            // Refresh vertices after changes
            verts = FeatureLineHelper.GetVertices(lotFL);
            var highPt = verts.OrderByDescending(p => p.Z).First();
            var lowPt = verts.OrderBy(p => p.Z).First();
            _log.Add($"  Elev min: {lowPt.Z:F3} | Elev max: {highPt.Z:F3}");
        }

        private List<int> IdentifyFrontVertices(FeatureLine lotFL, TinSurface egSurface, List<Point3d> verts)
        {
            var indices = new List<int>();

            if (_settings.StreetSource == StreetElevationSource.FeatureLine && _streetFeatureLines.Count > 0)
            {
                // Frente = vertices mas cercanos al feature line de calle
                var distances = new List<(int idx, double dist)>();
                for (int i = 0; i < verts.Count; i++)
                {
                    double minDist = double.MaxValue;
                    foreach (var sfl in _streetFeatureLines)
                    {
                        var sVerts = FeatureLineHelper.GetVertices(sfl);
                        for (int j = 0; j < sVerts.Count - 1; j++)
                        {
                            var a = new Point2d(sVerts[j].X, sVerts[j].Y);
                            var b = new Point2d(sVerts[j + 1].X, sVerts[j + 1].Y);
                            var seg = b - a;
                            double segLen = seg.Length;
                            if (segLen < 0.001) continue;
                            var toVert = new Point2d(verts[i].X, verts[i].Y) - a;
                            double t = (toVert.X * seg.X + toVert.Y * seg.Y) / (segLen * segLen);
                            t = Math.Max(0, Math.Min(1, t));
                            var closest = a + seg * t;
                            double d = new Point2d(verts[i].X, verts[i].Y).GetDistanceTo(closest);
                            if (d < minDist) minDist = d;
                        }
                    }
                    distances.Add((i, minDist));
                }
                distances = distances.OrderBy(e => e.dist).ToList();
                int frontCount = Math.Max(2, verts.Count / 2);
                for (int i = 0; i < frontCount && i < distances.Count; i++)
                    indices.Add(distances[i].idx);
            }
            else if (egSurface != null && _settings.MatchStreetElevation)
            {
                // Frente = vertices con elevacion mas baja en superficie
                var elevations = new List<(int idx, double elev)>();
                for (int i = 0; i < verts.Count; i++)
                {
                    double egElev = SurfaceHelper.GetElevationAtPoint(egSurface, verts[i].X, verts[i].Y);
                    elevations.Add((i, egElev));
                }
                elevations = elevations.OrderBy(e => e.elev).ToList();
                int frontCount = Math.Max(2, verts.Count / 2);
                for (int i = 0; i < frontCount && i < elevations.Count; i++)
                    indices.Add(elevations[i].idx);
            }
            else
            {
                indices.Add(0);
                if (verts.Count > 1) indices.Add(1);
            }

            return indices;
        }

        private void SetFrontElevations(FeatureLine lotFL, List<int> frontIndices,
            List<Point3d> verts, TinSurface egSurface)
        {
            foreach (int idx in frontIndices)
            {
                double elev;
                if (_settings.MatchStreetElevation)
                {
                    elev = GetStreetElevation(verts[idx].X, verts[idx].Y, egSurface, verts[idx].Z);
                    elev += _settings.PadElevationOffset;
                }
                else
                {
                    elev = verts[idx].Z;
                }

                lotFL.SetPointElevation(idx, elev);
                _log.Add($"  Frente [{idx}]: Elev = {elev:F3}");
            }
        }

        private void SetRearElevations(FeatureLine lotFL, List<int> frontIndices,
            List<int> rearIndices, List<Point3d> verts)
        {
            if (rearIndices.Count == 0) return;

            // Elevacion promedio del frente (ya actualizada)
            double frontAvgElev = 0;
            foreach (int idx in frontIndices)
            {
                var p = lotFL.GetPoints(Autodesk.Civil.FeatureLinePointType.AllPoints);
                frontAvgElev += ((Point3d)p[idx]).Z;
            }
            frontAvgElev /= frontIndices.Count;

            // Centro del frente
            double fcx = 0, fcy = 0;
            foreach (int idx in frontIndices) { fcx += verts[idx].X; fcy += verts[idx].Y; }
            fcx /= frontIndices.Count;
            fcy /= frontIndices.Count;

            foreach (int idx in rearIndices)
            {
                double dist = new Point2d(verts[idx].X, verts[idx].Y).GetDistanceTo(new Point2d(fcx, fcy));
                double slopeToUse = _settings.RearSlope;
                double elevChange = _settings.DrainToRear
                    ? -(dist * slopeToUse / 100.0)
                    : dist * slopeToUse / 100.0;

                double newElev = frontAvgElev + elevChange;
                lotFL.SetPointElevation(idx, newElev);
                _log.Add($"  Posterior [{idx}]: Elev = {newElev:F3} (dist={dist:F2}m)");
            }
        }

        private void InterpolateSideElevations(FeatureLine lotFL, List<int> frontIndices,
            List<int> rearIndices, List<Point3d> verts)
        {
            var allFrontRear = new HashSet<int>(frontIndices);
            foreach (int i in rearIndices) allFrontRear.Add(i);

            // Get updated points
            var updatedPts = FeatureLineHelper.GetVertices(lotFL);

            for (int i = 0; i < verts.Count; i++)
            {
                if (allFrontRear.Contains(i)) continue;

                var pt = verts[i];
                double nearestFrontElev = 0;
                double minFrontDist = double.MaxValue;
                foreach (int fi in frontIndices)
                {
                    double d = new Point2d(pt.X, pt.Y).GetDistanceTo(new Point2d(updatedPts[fi].X, updatedPts[fi].Y));
                    if (d < minFrontDist) { minFrontDist = d; nearestFrontElev = updatedPts[fi].Z; }
                }

                double nearestRearElev = 0;
                double minRearDist = double.MaxValue;
                foreach (int ri in rearIndices)
                {
                    double d = new Point2d(pt.X, pt.Y).GetDistanceTo(new Point2d(updatedPts[ri].X, updatedPts[ri].Y));
                    if (d < minRearDist) { minRearDist = d; nearestRearElev = updatedPts[ri].Z; }
                }

                double totalDist = minFrontDist + minRearDist;
                if (totalDist < 0.001) continue;
                double ratio = minFrontDist / totalDist;
                double elev = nearestFrontElev + (nearestRearElev - nearestFrontElev) * ratio;
                lotFL.SetPointElevation(i, elev);
            }
        }

        private void ProcessPad(Transaction tr, FeatureLine lotFL, FeatureLine padFL, TinSurface egSurface)
        {
            _log.Add("  Procesando pad...");
            var padCenter = FeatureLineHelper.GetCentroid(padFL);

            double lotCenterElev = egSurface != null
                ? SurfaceHelper.GetElevationAtPoint(egSurface, padCenter.X, padCenter.Y)
                : FeatureLineHelper.GetCentroid(lotFL).Z;

            double padElev = lotCenterElev + _settings.PadElevationOffset;
            _log.Add($"  Pad elevacion: {padElev:F3}");

            if (_settings.PadSlopeAway)
            {
                FeatureLineHelper.SetElevationsWithSlope(padFL, padCenter, padElev, _settings.PadSlope, true);
                _log.Add($"  Pad con pendiente {_settings.PadSlope}% desde centro");
            }
            else
            {
                FeatureLineHelper.SetAllElevations(padFL, padElev);
                _log.Add($"  Pad plano a elev {padElev:F3}");
            }

            if (_settings.UseReveal)
                CreateReveal(tr, padFL, padElev);
        }

        private void CreateReveal(Transaction tr, FeatureLine padFL, double padElev)
        {
            var offsetPts = FeatureLineHelper.CreateOffset(padFL, _settings.RevealDistance);
            if (offsetPts.Count < 2) return;

            var btr = (BlockTableRecord)tr.GetObject(_doc.Database.CurrentSpaceId, OpenMode.ForWrite);

            using (var pline = new Polyline3d(Poly3dType.SimplePoly, offsetPts, false))
            {
                btr.AppendEntity(pline);
                tr.AddNewlyCreatedDBObject(pline, true);

                double revealElev = padElev - _settings.RevealDrop;
                _log.Add($"  Reveal creado: offset={_settings.RevealDistance}m, drop={_settings.RevealDrop}m, elev={revealElev:F3}");
                ProcessedFeatureLines.Add(pline.ObjectId);
            }
        }

        private FeatureLine FindAssociatedPad(Transaction tr, FeatureLine lotFL, List<ObjectId> padIds)
        {
            if (padIds.Count == 0) return null;

            var lotCenter = FeatureLineHelper.GetCentroid(lotFL);
            FeatureLine closestPad = null;
            double minDist = double.MaxValue;

            foreach (var padId in padIds)
            {
                var padFL = tr.GetObject(padId, OpenMode.ForWrite) as FeatureLine;
                if (padFL == null) continue;

                var padCenter = FeatureLineHelper.GetCentroid(padFL);
                double dist = new Point2d(lotCenter.X, lotCenter.Y).GetDistanceTo(
                    new Point2d(padCenter.X, padCenter.Y));

                if (dist < minDist)
                {
                    minDist = dist;
                    closestPad = padFL;
                }
            }

            if (closestPad != null)
                _log.Add($"  Pad asociado encontrado (dist={minDist:F2}m)");
            return closestPad;
        }

        /// <summary>
        /// Obtiene la elevacion de calle en un punto XY, usando la fuente configurada
        /// (superficie o feature line de calle)
        /// </summary>
        private double GetStreetElevation(double x, double y, TinSurface egSurface, double fallback)
        {
            if (_settings.StreetSource == StreetElevationSource.FeatureLine && _streetFeatureLines.Count > 0)
            {
                // Buscar el feature line de calle mas cercano y obtener su elevacion
                double bestDist = double.MaxValue;
                double bestElev = fallback;

                foreach (var sfl in _streetFeatureLines)
                {
                    double elev = FeatureLineHelper.GetElevationAtClosestPoint(sfl, x, y);
                    // Calcular distancia al FL para saber cual es el mas cercano
                    var verts = FeatureLineHelper.GetVertices(sfl);
                    double minD = double.MaxValue;
                    foreach (var v in verts)
                    {
                        double d = new Point2d(x, y).GetDistanceTo(new Point2d(v.X, v.Y));
                        if (d < minD) minD = d;
                    }
                    if (minD < bestDist)
                    {
                        bestDist = minD;
                        bestElev = elev;
                    }
                }
                return bestElev;
            }
            else if (egSurface != null)
            {
                return SurfaceHelper.GetElevationAtPoint(egSurface, x, y);
            }
            return fallback;
        }

        private TinSurface GetSurface(Transaction tr, string name)
        {
            var civilDoc = Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument;
            var surfIds = civilDoc.GetSurfaceIds();
            foreach (ObjectId id in surfIds)
            {
                var surf = tr.GetObject(id, OpenMode.ForRead) as TinSurface;
                if (surf != null && surf.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return surf;
            }
            return null;
        }
    }
}
