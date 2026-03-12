using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace LoteoCivil3D.Core
{
    [Serializable]
    public class GraderSettings
    {
        // Tipo de distribucion de lotes
        public LotLayoutType LayoutType { get; set; } = LotLayoutType.WithPad;

        // Pendientes de lote (%)
        public double LotSlopeMin { get; set; } = 2.0;
        public double LotSlopeMax { get; set; } = 5.0;
        public double LotSlopeDefault { get; set; } = 2.0;

        // Pad (plataforma de casa) - solo para WithPad
        public double PadOffsetFromLot { get; set; } = 1.0;
        public double PadElevationOffset { get; set; } = 0.15;
        public bool PadSlopeAway { get; set; } = true;
        public double PadSlope { get; set; } = 2.0;

        // Reveal (retiro/separacion del pad) - solo para WithPad
        public bool UseReveal { get; set; } = true;
        public double RevealDistance { get; set; } = 0.30;
        public double RevealDrop { get; set; } = 0.05;

        // Calle frontal
        public StreetElevationSource StreetSource { get; set; } = StreetElevationSource.Surface;
        public double FrontSetback { get; set; } = 3.0;
        public double FrontSlopeToStreet { get; set; } = 2.0;
        public bool MatchStreetElevation { get; set; } = true;

        // Drenaje posterior
        public double RearSlope { get; set; } = 2.0;
        public double RearPatioSlope { get; set; } = 1.5;
        public bool DrainToRear { get; set; } = true;

        // Laterales
        public double SideSlope { get; set; } = 2.0;
        public bool MatchAdjacentLots { get; set; } = false;

        // Back-to-back: Pendiente desde divisoria trasera a calle (%)
        public double BackToBackSlope { get; set; } = 2.0;
        // Back-to-back: Elevacion adicional en divisoria trasera sobre la calle (m)
        public double RidgeExtraElevation { get; set; } = 0.0;

        // Superficie
        public string SurfaceName { get; set; } = "EG";
        public string GradingSurfaceName { get; set; } = "FG-LOTES";
        public bool AddToSurface { get; set; } = true;
        public string BreaklineGroupName { get; set; } = "Loteo_Breaklines";

        // Estilos de Feature Line
        public string LotLineStyle { get; set; } = "Lot Line";
        public string PadStyle { get; set; } = "Pad";
        public string RevealStyle { get; set; } = "Reveal";

        // Grading direction
        public GradeDirection Direction { get; set; } = GradeDirection.FrontToBack;
    }

    /// <summary>
    /// Tipo de distribucion de lotes
    /// </summary>
    public enum LotLayoutType
    {
        /// <summary>Lote con pad/casa interior, retiros, reveal (estilo norteamericano)</summary>
        WithPad,
        /// <summary>Lotes espalda con espalda, sin pad, drenaje a calle frontal (estilo latinoamericano)</summary>
        BackToBack
    }

    /// <summary>
    /// De donde tomar la elevacion del frente del lote (la calle)
    /// </summary>
    public enum StreetElevationSource
    {
        /// <summary>Leer de una superficie (EG, corredor, etc)</summary>
        Surface,
        /// <summary>Leer de un Feature Line de calle (borde sardinel, borde pavimento)</summary>
        FeatureLine
    }

    public enum GradeDirection
    {
        FrontToBack,
        BackToFront,
        HighToLow,
        Custom
    }

    [Serializable]
    public class LabelerSettings
    {
        public bool LabelLotElevations { get; set; } = true;
        public bool LabelHighPoints { get; set; } = true;
        public bool LabelPadElevations { get; set; } = true;
        public bool LabelRevealElevations { get; set; } = false;
        public bool LabelSlopes { get; set; } = true;
        public bool LabelLotNumbers { get; set; } = true;

        public string ElevationLabelStyle { get; set; } = "Standard";
        public string SlopeLabelStyle { get; set; } = "Standard";
        public string TextStyle { get; set; } = "Standard";
        public double TextHeight { get; set; } = 0.10;
        public int DecimalPlaces { get; set; } = 2;
    }

    public static class SettingsManager
    {
        private static readonly string SettingsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LoteoCivil3D");

        private static readonly string GraderFile = Path.Combine(SettingsFolder, "GraderSettings.xml");
        private static readonly string LabelerFile = Path.Combine(SettingsFolder, "LabelerSettings.xml");

        public static GraderSettings CurrentGrader { get; set; } = new GraderSettings();
        public static LabelerSettings CurrentLabeler { get; set; } = new LabelerSettings();

        public static void LoadAll()
        {
            CurrentGrader = Load<GraderSettings>(GraderFile) ?? new GraderSettings();
            CurrentLabeler = Load<LabelerSettings>(LabelerFile) ?? new LabelerSettings();
        }

        public static void SaveAll()
        {
            Save(CurrentGrader, GraderFile);
            Save(CurrentLabeler, LabelerFile);
        }

        public static void SaveGrader() => Save(CurrentGrader, GraderFile);
        public static void SaveLabeler() => Save(CurrentLabeler, LabelerFile);

        private static T Load<T>(string path) where T : class
        {
            try
            {
                if (!File.Exists(path)) return null;
                var serializer = new XmlSerializer(typeof(T));
                using (var reader = new StreamReader(path))
                    return (T)serializer.Deserialize(reader);
            }
            catch { return null; }
        }

        private static void Save<T>(T obj, string path)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var serializer = new XmlSerializer(typeof(T));
                using (var writer = new StreamWriter(path))
                    serializer.Serialize(writer, obj);
            }
            catch { }
        }

        public static void ExportSettings(string path)
        {
            var bundle = new SettingsBundle { Grader = CurrentGrader, Labeler = CurrentLabeler };
            Save(bundle, path);
        }

        public static void ImportSettings(string path)
        {
            var bundle = Load<SettingsBundle>(path);
            if (bundle != null)
            {
                CurrentGrader = bundle.Grader ?? new GraderSettings();
                CurrentLabeler = bundle.Labeler ?? new LabelerSettings();
                SaveAll();
            }
        }
    }

    [Serializable]
    public class SettingsBundle
    {
        public GraderSettings Grader { get; set; }
        public LabelerSettings Labeler { get; set; }
    }
}
