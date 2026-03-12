"""
Panel QGIS - Cartera Predoblado
TopografiaCivil3d.com

Plugin de panel para QGIS que permite filtrar, estilizar, analizar
y exportar datos de la cartera de predoblado de gasoductos.
"""

import os
import sys
import csv
import configparser
from datetime import datetime

from qgis.PyQt.QtWidgets import (
    QDockWidget, QWidget, QVBoxLayout, QHBoxLayout, QGridLayout,
    QPushButton, QLabel, QLineEdit, QComboBox, QTextEdit,
    QTabWidget, QFileDialog, QMessageBox, QGroupBox, QCheckBox,
    QSpinBox, QSizePolicy
)
from qgis.PyQt.QtCore import Qt, QVariant
from qgis.PyQt.QtGui import QColor, QFont
from qgis.core import (
    QgsProject, QgsVectorLayer, QgsFeatureRequest, QgsExpression,
    QgsRuleBasedRenderer, QgsSymbol, QgsMarkerSymbol,
    QgsPalLayerSettings, QgsVectorLayerSimpleLabeling,
    QgsTextFormat, QgsTextBufferSettings,
    QgsRuleBasedLabeling, QgsMapLayerStyle,
    QgsLayerTreeGroup, QgsLayerTreeLayer,
    QgsFeature, QgsGeometry, QgsPointXY, QgsField,
    Qgis
)
from qgis.utils import iface


# ============================================================================
# PANEL PRINCIPAL
# ============================================================================
class PanelCartera(QDockWidget):

    def __init__(self, parent=None):
        super().__init__("Cartera Predoblado", parent)
        self.setAllowedAreas(Qt.LeftDockWidgetArea | Qt.RightDockWidgetArea)

        self.proyecto_path = ''
        self.config = None
        self.capa_activa = None

        self._build_ui()
        self._cargar_proyecto()

    # ====================================================================
    # CONSTRUCCION DE LA UI
    # ====================================================================
    def _build_ui(self):
        main_widget = QWidget()
        main_layout = QVBoxLayout(main_widget)
        main_layout.setContentsMargins(5, 5, 5, 5)

        # --- SECCION PROYECTO ---
        grp_proyecto = QGroupBox("Proyecto")
        lay_proy = QVBoxLayout(grp_proyecto)

        row_carpeta = QHBoxLayout()
        self.txt_carpeta = QLineEdit()
        self.txt_carpeta.setPlaceholderText("Carpeta raiz del proyecto...")
        btn_carpeta = QPushButton("[...]")
        btn_carpeta.setFixedWidth(35)
        btn_carpeta.clicked.connect(self._seleccionar_carpeta)
        row_carpeta.addWidget(self.txt_carpeta)
        row_carpeta.addWidget(btn_carpeta)
        lay_proy.addLayout(row_carpeta)

        self.btn_recargar = QPushButton("RECARGAR DATOS (Merge + Actualizar QGIS)")
        self.btn_recargar.setStyleSheet("QPushButton { background-color: #27ae60; color: white; font-weight: bold; padding: 8px; }")
        self.btn_recargar.clicked.connect(self._recargar_datos)
        lay_proy.addWidget(self.btn_recargar)

        main_layout.addWidget(grp_proyecto)

        # --- SELECTOR DE CAPA ---
        row_capa = QHBoxLayout()
        row_capa.addWidget(QLabel("Capa activa:"))
        self.combo_capa = QComboBox()
        self.combo_capa.currentIndexChanged.connect(self._cambiar_capa)
        row_capa.addWidget(self.combo_capa, 1)
        main_layout.addLayout(row_capa)

        # --- PESTANAS ---
        self.tabs = QTabWidget()

        # Pestaña FILTROS
        self.tab_filtros = QWidget()
        self._build_tab_filtros()
        self.tabs.addTab(self.tab_filtros, "FILTROS")

        # Pestaña ESTILOS
        self.tab_estilos = QWidget()
        self._build_tab_estilos()
        self.tabs.addTab(self.tab_estilos, "ESTILOS")

        # Pestaña ESTADISTICAS
        self.tab_stats = QWidget()
        self._build_tab_stats()
        self.tabs.addTab(self.tab_stats, "ESTADISTICAS")

        # Pestaña EXPORTAR
        self.tab_export = QWidget()
        self._build_tab_export()
        self.tabs.addTab(self.tab_export, "EXPORTAR")

        main_layout.addWidget(self.tabs, 1)

        # --- ZOOM + STATUS ---
        self.btn_zoom = QPushButton("Zoom a elementos")
        self.btn_zoom.clicked.connect(self._zoom_a_elementos)
        main_layout.addWidget(self.btn_zoom)

        self.lbl_status = QLabel("Listo")
        self.lbl_status.setStyleSheet("color: #666; font-size: 10px;")
        main_layout.addWidget(self.lbl_status)

        self.setWidget(main_widget)
        self._actualizar_combo_capas()

    def _build_tab_filtros(self):
        lay = QVBoxLayout(self.tab_filtros)

        # Botones rapidos
        grp_rapidos = QGroupBox("Filtros rapidos")
        grid = QGridLayout(grp_rapidos)
        botones = [
            ("TODOS", ""),
            ("CURVAS", "\"GRADOS Y DIRECCION\" IS NOT NULL AND \"GRADOS Y DIRECCION\" != ''"),
            ("CODOS", "upper(\"NOTAS\") LIKE '%CODO%'"),
            ("RECTOS", "(\"GRADOS Y DIRECCION\" IS NULL OR \"GRADOS Y DIRECCION\" = '') AND (\"NOTAS\" IS NULL OR upper(\"NOTAS\") NOT LIKE '%CODO%' AND upper(\"NOTAS\") NOT LIKE '%INICIO%' AND upper(\"NOTAS\") NOT LIKE '%FIN%' AND upper(\"NOTAS\") NOT LIKE '%PERFORADO%' AND upper(\"NOTAS\") NOT LIKE '%NIPLE%')"),
            ("ESPECIALES", "upper(\"NOTAS\") LIKE '%INICIO%' OR upper(\"NOTAS\") LIKE '%FIN%'"),
            ("NIPLES", "upper(\"NOTAS\") LIKE '%NIPLE%'"),
            ("PERFORADOS", "upper(\"NOTAS\") LIKE '%PERFORADO%'"),
        ]
        for i, (nombre, expr) in enumerate(botones):
            btn = QPushButton(nombre)
            btn.clicked.connect(lambda checked, e=expr, n=nombre: self._aplicar_filtro(e, n))
            grid.addWidget(btn, i // 4, i % 4)
        lay.addWidget(grp_rapidos)

        # Filtro por TRAMO
        grp_tramo = QGroupBox("Filtrar por tramo")
        lay_tramo = QVBoxLayout(grp_tramo)
        self.combo_tramo = QComboBox()
        self.combo_tramo.addItem("TODOS LOS TRAMOS")
        self.combo_tramo.currentIndexChanged.connect(self._filtrar_por_tramo)
        lay_tramo.addWidget(self.combo_tramo)
        lay.addWidget(grp_tramo)

        # Rango de tubos
        grp_rango = QGroupBox("Rango de tubos")
        lay_rango = QHBoxLayout(grp_rango)
        lay_rango.addWidget(QLabel("Desde:"))
        self.spin_tubo_desde = QSpinBox()
        self.spin_tubo_desde.setRange(0, 99999)
        lay_rango.addWidget(self.spin_tubo_desde)
        lay_rango.addWidget(QLabel("Hasta:"))
        self.spin_tubo_hasta = QSpinBox()
        self.spin_tubo_hasta.setRange(0, 99999)
        self.spin_tubo_hasta.setValue(99999)
        lay_rango.addWidget(self.spin_tubo_hasta)
        btn_rango = QPushButton("Filtrar")
        btn_rango.clicked.connect(self._filtrar_rango_tubos)
        lay_rango.addWidget(btn_rango)
        lay.addWidget(grp_rango)

        # Rango PK/Junta
        grp_pk = QGroupBox("Rango PK/Junta")
        lay_pk = QHBoxLayout(grp_pk)
        lay_pk.addWidget(QLabel("Desde:"))
        self.txt_pk_desde = QLineEdit()
        self.txt_pk_desde.setPlaceholderText("0+000")
        lay_pk.addWidget(self.txt_pk_desde)
        lay_pk.addWidget(QLabel("Hasta:"))
        self.txt_pk_hasta = QLineEdit()
        self.txt_pk_hasta.setPlaceholderText("99+999")
        lay_pk.addWidget(self.txt_pk_hasta)
        btn_pk = QPushButton("Filtrar")
        btn_pk.clicked.connect(self._filtrar_rango_pk)
        lay_pk.addWidget(btn_pk)
        lay.addWidget(grp_pk)

        # Busqueda libre
        grp_buscar = QGroupBox("Buscar texto")
        lay_buscar = QHBoxLayout(grp_buscar)
        self.txt_buscar = QLineEdit()
        self.txt_buscar.setPlaceholderText("Buscar en todos los campos...")
        self.txt_buscar.returnPressed.connect(self._buscar_texto)
        lay_buscar.addWidget(self.txt_buscar)
        btn_buscar = QPushButton("Buscar")
        btn_buscar.clicked.connect(self._buscar_texto)
        lay_buscar.addWidget(btn_buscar)
        lay.addWidget(grp_buscar)

        lay.addStretch()

    def _build_tab_estilos(self):
        lay = QVBoxLayout(self.tab_estilos)

        botones_estilo = [
            ("Colorear por tipo", self._estilo_colorear_tipo),
            ("Etiquetas N. TUBO", self._etiqueta_ntubo),
            ("Etiquetas INICIO/FIN", self._etiqueta_inicio_fin),
            ("Etiquetas PK/JUNTA", self._etiqueta_pk),
            ("Etiquetas GRADOS", self._etiqueta_grados),
            ("Quitar etiquetas", self._quitar_etiquetas),
            ("Restablecer estilo", self._restablecer_estilo),
        ]
        for texto, func in botones_estilo:
            btn = QPushButton(texto)
            btn.clicked.connect(func)
            lay.addWidget(btn)
        lay.addStretch()

    def _build_tab_stats(self):
        lay = QVBoxLayout(self.tab_stats)
        btn_calc = QPushButton("Calcular estadisticas")
        btn_calc.clicked.connect(self._calcular_stats)
        lay.addWidget(btn_calc)
        self.txt_stats = QTextEdit()
        self.txt_stats.setReadOnly(True)
        self.txt_stats.setFont(QFont("Consolas", 9))
        lay.addWidget(self.txt_stats, 1)

    def _build_tab_export(self):
        lay = QVBoxLayout(self.tab_export)
        botones_exp = [
            ("Exportar filtro actual", self._exportar_filtro),
            ("Exportar CURVAS", lambda: self._exportar_tipo("CURVAS", "\"GRADOS Y DIRECCION\" IS NOT NULL AND \"GRADOS Y DIRECCION\" != ''")),
            ("Exportar CODOS", lambda: self._exportar_tipo("CODOS", "upper(\"NOTAS\") LIKE '%CODO%'")),
            ("Exportar TODO", lambda: self._exportar_tipo("TODO", "")),
        ]
        for texto, func in botones_exp:
            btn = QPushButton(texto)
            btn.clicked.connect(func)
            lay.addWidget(btn)

        # Separador visual
        line = QLabel("")
        line.setFixedHeight(1)
        line.setStyleSheet("background-color: #ccc; margin: 8px 0;")
        lay.addWidget(line)

        # Boton mapa HTML
        self.btn_html_map = QPushButton("Exportar Mapa HTML (navegador)")
        self.btn_html_map.setStyleSheet(
            "QPushButton { background-color: #2196F3; color: white; font-weight: bold; padding: 8px; }"
            "QPushButton:hover { background-color: #1976D2; }"
        )
        self.btn_html_map.clicked.connect(self._exportar_mapa_html)
        lay.addWidget(self.btn_html_map)

        # Separador visual
        line2 = QLabel("")
        line2.setFixedHeight(1)
        line2.setStyleSheet("background-color: #ccc; margin: 8px 0;")
        lay.addWidget(line2)


        lay.addStretch()

    # ====================================================================
    # PROYECTO Y CONFIGURACION
    # ====================================================================
    def _cargar_proyecto(self):
        """Carga la ruta del proyecto desde QgsProject o desde la entrada del usuario."""
        proj = QgsProject.instance()
        saved = proj.readEntry("PanelCartera", "proyecto_path", "")[0]
        if saved and os.path.isdir(saved):
            self.proyecto_path = saved
            self.txt_carpeta.setText(saved)
        else:
            # Intentar detectar del proyecto QGIS
            proj_file = proj.fileName()
            if proj_file:
                d = os.path.dirname(proj_file)
                # Subir niveles hasta encontrar carpeta con 80_APLICACIONES
                for _ in range(5):
                    if os.path.isdir(os.path.join(d, '80_APLICACIONES')):
                        self.proyecto_path = d
                        self.txt_carpeta.setText(d)
                        break
                    d = os.path.dirname(d)

        self._cargar_config()

    def _cargar_config(self):
        """Busca config_proyecto.ini en la carpeta del script (80_APLICACIONES) o fallback a 03_Scripts."""
        self.config = None

        # Primero: buscar en la carpeta donde esta este script
        try:
            script_dir = os.path.dirname(os.path.abspath(__file__))
        except NameError:
            script_dir = None

        config_found = False
        if script_dir:
            cfg_path = os.path.join(script_dir, 'config_proyecto.ini')
            if os.path.exists(cfg_path):
                self.config = configparser.ConfigParser()
                self.config.read(cfg_path, encoding='utf-8')
                config_found = True

        # Fallback: buscar en proyecto_path/80_APLICACIONES
        if not config_found and self.proyecto_path:
            cfg_path = os.path.join(self.proyecto_path, '80_APLICACIONES', 'config_proyecto.ini')
            if os.path.exists(cfg_path):
                self.config = configparser.ConfigParser()
                self.config.read(cfg_path, encoding='utf-8')
                config_found = True

        # Fallback: buscar en proyecto_path/03_Scripts
        if not config_found and self.proyecto_path:
            cfg_path = os.path.join(self.proyecto_path, '03_Scripts', 'config_proyecto.ini')
            if os.path.exists(cfg_path):
                self.config = configparser.ConfigParser()
                self.config.read(cfg_path, encoding='utf-8')
                config_found = True

        if config_found:
            self._status("Config cargado")
        else:
            self._status("Config no encontrado")

    def _seleccionar_carpeta(self):
        d = QFileDialog.getExistingDirectory(self, "Carpeta raiz del proyecto")
        if d:
            self.proyecto_path = d
            self.txt_carpeta.setText(d)
            QgsProject.instance().writeEntry("PanelCartera", "proyecto_path", d)
            self._cargar_config()

    def _recargar_datos(self):
        """Ejecuta merge_cartera_gis.py y recarga capas por tramo en QGIS."""
        if not self.proyecto_path:
            QMessageBox.warning(self, "Error", "Seleccione la carpeta del proyecto primero.")
            return

        self._status("Ejecutando merge...")

        try:
            # Buscar el script merge en 80_APLICACIONES
            script_dir = os.path.join(self.proyecto_path, '80_APLICACIONES')
            merge_path = os.path.join(script_dir, 'merge_cartera_gis.py')
            if not os.path.exists(merge_path):
                merge_path = os.path.join(script_dir, 'Merge_Cartera_GIS.py')
            if not os.path.exists(merge_path):
                QMessageBox.warning(self, "Error", f"No se encontro merge_cartera_gis.py en:\n{script_dir}")
                return

            # Importar dinamicamente
            import importlib.util
            spec = importlib.util.spec_from_file_location("merge_cartera_gis", merge_path)
            mod = importlib.util.module_from_spec(spec)
            spec.loader.exec_module(mod)

            processor = mod.CarteraGISProcessor()

            # Cargar config
            config_path = os.path.join(script_dir, 'config_proyecto.ini')
            if os.path.exists(config_path):
                processor.load_config(config_path)

            # Leer rutas del config
            if not self.config:
                QMessageBox.warning(self, "Error", "No se encontro config_proyecto.ini")
                return

            csv_rel = self.config.get('ARCHIVOS', 'coordenadas_csv', fallback='')
            xl_rel = self.config.get('ARCHIVOS', 'cartera_excel', fallback='')
            geojson_rel = self.config.get('ARCHIVOS', 'geojson_salida', fallback='')
            epsg = self.config.getint('PROYECTO', 'epsg', fallback=32616)

            csv_path = os.path.join(self.proyecto_path, csv_rel) if csv_rel else ''
            xl_path = os.path.join(self.proyecto_path, xl_rel) if xl_rel else ''
            gis_folder = os.path.dirname(os.path.join(self.proyecto_path, geojson_rel)) if geojson_rel else os.path.join(self.proyecto_path, '04_PREDOBLADO', 'GIS')

            if not os.path.exists(xl_path):
                QMessageBox.warning(self, "Error", f"Excel no encontrado:\n{xl_path}")
                return
            if not os.path.exists(csv_path) and not processor.tubos_repetidos:
                QMessageBox.warning(self, "Error", f"CSV no encontrado:\n{csv_path}")
                return

            # Ejecutar merge
            if not processor.tubos_repetidos:
                processor.read_csv(csv_path)
            processor.read_excel(xl_path)
            csv_folder = os.path.dirname(csv_path) if processor.tubos_repetidos else None
            n = processor.merge_data(csv_folder=csv_folder)

            # Exportar por tramo (siempre, incluso con 0 matches - para crear tablas)
            tramo_result = processor.export_by_tramo(gis_folder, epsg)

            # Tambien exportar combinado si hay datos
            if n > 0 and geojson_rel:
                geojson_path = os.path.join(self.proyecto_path, geojson_rel)
                os.makedirs(os.path.dirname(geojson_path), exist_ok=True)
                processor.export_geojson(geojson_path, epsg)

            con_coords = sum(1 for v in tramo_result.values() if v['type'] == 'geojson')
            sin_coords = sum(1 for v in tramo_result.values() if v['type'] == 'csv')
            self._status(f"Merge: {len(tramo_result)} tramos ({con_coords} con coords, {sin_coords} tablas)")

            # Recargar capas por tramo en QGIS
            self._recargar_capas_qgis(tramo_result, epsg)

        except Exception as ex:
            import traceback
            QMessageBox.critical(self, "Error en merge", f"{ex}\n\n{traceback.format_exc()}")
            self._status(f"Error: {ex}")

    def _recargar_capas_qgis(self, tramo_result, epsg):
        """Recarga capas por tramo dentro de un grupo en QGIS."""
        proj = QgsProject.instance()
        root = proj.layerTreeRoot()
        group_name = "Cartera Predoblado"

        # Buscar grupo existente y guardar estilos
        saved_styles = {}
        group = root.findGroup(group_name)
        if group:
            for child in group.children():
                if isinstance(child, QgsLayerTreeLayer) and child.layer():
                    layer = child.layer()
                    style = QgsMapLayerStyle()
                    style.readFromLayer(layer)
                    saved_styles[layer.name()] = style
            # Remover capas del grupo del proyecto
            self.capa_activa = None
            for child in group.children():
                if isinstance(child, QgsLayerTreeLayer) and child.layer():
                    proj.removeMapLayer(child.layer().id())
            root.removeChildNode(group)

        # Crear grupo nuevo
        group = root.insertGroup(0, group_name)

        # Ordenar tramos por PK (con coordenadas primero, sin coordenadas al final)
        def _pk_sort_key(item):
            name, info = item
            # Sin coordenadas al final
            tipo_orden = 0 if info['type'] == 'geojson' else 1
            # Extraer primer numero PK del nombre (ej: "KP25+670 AL KP27+150" -> 25670)
            import re
            m = re.search(r'KP\s*(\d+)\+(\d+)', name, re.IGNORECASE)
            pk_val = int(m.group(1)) * 1000 + int(m.group(2)) if m else 999999
            return (tipo_orden, pk_val, name)

        sorted_tramos = sorted(tramo_result.items(), key=_pk_sort_key)

        loaded = 0
        for tramo_name, info in sorted_tramos:
            file_path = info['path']
            file_type = info['type']

            if file_type == 'geojson':
                layer = QgsVectorLayer(file_path, tramo_name, "ogr")
            else:
                # CSV sin geometria - cargar como tabla
                uri = "file:///{}?type=csv&delimiter={}&encoding=UTF-8-sig&geomType=none".format(
                    file_path.replace('\\', '/'), ';')
                layer = QgsVectorLayer(uri, tramo_name, "delimitedtext")

            if layer and layer.isValid():
                proj.addMapLayer(layer, False)
                group.addLayer(layer)
                # Restaurar estilo si existia
                if tramo_name in saved_styles:
                    saved_styles[tramo_name].writeToLayer(layer)
                    layer.triggerRepaint()
                # Popup personalizado
                self._set_popup_template(layer)
                loaded += 1

        # Activar Map Tips para que el popup funcione al pasar el mouse
        action = iface.actionMapTips()
        if action and not action.isChecked():
            action.setChecked(True)

        self._actualizar_combo_capas()
        # Seleccionar la primera capa del grupo
        if self.combo_capa.count() > 0:
            for i in range(self.combo_capa.count()):
                name = self.combo_capa.itemText(i)
                if name in tramo_result:
                    self.combo_capa.setCurrentIndex(i)
                    break
        self._actualizar_combo_tramos()
        self._status(f"{loaded} capas cargadas en grupo '{group_name}'")

    # ====================================================================
    # SELECTOR DE CAPA
    # ====================================================================
    def _actualizar_combo_capas(self):
        self.combo_capa.blockSignals(True)
        current = self.combo_capa.currentText()
        self.combo_capa.clear()
        for lid, layer in QgsProject.instance().mapLayers().items():
            if isinstance(layer, QgsVectorLayer):
                self.combo_capa.addItem(layer.name(), lid)
        # Restaurar seleccion
        idx = self.combo_capa.findText(current)
        if idx >= 0:
            self.combo_capa.setCurrentIndex(idx)
        self.combo_capa.blockSignals(False)

    def _cambiar_capa(self, idx):
        if idx < 0:
            self.capa_activa = None
            return
        lid = self.combo_capa.itemData(idx)
        self.capa_activa = QgsProject.instance().mapLayer(lid) if lid else None
        self._actualizar_combo_tramos()

    def _obtener_capa(self):
        """Retorna la capa activa seleccionada en el combo."""
        if self.capa_activa and self.capa_activa.isValid():
            return self.capa_activa
        # Fallback
        idx = self.combo_capa.currentIndex()
        if idx >= 0:
            lid = self.combo_capa.itemData(idx)
            layer = QgsProject.instance().mapLayer(lid)
            if layer and layer.isValid():
                self.capa_activa = layer
                return layer
        return None

    # ====================================================================
    # FILTROS
    # ====================================================================
    def _aplicar_filtro(self, expresion, nombre="Filtro"):
        layer = self._obtener_capa()
        if not layer:
            QMessageBox.warning(self, "Error", "No hay capa activa seleccionada.")
            return

        if not expresion:
            # Quitar filtro
            layer.setSubsetString("")
            count = layer.featureCount()
            self._status(f"TODOS: {count} elementos")
            return

        # Contar antes de aplicar para evitar tabla en blanco
        req = QgsFeatureRequest(QgsExpression(expresion))
        req.setFlags(QgsFeatureRequest.NoGeometry)
        count = 0
        for _ in layer.getFeatures(req):
            count += 1

        if count == 0:
            self._status(f"{nombre}: 0 elementos (filtro no aplicado)")
            QMessageBox.information(self, nombre, f"No se encontraron elementos con filtro:\n{nombre}")
            return

        layer.setSubsetString(expresion)
        self._status(f"{nombre}: {count} elementos")

    def _filtrar_por_tramo(self, idx):
        if idx <= 0:
            # "TODOS LOS TRAMOS" o nada
            self._aplicar_filtro("", "TODOS")
            return
        tramo = self.combo_tramo.currentText()
        expr = f"\"TRAMO\" = '{tramo}'"
        self._aplicar_filtro(expr, f"Tramo: {tramo}")

    def _actualizar_combo_tramos(self):
        """Actualiza el combo de tramos con los valores unicos del campo TRAMO."""
        self.combo_tramo.blockSignals(True)
        self.combo_tramo.clear()
        self.combo_tramo.addItem("TODOS LOS TRAMOS")
        layer = self._obtener_capa()
        if layer and layer.isValid():
            field_idx = layer.fields().indexOf('TRAMO')
            if field_idx >= 0:
                tramos = set()
                for feat in layer.getFeatures():
                    val = feat.attribute('TRAMO')
                    if val and str(val).strip():
                        tramos.add(str(val).strip())
                for t in sorted(tramos):
                    self.combo_tramo.addItem(t)
        self.combo_tramo.blockSignals(False)

    def _filtrar_rango_tubos(self):
        desde = self.spin_tubo_desde.value()
        hasta = self.spin_tubo_hasta.value()
        expr = f"to_int(\"N. TUBO\") >= {desde} AND to_int(\"N. TUBO\") <= {hasta}"
        self._aplicar_filtro(expr, f"Tubos {desde}-{hasta}")

    def _filtrar_rango_pk(self):
        pk_desde = self.txt_pk_desde.text().strip()
        pk_hasta = self.txt_pk_hasta.text().strip()
        if not pk_desde and not pk_hasta:
            self._aplicar_filtro("", "TODOS")
            return
        expr_parts = []
        if pk_desde:
            expr_parts.append(f"\"PK/JUNTA\" >= '{pk_desde}'")
        if pk_hasta:
            expr_parts.append(f"\"PK/JUNTA\" <= '{pk_hasta}'")
        expr = " AND ".join(expr_parts)
        self._aplicar_filtro(expr, f"PK {pk_desde or '*'} a {pk_hasta or '*'}")

    def _buscar_texto(self):
        texto = self.txt_buscar.text().strip()
        if not texto:
            self._aplicar_filtro("", "TODOS")
            return
        campos = ['"N. TUBO"', '"SERIAL NUMERO"', '"NOTAS"', '"PK/JUNTA"',
                  '"GRADOS Y DIRECCION"', '"CENTRO DE LA CURVA"', '"TRAMO"']
        parts = [f'{c} ILIKE \'%{texto}%\'' for c in campos]
        expr = " OR ".join(parts)
        self._aplicar_filtro(expr, f"Buscar: {texto}")

    # ====================================================================
    # ESTILOS
    # ====================================================================
    def _estilo_colorear_tipo(self):
        layer = self._obtener_capa()
        if not layer:
            return

        root = QgsRuleBasedRenderer.Rule(QgsSymbol.defaultSymbol(layer.geometryType()))

        reglas = [
            ("INICIO TRAMO", "upper(\"NOTAS\") LIKE '%INICIO%' OR upper(\"CENTRO DE LA CURVA\") LIKE '%INICIO%' OR upper(\"PK/JUNTA\") LIKE '%INICIO%' OR upper(\"GRADOS Y DIRECCION\") LIKE '%INICIO%'", "#00cc00", "circle", 5.5, '1.0'),
            ("FIN TRAMO", "upper(\"NOTAS\") LIKE '%FIN%' OR upper(\"CENTRO DE LA CURVA\") LIKE '%FIN%' OR upper(\"PK/JUNTA\") LIKE '%FIN%' OR upper(\"GRADOS Y DIRECCION\") LIKE '%FIN%'", "#cc0000", "circle", 5.5, '1.0'),
            ("Solape", "upper(\"NOTAS\") LIKE '%SOLAPE%' OR upper(\"NOTAS\") LIKE '%TRASLAPE%'", "#00cccc", "star", 5.0, '0.4'),
            ("Codos", "upper(\"NOTAS\") LIKE '%CODO%'", "#8b0000", "circle", 5.0, '0.4'),
            ("Niples", "upper(\"NOTAS\") LIKE '%NIPLE%'", "#800080", "triangle", 4.5, '0.4'),
            ("Perforados", "upper(\"NOTAS\") LIKE '%PERFORADO%'", "#ff8c00", "triangle", 4.5, '0.4'),
            ("Seccion Movil", "upper(\"NOTAS\") LIKE '%SECCION%'", "#1E90FF", "circle", 5.0, '0.5'),
            ("Camino", "upper(\"NOTAS\") LIKE '%CAMINO%'", "#D2691E", "circle", 5.0, '0.5'),
            ("Curvas", "\"GRADOS Y DIRECCION\" IS NOT NULL AND \"GRADOS Y DIRECCION\" != ''", "#ffbf00", "circle", 5.0, '0.4'),
            ("Rectos", "ELSE", "#999999", "circle", 3.5, '0.3'),
        ]

        for nombre, expr, color, forma, tam, borde in reglas:
            sym = QgsMarkerSymbol.createSimple({
                'name': forma, 'color': color,
                'size': str(tam), 'outline_color': '#333333', 'outline_width': borde
            })
            rule = QgsRuleBasedRenderer.Rule(sym)
            rule.setLabel(nombre)
            if expr == "ELSE":
                try:
                    rule.setIsElse(True)
                except AttributeError:
                    rule.setFilterExpression("TRUE")
            else:
                rule.setFilterExpression(expr)
            root.appendChild(rule)

        renderer = QgsRuleBasedRenderer(root)
        layer.setRenderer(renderer)
        layer.triggerRepaint()
        iface.layerTreeView().refreshLayerSymbology(layer.id())
        self._status("Estilo: Colorear por tipo")

    def _etiqueta_ntubo(self):
        layer = self._obtener_capa()
        if not layer:
            return
        settings = QgsPalLayerSettings()
        settings.fieldName = '"N. TUBO"'
        settings.isExpression = True
        txt_format = QgsTextFormat()
        txt_format.setSize(8)
        txt_format.setColor(QColor('#333333'))
        buf = QgsTextBufferSettings()
        buf.setEnabled(True)
        buf.setSize(1)
        buf.setColor(QColor('#ffffff'))
        txt_format.setBuffer(buf)
        settings.setFormat(txt_format)
        # Offset Y para no tapar el punto
        try:
            settings.placement = Qgis.LabelPlacement.OverPoint
        except AttributeError:
            pass
        settings.yOffset = -2.5
        layer.setLabelsEnabled(True)
        layer.setLabeling(QgsVectorLayerSimpleLabeling(settings))
        layer.triggerRepaint()
        self._status("Etiquetas: N. TUBO")

    def _etiqueta_inicio_fin(self):
        layer = self._obtener_capa()
        if not layer:
            return

        root = QgsRuleBasedLabeling.Rule(QgsPalLayerSettings())

        # INICIO TRAMO
        s_inicio = QgsPalLayerSettings()
        s_inicio.fieldName = "concat('T-', \"N. TUBO\", ' INICIO')"
        s_inicio.isExpression = True
        fmt_inicio = QgsTextFormat()
        fmt_inicio.setSize(9)
        fmt_inicio.setColor(QColor('#006600'))
        buf_i = QgsTextBufferSettings()
        buf_i.setEnabled(True); buf_i.setSize(1); buf_i.setColor(QColor('#ffffff'))
        fmt_inicio.setBuffer(buf_i)
        s_inicio.setFormat(fmt_inicio)
        rule_inicio = QgsRuleBasedLabeling.Rule(s_inicio)
        rule_inicio.setFilterExpression("upper(\"NOTAS\") LIKE '%INICIO%'")
        rule_inicio.setDescription("INICIO TRAMO")
        root.appendChild(rule_inicio)

        # FIN TRAMO
        s_fin = QgsPalLayerSettings()
        s_fin.fieldName = "concat('T-', \"N. TUBO\", ' FIN')"
        s_fin.isExpression = True
        fmt_fin = QgsTextFormat()
        fmt_fin.setSize(9)
        fmt_fin.setColor(QColor('#cc0000'))
        buf_f = QgsTextBufferSettings()
        buf_f.setEnabled(True); buf_f.setSize(1); buf_f.setColor(QColor('#ffffff'))
        fmt_fin.setBuffer(buf_f)
        s_fin.setFormat(fmt_fin)
        rule_fin = QgsRuleBasedLabeling.Rule(s_fin)
        rule_fin.setFilterExpression("upper(\"NOTAS\") LIKE '%FIN%'")
        rule_fin.setDescription("FIN TRAMO")
        root.appendChild(rule_fin)

        labeling = QgsRuleBasedLabeling(root)
        layer.setLabeling(labeling)
        layer.setLabelsEnabled(True)
        layer.triggerRepaint()
        self._status("Etiquetas: INICIO/FIN")

    def _etiqueta_pk(self):
        layer = self._obtener_capa()
        if not layer:
            return
        settings = QgsPalLayerSettings()
        settings.fieldName = '"PK/JUNTA"'
        settings.isExpression = True
        txt_format = QgsTextFormat()
        txt_format.setSize(8)
        txt_format.setColor(QColor('#00FFFF'))
        buf = QgsTextBufferSettings()
        buf.setEnabled(True)
        buf.setSize(1.5)
        buf.setColor(QColor('#000000'))
        txt_format.setBuffer(buf)
        settings.setFormat(txt_format)
        layer.setLabelsEnabled(True)
        layer.setLabeling(QgsVectorLayerSimpleLabeling(settings))
        layer.triggerRepaint()
        self._status("Etiquetas: PK/JUNTA")

    def _etiqueta_grados(self):
        layer = self._obtener_capa()
        if not layer:
            return
        settings = QgsPalLayerSettings()
        settings.fieldName = '"GRADOS Y DIRECCION"'
        settings.isExpression = True
        txt_format = QgsTextFormat()
        txt_format.setSize(8)
        txt_format.setColor(QColor('#ffffff'))
        buf = QgsTextBufferSettings()
        buf.setEnabled(True)
        buf.setSize(1.5)
        buf.setColor(QColor('#cc6600'))
        txt_format.setBuffer(buf)
        settings.setFormat(txt_format)
        layer.setLabelsEnabled(True)
        layer.setLabeling(QgsVectorLayerSimpleLabeling(settings))
        layer.triggerRepaint()
        self._status("Etiquetas: GRADOS Y DIRECCION")

    def _quitar_etiquetas(self):
        layer = self._obtener_capa()
        if not layer:
            return
        layer.setLabelsEnabled(False)
        layer.triggerRepaint()
        self._status("Etiquetas removidas")

    def _restablecer_estilo(self):
        layer = self._obtener_capa()
        if not layer:
            return
        layer.setSubsetString("")
        layer.loadDefaultStyle()
        layer.triggerRepaint()
        iface.layerTreeView().refreshLayerSymbology(layer.id())
        self._status("Estilo restablecido")

    # ====================================================================
    # POPUP (MAP TIP)
    # ====================================================================
    def _set_popup_template(self, layer):
        """Configura popup estilo HTML (Map Tip) para la capa de puntos."""
        if not layer or not layer.isSpatial():
            return
        html = (
            '<table style="font-family:Arial; font-size:12px; border-collapse:collapse;">'
            '<tr><td style="color:#666; padding:2px 8px 2px 0;">N. TUBO</td>'
            '<td style="font-weight:bold;">[% "N. TUBO" %]</td></tr>'
            '<tr><td style="color:#666; padding:2px 8px 2px 0;">Tramo</td>'
            '<td>[% "TRAMO" %]</td></tr>'
            '<tr><td style="color:#666; padding:2px 8px 2px 0;">PK/Junta</td>'
            '<td>[% "PK/JUNTA" %]</td></tr>'
            '<tr><td style="color:#666; padding:2px 8px 2px 0;">Serial</td>'
            '<td>[% "SERIAL NUMERO" %]</td></tr>'
            '<tr><td style="color:#666; padding:2px 8px 2px 0;">Longitud</td>'
            '<td>[% "LONGITUD" %] m</td></tr>'
            '<tr><td style="color:#666; padding:2px 8px 2px 0;">Espesor</td>'
            '<td>[% "ESP. (mm)" %] mm</td></tr>'
            '<tr><td style="color:#666; padding:2px 8px 2px 0;">Grados</td>'
            '<td style="font-weight:bold; color:#cc6600;">[% "GRADOS Y DIRECCION" %]</td></tr>'
            '<tr><td style="color:#666; padding:2px 8px 2px 0;">Centro curva</td>'
            '<td>[% "CENTRO DE LA CURVA" %]</td></tr>'
            '<tr><td style="color:#666; padding:2px 8px 2px 0;">Notas</td>'
            '<td style="font-weight:bold; color:#cc6600;">[% "NOTAS" %]</td></tr>'
            '</table>'
        )
        layer.setMapTipTemplate(html)

    # ====================================================================
    # ESTADISTICAS
    # ====================================================================
    def _calcular_stats(self):
        layer = self._obtener_capa()
        if not layer:
            return

        self.txt_stats.clear()
        feats = list(layer.getFeatures())
        total = len(feats)
        if total == 0:
            self.txt_stats.append("Sin datos")
            return

        rectos = 0; curvas = 0; codos = 0; niples = 0; perforados = 0
        solapes = 0; inicios = 0; fines = 0
        longitudes = []; curva_grados = []; espesores = set(); seriales = set()
        pares_fin_inicio = []
        prev_fin = None

        # Datos por tramo
        tramo_stats = {}

        for f in feats:
            notas = str(f.attribute('NOTAS') or '').upper()
            grados = str(f.attribute('GRADOS Y DIRECCION') or '').strip()
            long_val = f.attribute('LONGITUD')
            esp_val = f.attribute('ESP. (mm)')
            serial = str(f.attribute('SERIAL NUMERO') or '').strip()
            tramo = str(f.attribute('TRAMO') or '').strip()

            # Stats por tramo
            if tramo:
                if tramo not in tramo_stats:
                    tramo_stats[tramo] = {'total': 0, 'rectos': 0, 'curvas': 0, 'codos': 0}
                tramo_stats[tramo]['total'] += 1

            is_codo = 'CODO' in notas
            is_niple = 'NIPLE' in notas
            is_perforado = 'PERFORADO' in notas
            is_inicio = 'INICIO' in notas
            is_fin = 'FIN' in notas
            is_solape = 'SOLAPE' in notas or 'TRASLAPE' in notas
            has_curva = bool(grados)

            if is_codo:
                codos += 1
                if tramo and tramo in tramo_stats:
                    tramo_stats[tramo]['codos'] += 1
            elif is_niple:
                niples += 1
            elif is_perforado:
                perforados += 1
            elif is_solape:
                solapes += 1
            elif is_inicio:
                inicios += 1
                if prev_fin:
                    pares_fin_inicio.append((prev_fin, str(f.attribute('N. TUBO') or '')))
                prev_fin = None
            elif is_fin:
                fines += 1
                prev_fin = str(f.attribute('N. TUBO') or '')
            elif has_curva:
                curvas += 1
                if tramo and tramo in tramo_stats:
                    tramo_stats[tramo]['curvas'] += 1
                try:
                    g = float(grados.split()[0].replace('°','').replace(',','.'))
                    curva_grados.append(g)
                except (ValueError, IndexError):
                    pass
            else:
                rectos += 1
                if tramo and tramo in tramo_stats:
                    tramo_stats[tramo]['rectos'] += 1

            if long_val is not None:
                try:
                    longitudes.append(float(long_val))
                except (ValueError, TypeError):
                    pass
            if esp_val is not None:
                try:
                    espesores.add(float(esp_val))
                except (ValueError, TypeError):
                    pass
            if serial:
                seriales.add(serial)

        especiales = max(inicios, fines)
        s = []
        s.append(f"ESTADISTICAS DE LA CARTERA")
        s.append(f"{'=' * 40}")
        s.append(f"Total elementos:  {total}")
        s.append(f"")
        s.append(f"Rectos:           {rectos}")
        s.append(f"Curvas:           {curvas}")
        s.append(f"Codos:            {codos}")
        s.append(f"Niples:           {niples}")
        s.append(f"Perforados:       {perforados}")
        s.append(f"Solapes:          {solapes}")
        s.append(f"Especiales (I/F): {especiales} ({inicios} inicio + {fines} fin)")
        s.append(f"")

        if pares_fin_inicio:
            s.append(f"PARES FIN -> INICIO:")
            for fin_t, ini_t in pares_fin_inicio:
                s.append(f"  T-{fin_t} (FIN) -> T-{ini_t} (INICIO)")
            s.append(f"")

        if longitudes:
            s.append(f"LONGITUDES:")
            s.append(f"  Total:    {sum(longitudes):.2f} m")
            s.append(f"  Minima:   {min(longitudes):.2f} m")
            s.append(f"  Maxima:   {max(longitudes):.2f} m")
            s.append(f"  Promedio: {sum(longitudes)/len(longitudes):.2f} m")
            s.append(f"")

        if curva_grados:
            s.append(f"CURVAS (grados):")
            s.append(f"  Minima:   {min(curva_grados):.2f}")
            s.append(f"  Maxima:   {max(curva_grados):.2f}")
            s.append(f"  Promedio: {sum(curva_grados)/len(curva_grados):.2f}")
            s.append(f"")

        if espesores:
            s.append(f"ESPESORES: {', '.join(str(e) for e in sorted(espesores))} mm")
            s.append(f"")

        if seriales:
            s.append(f"SERIALES unicos: {len(seriales)}")
            s.append(f"")

        # Desglose por tramo
        if tramo_stats:
            s.append(f"{'=' * 40}")
            s.append(f"DESGLOSE POR TRAMO:")
            s.append(f"{'=' * 40}")
            for tramo_name in sorted(tramo_stats.keys()):
                ts = tramo_stats[tramo_name]
                s.append(f"  {tramo_name}:")
                s.append(f"    {ts['total']} tubos ({ts['rectos']} rectos, {ts['curvas']} curvas, {ts['codos']} codos)")

        self.txt_stats.setPlainText('\n'.join(s))
        self._status(f"Estadisticas: {total} elementos")

    # ====================================================================
    # EXPORTAR
    # ====================================================================
    def _exportar_filtro(self):
        layer = self._obtener_capa()
        if not layer:
            return
        filtro = layer.subsetString()
        nombre = "filtro_actual"
        self._exportar_csv(layer, nombre)

    def _exportar_tipo(self, nombre, expresion):
        layer = self._obtener_capa()
        if not layer:
            return
        # Guardar filtro actual
        old_filter = layer.subsetString()
        if expresion:
            layer.setSubsetString(expresion)
        self._exportar_csv(layer, nombre)
        # Restaurar filtro
        layer.setSubsetString(old_filter)

    def _exportar_mapa_html(self):
        """Ejecuta merge y genera mapa HTML interactivo con Leaflet.js."""
        if not self.proyecto_path:
            QMessageBox.warning(self, "Error", "Seleccione la carpeta del proyecto primero.")
            return

        self._status("Generando mapa HTML...")

        try:
            # Importar merge_cartera_gis
            script_dir = os.path.join(self.proyecto_path, '80_APLICACIONES')
            merge_path = os.path.join(script_dir, 'merge_cartera_gis.py')
            if not os.path.exists(merge_path):
                merge_path = os.path.join(script_dir, 'Merge_Cartera_GIS.py')
            if not os.path.exists(merge_path):
                QMessageBox.warning(self, "Error", f"No se encontro merge_cartera_gis.py en:\n{script_dir}")
                return

            import importlib.util
            spec = importlib.util.spec_from_file_location("merge_cartera_gis", merge_path)
            mod = importlib.util.module_from_spec(spec)
            spec.loader.exec_module(mod)

            processor = mod.CarteraGISProcessor()

            # Cargar config
            config_path = os.path.join(script_dir, 'config_proyecto.ini')
            if os.path.exists(config_path):
                processor.load_config(config_path)

            if not self.config:
                QMessageBox.warning(self, "Error", "No se encontro config_proyecto.ini")
                return

            csv_rel = self.config.get('ARCHIVOS', 'coordenadas_csv', fallback='')
            xl_rel = self.config.get('ARCHIVOS', 'cartera_excel', fallback='')
            geojson_rel = self.config.get('ARCHIVOS', 'geojson_salida', fallback='')
            zona_utm = self.config.getint('PROYECTO', 'zona_utm', fallback=16)

            csv_path = os.path.join(self.proyecto_path, csv_rel) if csv_rel else ''
            xl_path = os.path.join(self.proyecto_path, xl_rel) if xl_rel else ''
            gis_folder = os.path.dirname(os.path.join(self.proyecto_path, geojson_rel)) if geojson_rel else os.path.join(self.proyecto_path, '04_PREDOBLADO', 'GIS')

            if not os.path.exists(xl_path):
                QMessageBox.warning(self, "Error", f"Excel no encontrado:\n{xl_path}")
                return
            if not os.path.exists(csv_path) and not processor.tubos_repetidos:
                QMessageBox.warning(self, "Error", f"CSV no encontrado:\n{csv_path}")
                return

            # Ejecutar merge
            if not processor.tubos_repetidos:
                processor.read_csv(csv_path)
            processor.read_excel(xl_path)
            csv_folder = os.path.dirname(csv_path) if processor.tubos_repetidos else None
            processor.merge_data(csv_folder=csv_folder)

            # Generar mapa HTML
            os.makedirs(gis_folder, exist_ok=True)
            html_path = os.path.join(gis_folder, 'mapa_cartera_predoblado.html')
            processor.export_html_map(html_path, zona_utm)

            self._status(f"Mapa HTML: {os.path.basename(html_path)}")

            # Abrir en navegador
            import webbrowser
            webbrowser.open(html_path)

            QMessageBox.information(self, "Mapa HTML",
                f"Mapa generado:\n{html_path}\n\nSe abrio en el navegador.")

        except Exception as ex:
            import traceback
            QMessageBox.critical(self, "Error", f"{ex}\n\n{traceback.format_exc()}")
            self._status(f"Error: {ex}")

    def _exportar_csv(self, layer, nombre_base):
        # Default: carpeta 11_REPORTES del proyecto
        default_dir = ''
        if self.proyecto_path:
            carpeta_exp = '11_REPORTES'
            if self.config:
                carpeta_exp = self.config.get('ARCHIVOS', 'carpeta_exportados', fallback='11_REPORTES')
            default_dir = os.path.join(self.proyecto_path, carpeta_exp)
            os.makedirs(default_dir, exist_ok=True)

        timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
        default_name = f"cartera_{nombre_base}_{timestamp}.csv"
        default_path = os.path.join(default_dir, default_name) if default_dir else default_name

        path, _ = QFileDialog.getSaveFileName(self, "Exportar CSV", default_path,
                                               "CSV (*.csv);;Todos (*.*)")
        if not path:
            return

        campos = ['N. TUBO', 'LONGITUD', 'SERIAL NUMERO', 'ESP. (mm)',
                  'GRADOS Y DIRECCION', 'CENTRO DE LA CURVA', 'NOTAS', 'PK/JUNTA',
                  'TRAMO', 'ESTE', 'NORTE', 'COTA']

        count = 0
        with open(path, 'w', newline='', encoding='utf-8-sig') as f:
            writer = csv.writer(f, delimiter=';')
            writer.writerow(campos)
            for feat in layer.getFeatures():
                row = [str(feat.attribute(c) or '') for c in campos]
                writer.writerow(row)
                count += 1

        self._status(f"Exportado: {count} registros -> {os.path.basename(path)}")
        QMessageBox.information(self, "Exportado", f"{count} registros exportados a:\n{path}")


    # ====================================================================
    # UTILIDADES
    # ====================================================================
    def _zoom_a_elementos(self):
        layer = self._obtener_capa()
        if not layer:
            return
        if layer.featureCount() == 0:
            self._status("Sin elementos para zoom")
            return
        extent = layer.extent()
        if extent.isEmpty():
            return
        extent.grow(extent.width() * 0.1 or 100)
        iface.mapCanvas().setExtent(extent)
        iface.mapCanvas().refresh()
        self._status(f"Zoom: {layer.featureCount()} elementos")

    def _status(self, msg):
        self.lbl_status.setText(msg)


# ============================================================================
# REGISTRO DEL PANEL (para cargar desde la consola de QGIS)
# ============================================================================
def abrir_panel():
    """Abre el panel de Cartera Predoblado en QGIS."""
    panel = PanelCartera(iface.mainWindow())
    iface.addDockWidget(Qt.RightDockWidgetArea, panel)
    return panel
