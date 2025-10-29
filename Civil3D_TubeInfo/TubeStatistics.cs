using System;
using System.Collections.Generic;
using System.Linq;

namespace Civil3D_TubeInfo
{
    /// <summary>
    /// Clase para calcular estadísticas sobre los tubos procesados.
    /// </summary>
    public class TubeStatistics
    {
        private readonly List<TubeData> _tubes;

        public TubeStatistics(List<TubeData> tubes)
        {
            _tubes = tubes ?? throw new ArgumentNullException(nameof(tubes));
        }

        /// <summary>
        /// Total de tubos.
        /// </summary>
        public int TotalTubes => _tubes.Count;

        /// <summary>
        /// Coordenada X mínima.
        /// </summary>
        public double MinX => _tubes.Count > 0 ? _tubes.Min(t => t.X) : 0;

        /// <summary>
        /// Coordenada X máxima.
        /// </summary>
        public double MaxX => _tubes.Count > 0 ? _tubes.Max(t => t.X) : 0;

        /// <summary>
        /// Coordenada X promedio.
        /// </summary>
        public double AverageX => _tubes.Count > 0 ? _tubes.Average(t => t.X) : 0;

        /// <summary>
        /// Coordenada Y mínima.
        /// </summary>
        public double MinY => _tubes.Count > 0 ? _tubes.Min(t => t.Y) : 0;

        /// <summary>
        /// Coordenada Y máxima.
        /// </summary>
        public double MaxY => _tubes.Count > 0 ? _tubes.Max(t => t.Y) : 0;

        /// <summary>
        /// Coordenada Y promedio.
        /// </summary>
        public double AverageY => _tubes.Count > 0 ? _tubes.Average(t => t.Y) : 0;

        /// <summary>
        /// Coordenada Z (elevación) mínima.
        /// </summary>
        public double MinZ => _tubes.Count > 0 ? _tubes.Min(t => t.Z) : 0;

        /// <summary>
        /// Coordenada Z (elevación) máxima.
        /// </summary>
        public double MaxZ => _tubes.Count > 0 ? _tubes.Max(t => t.Z) : 0;

        /// <summary>
        /// Coordenada Z (elevación) promedio.
        /// </summary>
        public double AverageZ => _tubes.Count > 0 ? _tubes.Average(t => t.Z) : 0;

        /// <summary>
        /// Rango de estaciones.
        /// </summary>
        public double MinStation => _tubes.Count > 0 ? _tubes.Min(t => t.Station) : 0;

        /// <summary>
        /// Estación máxima.
        /// </summary>
        public double MaxStation => _tubes.Count > 0 ? _tubes.Max(t => t.Station) : 0;

        /// <summary>
        /// Diferencia de elevación entre máxima y mínima.
        /// </summary>
        public double ElevationDifference => MaxZ - MinZ;

        /// <summary>
        /// Obtiene un resumen de estadísticas formateado.
        /// </summary>
        public string GetSummary()
        {
            return $@"
ESTADÍSTICAS DE TUBOS
═════════════════════════════════════════
Total de tubos: {TotalTubes}

Coordenada X (m):
  Mínimo: {MinX:F3}
  Máximo: {MaxX:F3}
  Promedio: {AverageX:F3}

Coordenada Y (m):
  Mínimo: {MinY:F3}
  Máximo: {MaxY:F3}
  Promedio: {AverageY:F3}

Coordenada Z - Elevación (m):
  Mínimo: {MinZ:F3}
  Máximo: {MaxZ:F3}
  Promedio: {AverageZ:F3}
  Diferencia: {ElevationDifference:F3}

Estaciones (m):
  Mínima: {MinStation:F3}
  Máxima: {MaxStation:F3}
═════════════════════════════════════════";
        }
    }
}
