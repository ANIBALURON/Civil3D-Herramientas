using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Civil3D_TubeInfo
{
    public class TubeInfoCommands
    {
        [CommandMethod("INFOTUBOS")]
        public void InfoTubos()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                // Mensaje inicial
                ed.WriteMessage("\n========================================");
                ed.WriteMessage("\n  INFORMACIÓN DE TUBOS - CIVIL 3D");
                ed.WriteMessage("\n========================================\n");
                
                // PASO 1: Seleccionar la Vista de Perfil
                ed.WriteMessage("\nSeleccione la Vista de Perfil donde están los tubos:");
                
                PromptEntityOptions profileOpts = new PromptEntityOptions("\nSeleccione la Vista de Perfil: ");
                profileOpts.SetRejectMessage("\nDebe seleccionar una Vista de Perfil.");
                profileOpts.AddAllowedClass(typeof(ProfileView), false);
                
                PromptEntityResult profileRes = ed.GetEntity(profileOpts);
                if (profileRes.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nSelección cancelada.");
                    return;
                }

                ProfileView profileView = null;
                Alignment alignment = null;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    profileView = tr.GetObject(profileRes.ObjectId, OpenMode.ForRead) as ProfileView;
                    
                    if (profileView == null)
                    {
                        ed.WriteMessage("\nError: No se pudo obtener la Vista de Perfil.");
                        return;
                    }

                    // Obtener el alineamiento asociado
                    ObjectId alignmentId = profileView.AlignmentId;
                    alignment = tr.GetObject(alignmentId, OpenMode.ForRead) as Alignment;
                    
                    if (alignment == null)
                    {
                        ed.WriteMessage("\nError: La Vista de Perfil no tiene un alineamiento asociado.");
                        return;
                    }

                    ed.WriteMessage($"\n✓ Vista de Perfil seleccionada: {profileView.Name}");
                    ed.WriteMessage($"\n✓ Alineamiento: {alignment.Name}");
                    
                    tr.Commit();
                }

                // PASO 2: Seleccionar los bloques (Punta1 e INICIO)
                ed.WriteMessage("\n");
                PromptSelectionOptions pso = new PromptSelectionOptions();
                pso.MessageForAdding = "\nSeleccione los bloques 'Punta1' e 'INICIO':";

                // Crear filtro que acepte ambos tipos de bloques
                TypedValue[] filterList = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start, "INSERT")
                };

                SelectionFilter filter = new SelectionFilter(filterList);
                PromptSelectionResult psr = ed.GetSelection(pso, filter);
                
                if (psr.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nSelección cancelada.");
                    return;
                }

                List<TubeData> tubesData = new List<TubeData>();
                int blocksProcessed = 0;
                int blocksSkipped = 0;

                // PASO 3: Procesar cada bloque
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    profileView = tr.GetObject(profileRes.ObjectId, OpenMode.ForRead) as ProfileView;
                    alignment = tr.GetObject(profileView.AlignmentId, OpenMode.ForRead) as Alignment;

                    foreach (SelectedObject selObj in psr.Value)
                    {
                        if (selObj == null) continue;

                        try
                        {
                            BlockReference blockRef = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as BlockReference;

                            if (blockRef != null && (blockRef.Name == "Punta1" || blockRef.Name == "INICIO"))
                            {
                                TubeData tubeData = new TubeData();
                                
                                // Extraer el atributo NUMERO
                                AttributeCollection attCol = blockRef.AttributeCollection;
                                foreach (ObjectId attId in attCol)
                                {
                                    AttributeReference attRef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                                    if (attRef != null && attRef.Tag.ToUpper() == "NUMERO")
                                    {
                                        tubeData.Numero = attRef.TextString;
                                        break;
                                    }
                                }
                                
                                // Si no tiene número, asignar uno genérico
                                if (string.IsNullOrEmpty(tubeData.Numero))
                                {
                                    tubeData.Numero = $"S/N-{blocksProcessed + 1}";
                                }

                                // CALCULAR COORDENADAS CORRECTAMENTE usando ProfileView y Alignment
                                try
                                {
                                    // Obtener estación y elevación desde la posición del bloque en el perfil
                                    double station = 0;
                                    double elevation = 0;
                                    profileView.FindStationAndElevationAtXY(
                                        blockRef.Position.X, 
                                        blockRef.Position.Y, 
                                        ref station, 
                                        ref elevation);

                                    // Obtener las coordenadas X, Y reales del alineamiento en esa estación
                                    double xReal = 0;
                                    double yReal = 0;
                                    alignment.PointLocation(station, 0, ref xReal, ref yReal);

                                    // Asignar las coordenadas calculadas
                                    tubeData.X = xReal;
                                    tubeData.Y = yReal;
                                    tubeData.Z = elevation;
                                    tubeData.Station = station;

                                    tubesData.Add(tubeData);
                                    blocksProcessed++;
                                }
                                catch (System.Exception ex)
                                {
                                    ed.WriteMessage($"\n⚠ Error calculando coordenadas para tubo {tubeData.Numero}: {ex.Message}");
                                    blocksSkipped++;
                                }
                            }
                            else
                            {
                                blocksSkipped++;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            ed.WriteMessage($"\n⚠ Error procesando bloque: {ex.Message}");
                            blocksSkipped++;
                        }
                    }
                    
                    tr.Commit();
                }

                // Ordenar por número de tubo
                tubesData = tubesData.OrderBy(t => 
                {
                    int num;
                    if (int.TryParse(t.Numero, out num))
                        return num;
                    else
                        return 999999;
                }).ToList();

                // Mostrar ventana con los resultados
                if (tubesData.Count > 0)
                {
                    ed.WriteMessage($"\n✓ Bloques procesados: {blocksProcessed}");
                    if (blocksSkipped > 0)
                        ed.WriteMessage($"\n⚠ Bloques omitidos: {blocksSkipped}");
                    
                    ed.WriteMessage("\n\nAbriendo ventana con información...\n");
                    
                    TubeInfoForm form = new TubeInfoForm(tubesData, alignment.Name);
                    Application.ShowModalDialog(form);
                }
                else
                {
                    ed.WriteMessage("\n✗ No se encontró información de tubos.");
                    ed.WriteMessage("\n  Verifique que los bloques tengan los nombres 'Punta1' o 'INICIO'.\n");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n✗ Error: {ex.Message}");
                ed.WriteMessage($"\n  Detalle: {ex.StackTrace}\n");
            }
        }
    }

    // Clase para almacenar datos de cada tubo
    public class TubeData
    {
        public string Numero { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Station { get; set; }

        // Propiedades formateadas para mostrar
        public string NumeroDisplay => Numero ?? "S/N";
        public string XFormatted => $"{X:F3}";
        public string YFormatted => $"{Y:F3}";
        public string ZFormatted => $"{Z:F3}";

        /// <summary>
        /// Formatea la estación como PK10+500 (ejemplo: 10500 metros = PK10+500)
        /// </summary>
        public string StationFormatted
        {
            get
            {
                int totalMeters = (int)Station;
                int kilometers = totalMeters / 1000;
                int meters = totalMeters % 1000;
                return $"PK{kilometers}+{meters:D3}";
            }
        }
    }
}
