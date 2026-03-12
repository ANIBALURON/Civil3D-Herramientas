"""
Descarga y recorte de capas INEGI para el proyecto Cuxtal II
- Red vial (caminos, carreteras, autopistas) desde RNC ya descargada
- Vias ferreas desde Overpass API (OpenStreetMap / datos INEGI)
- Recorta todo al area del gasoducto con buffer de 5 km

Requisitos: ogr2ogr (viene con QGIS/OSGeo4W), requests (pip)
Uso: python descargar_capas_inegi.py
"""

import subprocess
import os
import sys
import json
import csv
import math
import urllib.request
import urllib.parse
import time

# === CONFIGURACION ===
OGR2OGR = r"C:\Users\Anibal\AppData\Local\Programs\OSGeo4W\bin\ogr2ogr.exe"
OGRINFO = r"C:\Users\Anibal\AppData\Local\Programs\OSGeo4W\bin\ogrinfo.exe"

# Carpeta de la RNC ya descargada
RNC_DIR = r"D:\CUXTAL_II\04_PREDOBLADO\GIS\Inegi Carreteras\889463807452_s\conjunto_de_datos"
RED_VIAL_SHP = os.path.join(RNC_DIR, "red_vial.shp")
ESTRUCTURA_SHP = os.path.join(RNC_DIR, "estructura.shp")
PUENTE_SHP = os.path.join(RNC_DIR, "puente.shp")

# Carpeta de salida
OUTPUT_DIR = r"D:\CUXTAL_II\04_PREDOBLADO\GIS\Inegi_Capas_Proyecto"

# Coordenadas CSV del proyecto
COORDS_CSV = r"D:\CUXTAL_II\04_PREDOBLADO\3_COORDENADAS TUBOS\coordenas.csv"

# Buffer en km alrededor del gasoducto
BUFFER_KM = 5

# CRS de salida
EPSG_OUT = 32615  # UTM zona 15N


def utm_to_latlon(easting, northing, zone=15):
    """Convierte UTM zona 15N a lat/lon WGS84."""
    k0 = 0.9996
    a = 6378137.0
    e = 0.0818191908426
    e2 = e * e
    e_p2 = e2 / (1 - e2)
    x = easting - 500000.0
    y = northing
    M = y / k0
    mu = M / (a * (1 - e2/4 - 3*e2**2/64 - 5*e2**3/256))
    e1 = (1 - math.sqrt(1 - e2)) / (1 + math.sqrt(1 - e2))
    phi1 = (mu + (3*e1/2 - 27*e1**3/32)*math.sin(2*mu)
            + (21*e1**2/16 - 55*e1**4/32)*math.sin(4*mu)
            + (151*e1**3/96)*math.sin(6*mu))
    N1 = a / math.sqrt(1 - e2 * math.sin(phi1)**2)
    T1 = math.tan(phi1)**2
    C1 = e_p2 * math.cos(phi1)**2
    R1 = a * (1 - e2) / (1 - e2 * math.sin(phi1)**2)**1.5
    D = x / (N1 * k0)
    lat = phi1 - (N1*math.tan(phi1)/R1) * (
        D**2/2
        - (5+3*T1+10*C1-4*C1**2-9*e_p2)*D**4/24
        + (61+90*T1+298*C1+45*T1**2-252*e_p2-3*C1**2)*D**6/720
    )
    lon = (D - (1+2*T1+C1)*D**3/6
           + (5-2*C1+28*T1-3*C1**2+8*e_p2+24*T1**2)*D**5/120
           ) / math.cos(phi1)
    cm = (zone - 1) * 6 - 180 + 3
    return math.degrees(lat), math.degrees(lon) + cm


def get_project_bbox():
    """Lee coordenadas del proyecto y calcula bbox con buffer."""
    xs, ys = [], []
    with open(COORDS_CSV, 'r') as f:
        reader = csv.reader(f)
        for row in reader:
            try:
                x, y = float(row[1]), float(row[2])
                xs.append(x)
                ys.append(y)
            except (ValueError, IndexError):
                continue

    lat1, lon1 = utm_to_latlon(min(xs), min(ys))
    lat2, lon2 = utm_to_latlon(max(xs), max(ys))
    lat_min, lat_max = min(lat1, lat2), max(lat1, lat2)
    lon_min, lon_max = min(lon1, lon2), max(lon1, lon2)

    # Buffer en grados (~0.045 grados por km en esta latitud)
    buf = BUFFER_KM * 0.009
    bbox = {
        'lon_min': lon_min - buf,
        'lat_min': lat_min - buf,
        'lon_max': lon_max + buf,
        'lat_max': lat_max + buf,
    }
    print(f"  Puntos del proyecto: {len(xs)}")
    print(f"  Lat: {lat_min:.4f} - {lat_max:.4f}")
    print(f"  Lon: {lon_min:.4f} - {lon_max:.4f}")
    print(f"  Bbox con buffer {BUFFER_KM}km: "
          f"{bbox['lon_min']:.4f},{bbox['lat_min']:.4f},"
          f"{bbox['lon_max']:.4f},{bbox['lat_max']:.4f}")
    return bbox


def run_ogr2ogr(args, desc=""):
    """Ejecuta ogr2ogr con los argumentos dados."""
    cmd = [OGR2OGR] + args
    print(f"  Ejecutando: {desc}...")
    result = subprocess.run(cmd, capture_output=True, text=True, timeout=300)
    if result.returncode != 0:
        print(f"  ERROR: {result.stderr[:500]}")
        return False
    return True


def clip_rnc_layer(input_shp, output_name, bbox, where_clause=None):
    """Recorta una capa de la RNC al bbox del proyecto."""
    output_shp = os.path.join(OUTPUT_DIR, f"{output_name}.shp")

    # Formato bbox para ogr2ogr: -spat xmin ymin xmax ymax
    # La RNC usa EPSG:6365 (Mexico ITRF2008) que es practicamente WGS84
    args = [
        "-f", "ESRI Shapefile",
        output_shp,
        input_shp,
        "-spat", str(bbox['lon_min']), str(bbox['lat_min']),
              str(bbox['lon_max']), str(bbox['lat_max']),
        "-t_srs", f"EPSG:{EPSG_OUT}",
        "-lco", "ENCODING=UTF-8",
        "-overwrite",
    ]
    if where_clause:
        args.extend(["-where", where_clause])

    success = run_ogr2ogr(args, f"Recortando {output_name}")
    if success and os.path.exists(output_shp):
        # Contar features
        info = subprocess.run(
            [OGRINFO, "-so", output_shp, output_name],
            capture_output=True, text=True
        )
        for line in info.stdout.splitlines():
            if "Feature Count" in line:
                print(f"  -> {output_name}.shp: {line.strip()}")
                break
    return success


def download_railways_osm(bbox):
    """Descarga vias ferreas de Overpass API (OpenStreetMap)."""
    print("\n[3/4] Descargando vias ferreas desde OpenStreetMap (Overpass API)...")

    # Query Overpass para vias ferreas en el bbox
    overpass_query = f"""
    [out:json][timeout:60];
    (
      way["railway"="rail"]({bbox['lat_min']},{bbox['lon_min']},{bbox['lat_max']},{bbox['lon_max']});
      way["railway"="abandoned"]({bbox['lat_min']},{bbox['lon_min']},{bbox['lat_max']},{bbox['lon_max']});
      way["railway"="disused"]({bbox['lat_min']},{bbox['lon_min']},{bbox['lat_max']},{bbox['lon_max']});
    );
    out body;
    >;
    out skel qt;
    """

    url = "https://overpass-api.de/api/interpreter"
    data = urllib.parse.urlencode({"data": overpass_query}).encode()
    req = urllib.request.Request(url, data=data)

    try:
        print("  Consultando Overpass API...")
        resp = urllib.request.urlopen(req, timeout=120)
        osm_data = json.loads(resp.read().decode())
    except Exception as e:
        print(f"  ERROR descargando de Overpass: {e}")
        return False

    # Parsear respuesta OSM a GeoJSON
    nodes = {}
    ways = []
    for elem in osm_data.get('elements', []):
        if elem['type'] == 'node':
            nodes[elem['id']] = (elem['lon'], elem['lat'])
        elif elem['type'] == 'way':
            ways.append(elem)

    if not ways:
        print("  No se encontraron vias ferreas en el area del proyecto.")
        return True

    # Crear GeoJSON
    features = []
    for way in ways:
        coords = []
        for nd in way.get('nodes', []):
            if nd in nodes:
                coords.append(list(nodes[nd]))
        if len(coords) >= 2:
            tags = way.get('tags', {})
            feature = {
                "type": "Feature",
                "geometry": {
                    "type": "LineString",
                    "coordinates": coords
                },
                "properties": {
                    "osm_id": way['id'],
                    "railway": tags.get('railway', ''),
                    "name": tags.get('name', ''),
                    "operator": tags.get('operator', ''),
                    "usage": tags.get('usage', ''),
                    "source": tags.get('source', 'OpenStreetMap'),
                }
            }
            features.append(feature)

    geojson = {
        "type": "FeatureCollection",
        "features": features
    }

    # Guardar GeoJSON temporal
    temp_geojson = os.path.join(OUTPUT_DIR, "vias_ferreas_temp.geojson")
    with open(temp_geojson, 'w', encoding='utf-8') as f:
        json.dump(geojson, f, ensure_ascii=False)

    print(f"  Encontradas {len(features)} vias ferreas")

    # Convertir a shapefile UTM con ogr2ogr
    output_shp = os.path.join(OUTPUT_DIR, "vias_ferreas.shp")
    args = [
        "-f", "ESRI Shapefile",
        output_shp,
        temp_geojson,
        "-t_srs", f"EPSG:{EPSG_OUT}",
        "-s_srs", "EPSG:4326",
        "-lco", "ENCODING=UTF-8",
        "-overwrite",
    ]
    success = run_ogr2ogr(args, "Convirtiendo vias ferreas a SHP/UTM15")

    # Limpiar temporal
    if os.path.exists(temp_geojson):
        os.remove(temp_geojson)

    if success:
        print(f"  -> vias_ferreas.shp: {len(features)} features")
    return success


def create_summary(bbox):
    """Crea un resumen de las capas generadas."""
    summary_file = os.path.join(OUTPUT_DIR, "RESUMEN_CAPAS.txt")
    shapefiles = [f for f in os.listdir(OUTPUT_DIR) if f.endswith('.shp')]

    lines = [
        "=" * 60,
        "CAPAS INEGI / OSM - PROYECTO CUXTAL II",
        "=" * 60,
        f"Generado: {time.strftime('%Y-%m-%d %H:%M')}",
        f"CRS: EPSG:{EPSG_OUT} (UTM Zona 15N)",
        f"Buffer: {BUFFER_KM} km alrededor del gasoducto",
        f"Bbox (lon,lat): {bbox['lon_min']:.4f},{bbox['lat_min']:.4f},"
        f"{bbox['lon_max']:.4f},{bbox['lat_max']:.4f}",
        "",
        "CAPAS GENERADAS:",
        "-" * 40,
    ]

    for shp in sorted(shapefiles):
        name = shp.replace('.shp', '')
        info = subprocess.run(
            [OGRINFO, "-so", os.path.join(OUTPUT_DIR, shp), name],
            capture_output=True, text=True
        )
        count = "?"
        for line in info.stdout.splitlines():
            if "Feature Count" in line:
                count = line.split(":")[-1].strip()
                break
        lines.append(f"  {shp:40s} -> {count} features")

    lines.extend([
        "",
        "TIPOS DE VIALIDAD EN red_vial_completa:",
        "  Carretera, Camino, Autopista (Boulevard, Periferico,",
        "  Circunvalacion), Calles, Avenidas, etc.",
        "",
        "CAPAS SEPARADAS:",
        "  carreteras.shp    -> Solo tipo 'Carretera'",
        "  caminos.shp       -> Solo tipo 'Camino' y 'Vereda'",
        "  vias_ferreas.shp  -> Ferrocarriles (fuente: OpenStreetMap/INEGI)",
        "  puentes.shp       -> Puentes y estructuras viales",
        "",
        "FUENTES:",
        "  Red vial: Red Nacional de Caminos (RNC) INEGI 2025",
        "  Vias ferreas: OpenStreetMap (datos originales INEGI)",
        "",
        "Para cargar en QGIS: Capa > Anadir capa > Anadir capa vectorial",
        "=" * 60,
    ])

    with open(summary_file, 'w', encoding='utf-8') as f:
        f.write('\n'.join(lines))
    print(f"\n  Resumen guardado en: {summary_file}")


def main():
    print("=" * 60)
    print("DESCARGA CAPAS INEGI - PROYECTO CUXTAL II")
    print("=" * 60)

    # Verificar que ogr2ogr existe
    if not os.path.exists(OGR2OGR):
        print(f"ERROR: No se encontro ogr2ogr en {OGR2OGR}")
        sys.exit(1)

    # Verificar que la RNC existe
    if not os.path.exists(RED_VIAL_SHP):
        print(f"ERROR: No se encontro red_vial.shp en {RNC_DIR}")
        sys.exit(1)

    # Crear carpeta de salida
    os.makedirs(OUTPUT_DIR, exist_ok=True)

    # Calcular bbox
    print("\n[1/4] Calculando extension del proyecto...")
    bbox = get_project_bbox()

    # Recortar red vial completa
    print("\n[2/4] Recortando Red Nacional de Caminos al area del proyecto...")

    # a) Red vial completa
    clip_rnc_layer(RED_VIAL_SHP, "red_vial_completa", bbox)

    # b) Solo carreteras
    clip_rnc_layer(RED_VIAL_SHP, "carreteras", bbox,
                   where_clause="TIPO_VIAL = 'Carretera'")

    # c) Solo caminos y veredas
    clip_rnc_layer(RED_VIAL_SHP, "caminos", bbox,
                   where_clause="TIPO_VIAL IN ('Camino', 'Vereda')")

    # d) Autopistas/vias rapidas (boulevard, periferico, etc.)
    clip_rnc_layer(RED_VIAL_SHP, "autopistas_vias_rapidas", bbox,
                   where_clause="TIPO_VIAL IN ('Boulevard', 'Periférico', "
                   "'Circunvalación', 'Eje vial', 'Viaducto', 'Calzada')")

    # e) Puentes
    if os.path.exists(PUENTE_SHP):
        clip_rnc_layer(PUENTE_SHP, "puentes", bbox)

    # f) Estructuras viales
    if os.path.exists(ESTRUCTURA_SHP):
        clip_rnc_layer(ESTRUCTURA_SHP, "estructuras", bbox)

    # Descargar vias ferreas de OSM
    download_railways_osm(bbox)

    # Resumen
    print("\n[4/4] Generando resumen...")
    create_summary(bbox)

    print("\n" + "=" * 60)
    print(f"LISTO! Capas guardadas en:")
    print(f"  {OUTPUT_DIR}")
    print("=" * 60)


if __name__ == "__main__":
    main()
