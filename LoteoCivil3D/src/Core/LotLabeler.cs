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
    public class LotLabeler
    {
        private readonly Document _doc;
        private readonly Editor _ed;
        private readonly LabelerSettings _settings;
        private readonly List<string> _log = new List<string>();

        public List<string> Log => _log;

        public LotLabeler(Document doc, LabelerSettings settings)
        {
            _doc = doc;
            _ed = doc.Editor;
            _settings = settings;
        }

        public bool Execute(List<ObjectId> featureLineIds)
        {
            if (featureLineIds.Count == 0)
            {
                _log.Add("ERROR: No se seleccionaron feature lines.");
                return false;
            }

            using (var tr = _doc.Database.TransactionManager.StartTransaction())
            {
                try
                {
                    var btr = (BlockTableRecord)tr.GetObject(
                        _doc.Database.CurrentSpaceId, OpenMode.ForWrite);

                    int labelCount = 0;

                    foreach (var flId in featureLineIds)
                    {
                        var fl = tr.GetObject(flId, OpenMode.ForRead) as FeatureLine;
                        if (fl == null) continue;

                        var verts = FeatureLineHelper.GetVertices(fl);

                        if (_settings.LabelLotElevations)
                        {
                            for (int i = 0; i < verts.Count; i++)
                            {
                                string text = FormatElevation(verts[i].Z);
                                CreateTextLabel(tr, btr, verts[i], text, 0);
                                labelCount++;
                            }
                        }

                        if (_settings.LabelHighPoints && verts.Count > 0)
                        {
                            var highPt = verts.OrderByDescending(p => p.Z).First();
                            string text = "HP " + FormatElevation(highPt.Z);
                            CreateTextLabel(tr, btr, highPt, text, 0, true);
                            labelCount++;
                        }

                        if (_settings.LabelSlopes)
                        {
                            for (int i = 0; i < verts.Count - 1; i++)
                            {
                                double slope = FeatureLineHelper.CalcSlopePercent(verts[i], verts[i + 1]);
                                if (slope > 0.01)
                                {
                                    var midPt = new Point3d(
                                        (verts[i].X + verts[i + 1].X) / 2,
                                        (verts[i].Y + verts[i + 1].Y) / 2,
                                        (verts[i].Z + verts[i + 1].Z) / 2);

                                    double angle = Math.Atan2(
                                        verts[i + 1].Y - verts[i].Y,
                                        verts[i + 1].X - verts[i].X);

                                    string dir = verts[i + 1].Z > verts[i].Z ? "^" : "v";
                                    string text = $"{slope:F1}% {dir}";
                                    CreateTextLabel(tr, btr, midPt, text, angle);
                                    labelCount++;
                                }
                            }
                        }

                        if (_settings.LabelPadElevations && FeatureLineHelper.IsClosed(fl))
                        {
                            var center = FeatureLineHelper.GetCentroid(fl);
                            string text = "PAD " + FormatElevation(center.Z);
                            CreateTextLabel(tr, btr, center, text, 0, true);
                            labelCount++;
                        }
                    }

                    tr.Commit();
                    _log.Add($"Etiquetado completado: {labelCount} etiquetas creadas.");
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

        private string FormatElevation(double elev)
        {
            return elev.ToString("F" + _settings.DecimalPlaces);
        }

        private void CreateTextLabel(Transaction tr, BlockTableRecord btr,
            Point3d position, string text, double rotationRad, bool isSpecial = false)
        {
            var mtext = new MText();
            mtext.Location = position;
            mtext.Contents = text;
            mtext.TextHeight = _settings.TextHeight;
            mtext.Rotation = rotationRad;
            mtext.Attachment = AttachmentPoint.MiddleCenter;

            var textStyleId = GetTextStyle(tr);
            if (textStyleId != ObjectId.Null)
                mtext.TextStyleId = textStyleId;

            string layerName = isSpecial ? "LOTEO-LABELS-SPECIAL" : "LOTEO-LABELS";
            EnsureLayer(tr, layerName);
            mtext.Layer = layerName;

            btr.AppendEntity(mtext);
            tr.AddNewlyCreatedDBObject(mtext, true);
        }

        private ObjectId GetTextStyle(Transaction tr)
        {
            var db = _doc.Database;
            var tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
            if (tst.Has(_settings.TextStyle))
                return tst[_settings.TextStyle];
            return db.Textstyle;
        }

        private void EnsureLayer(Transaction tr, string layerName)
        {
            var db = _doc.Database;
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

            if (!lt.Has(layerName))
            {
                lt.UpgradeOpen();
                var layer = new LayerTableRecord();
                layer.Name = layerName;
                layer.Color = layerName.Contains("SPECIAL")
                    ? Autodesk.AutoCAD.Colors.Color.FromRgb(255, 100, 100)
                    : Autodesk.AutoCAD.Colors.Color.FromRgb(0, 200, 200);
                lt.Add(layer);
                tr.AddNewlyCreatedDBObject(layer, true);
            }
        }
    }
}
