using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.Civil.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Civil3D_TubeInfo
{
    /// <summary>
    /// Servicio especializado en el procesamiento de datos de tubos desde bloques Civil 3D.
    /// Encapsula toda la lógica de negocio separada de la UI y comandos.
    /// </summary>
    public class TubeProcessingService
    {
        private readonly Database _database;
        private readonly Editor _editor;

        public TubeProcessingService(Database database, Editor editor)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        }

        /// <summary>
        /// Procesa los bloques seleccionados y extrae la información de tubos.
        /// </summary>
        public TubeProcessingResult ProcessBlocks(SelectionSet selectionSet, ProfileView profileView, Alignment alignment)
        {
            if (selectionSet == null)
                throw new ArgumentNullException(nameof(selectionSet));
            if (profileView == null)
                throw new ArgumentNullException(nameof(profileView));
            if (alignment == null)
                throw new ArgumentNullException(nameof(alignment));

            var result = new TubeProcessingResult();
            var tubesData = new List<TubeData>();

            Logger.Info($"Iniciando procesamiento de {selectionSet.Count} bloques seleccionados");

            try
            {
                using (var tr = _database.TransactionManager.StartTransaction())
                {
                    profileView = tr.GetObject(profileView.ObjectId, OpenMode.ForRead) as ProfileView;
                    alignment = tr.GetObject(alignment.ObjectId, OpenMode.ForRead) as Alignment;

                    foreach (SelectedObject selObj in selectionSet)
                    {
                        if (selObj == null) continue;

                        try
                        {
                            var blockRef = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as BlockReference;

                            if (blockRef != null && (blockRef.Name == "Punta1" || blockRef.Name == "INICIO"))
                            {
                                var tubeData = ExtractTubeData(blockRef, profileView, alignment, tr);

                                if (tubeData != null)
                                {
                                    tubesData.Add(tubeData);
                                    result.BlocksProcessed++;
                                }
                            }
                            else
                            {
                                result.BlocksSkipped++;
                                Logger.Debug($"Bloque omitido: tipo inválido o no es 'Punta1' ni 'INICIO'");
                            }
                        }
                        catch (Exception ex)
                        {
                            result.BlocksSkipped++;
                            Logger.Error($"Error procesando bloque {selObj.ObjectId}", ex);
                        }
                    }

                    tr.Commit();
                }

                // Ordenar por número de tubo
                result.TubesData = OrderTubesByNumber(tubesData);
                result.IsSuccess = result.BlocksProcessed > 0;

                Logger.Info($"Procesamiento completado: {result.BlocksProcessed} bloques procesados, " +
                           $"{result.BlocksSkipped} omitidos, {result.TubesData.Count} tubos válidos");
            }
            catch (Exception ex)
            {
                Logger.Error("Error fatal en procesamiento de bloques", ex);
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Extrae la información de un bloque individual.
        /// </summary>
        private TubeData ExtractTubeData(BlockReference blockRef, ProfileView profileView,
                                        Alignment alignment, Transaction tr)
        {
            var tubeData = new TubeData();

            try
            {
                // Extraer atributo NUMERO
                var attCol = blockRef.AttributeCollection;
                foreach (ObjectId attId in attCol)
                {
                    var attRef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                    if (attRef != null && attRef.Tag.ToUpper() == "NUMERO")
                    {
                        tubeData.Numero = attRef.TextString;
                        break;
                    }
                }

                // Asignar número genérico si no existe
                if (string.IsNullOrEmpty(tubeData.Numero))
                {
                    tubeData.Numero = "S/N";
                }

                // Calcular coordenadas usando ProfileView y Alignment
                double station = 0;
                double elevation = 0;
                profileView.FindStationAndElevationAtXY(
                    blockRef.Position.X,
                    blockRef.Position.Y,
                    ref station,
                    ref elevation);

                // Obtener coordenadas reales del alineamiento
                double xReal = 0;
                double yReal = 0;
                alignment.PointLocation(station, 0, ref xReal, ref yReal);

                tubeData.X = xReal;
                tubeData.Y = yReal;
                tubeData.Z = elevation;
                tubeData.Station = station;

                return tubeData;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error extrayendo datos del bloque", ex);
                return null;
            }
        }

        /// <summary>
        /// Ordena los tubos por número en forma inteligente.
        /// </summary>
        private List<TubeData> OrderTubesByNumber(List<TubeData> tubes)
        {
            return tubes.OrderBy(t =>
            {
                if (int.TryParse(t.Numero, out int num))
                    return num;
                else
                    return 999999;
            }).ToList();
        }
    }

    /// <summary>
    /// Resultado del procesamiento de tubos.
    /// </summary>
    public class TubeProcessingResult
    {
        public bool IsSuccess { get; set; }
        public List<TubeData> TubesData { get; set; } = new List<TubeData>();
        public int BlocksProcessed { get; set; }
        public int BlocksSkipped { get; set; }
        public string ErrorMessage { get; set; }
    }
}
