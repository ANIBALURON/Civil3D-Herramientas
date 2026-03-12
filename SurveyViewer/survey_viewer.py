"""
Survey Database Viewer - Civil 3D
Aplicacion profesional para visualizar bases de datos SQLite de levantamiento
Autor: Anibal
"""

import tkinter as tk
from tkinter import ttk, filedialog, messagebox
import sqlite3
import csv
import os
import json

# ============================================================
# TEMA Y COLORES
# ============================================================
COLORS = {
    "bg_dark": "#1a1a2e",
    "bg_medium": "#16213e",
    "bg_light": "#0f3460",
    "accent": "#e94560",
    "accent_hover": "#ff6b81",
    "text_white": "#ffffff",
    "text_gray": "#a0a0b0",
    "text_light": "#d0d0e0",
    "table_bg": "#0d1b2a",
    "table_fg": "#e0e0e0",
    "table_selected": "#e94560",
    "table_stripe": "#1b2838",
    "border": "#2a2a4a",
    "success": "#2ecc71",
    "warning": "#f39c12",
    "button_bg": "#e94560",
    "button_hover": "#ff6b81",
    "button_green": "#27ae60",
    "button_green_hover": "#2ecc71",
    "button_blue": "#2980b9",
    "button_blue_hover": "#3498db",
    "entry_bg": "#1b2838",
    "header_bg": "#0f3460",
    "stat_bg": "#0d1b2a",
}

# Columnas para exportar puntos (PUNTO, ESTE, NORTE, ELEVACION, DESCRIPCION)
POINT_EXPORT_COLS = ["Number", "Easting", "Northing", "Elevation", "Description"]
POINT_EXPORT_HEADERS = ["PUNTO", "ESTE (X)", "NORTE (Y)", "ELEVACION (Z)", "DESCRIPCION"]

CONFIG_FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "config.json")


class SurveyViewer(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title("Survey Database Viewer - Civil 3D")
        self.geometry("1350x800")
        self.minsize(1050, 650)
        self.configure(bg=COLORS["bg_dark"])
        self.db_path = None
        self.conn = None
        self.current_table = None
        self.all_rows = []
        self.columns = []
        self.sort_column = None
        self.sort_reverse = False
        self.desc_filter = "TODAS"

        try:
            self.iconbitmap(default="")
        except:
            pass

        self._load_config()
        self._setup_styles()
        self._build_ui()
        self._setup_context_menu()
        self._center_window()

        # Abrir ultimo archivo si existe
        if self.last_file and os.path.exists(self.last_file):
            self.after(300, lambda: self._open_database(self.last_file))

    # --------------------------------------------------------
    # CONFIG (archivo reciente)
    # --------------------------------------------------------
    def _load_config(self):
        self.last_file = None
        try:
            if os.path.exists(CONFIG_FILE):
                with open(CONFIG_FILE, "r") as f:
                    cfg = json.load(f)
                    self.last_file = cfg.get("last_file")
        except:
            pass

    def _save_config(self):
        try:
            with open(CONFIG_FILE, "w") as f:
                json.dump({"last_file": self.db_path}, f)
        except:
            pass

    # --------------------------------------------------------
    # ESTILOS
    # --------------------------------------------------------
    def _setup_styles(self):
        self.style = ttk.Style()
        self.style.theme_use("clam")

        self.style.configure("Custom.Treeview",
                             background=COLORS["table_bg"],
                             foreground=COLORS["table_fg"],
                             fieldbackground=COLORS["table_bg"],
                             borderwidth=0,
                             font=("Segoe UI", 10),
                             rowheight=28)
        self.style.configure("Custom.Treeview.Heading",
                             background=COLORS["header_bg"],
                             foreground=COLORS["text_white"],
                             font=("Segoe UI", 10, "bold"),
                             borderwidth=1,
                             relief="flat")
        self.style.map("Custom.Treeview",
                       background=[("selected", COLORS["table_selected"])],
                       foreground=[("selected", COLORS["text_white"])])
        self.style.map("Custom.Treeview.Heading",
                       background=[("active", COLORS["accent"])])

        self.style.configure("Custom.Vertical.TScrollbar",
                             background=COLORS["bg_light"],
                             troughcolor=COLORS["bg_dark"],
                             borderwidth=0, arrowsize=14)
        self.style.configure("Custom.Horizontal.TScrollbar",
                             background=COLORS["bg_light"],
                             troughcolor=COLORS["bg_dark"],
                             borderwidth=0, arrowsize=14)

        self.style.configure("Custom.TCombobox",
                             fieldbackground=COLORS["entry_bg"],
                             background=COLORS["bg_light"],
                             foreground=COLORS["text_white"],
                             arrowcolor=COLORS["accent"],
                             font=("Segoe UI", 11))
        self.style.map("Custom.TCombobox",
                       fieldbackground=[("readonly", COLORS["entry_bg"])],
                       foreground=[("readonly", COLORS["text_white"])])

    # --------------------------------------------------------
    # INTERFAZ
    # --------------------------------------------------------
    def _build_ui(self):
        # === HEADER ===
        header = tk.Frame(self, bg=COLORS["bg_medium"], height=70)
        header.pack(fill="x", side="top")
        header.pack_propagate(False)

        title_frame = tk.Frame(header, bg=COLORS["bg_medium"])
        title_frame.pack(side="left", padx=20, pady=10)

        tk.Label(title_frame, text="SURVEY DATABASE VIEWER",
                 font=("Segoe UI", 18, "bold"),
                 fg=COLORS["text_white"], bg=COLORS["bg_medium"]).pack(anchor="w")
        tk.Label(title_frame, text="Civil 3D - Visualizador de Levantamiento",
                 font=("Segoe UI", 10),
                 fg=COLORS["text_gray"], bg=COLORS["bg_medium"]).pack(anchor="w")

        btn_frame = tk.Frame(header, bg=COLORS["bg_medium"])
        btn_frame.pack(side="right", padx=20)
        self.btn_open = self._create_button(btn_frame, "  ABRIR BASE DE DATOS  ",
                                            self._open_database, icon="📂")
        self.btn_open.pack(side="right")

        # === TOOLBAR ===
        toolbar = tk.Frame(self, bg=COLORS["bg_dark"], height=55)
        toolbar.pack(fill="x", side="top", padx=15, pady=(10, 0))
        toolbar.pack_propagate(False)

        self.lbl_file = tk.Label(toolbar, text="Ningún archivo cargado",
                                 font=("Segoe UI", 10),
                                 fg=COLORS["text_gray"], bg=COLORS["bg_dark"])
        self.lbl_file.pack(side="left", padx=5)

        tk.Frame(toolbar, bg=COLORS["border"], width=2).pack(side="left", fill="y", padx=15, pady=5)

        tk.Label(toolbar, text="TABLA:",
                 font=("Segoe UI", 10, "bold"),
                 fg=COLORS["text_gray"], bg=COLORS["bg_dark"]).pack(side="left", padx=(5, 8))

        self.combo_tables = ttk.Combobox(toolbar, state="readonly",
                                         style="Custom.TCombobox", width=18)
        self.combo_tables.pack(side="left")
        self.combo_tables.bind("<<ComboboxSelected>>", self._on_table_select)

        tk.Frame(toolbar, bg=COLORS["border"], width=2).pack(side="left", fill="y", padx=15, pady=5)

        # Filtro por descripcion
        tk.Label(toolbar, text="FILTRO:",
                 font=("Segoe UI", 10, "bold"),
                 fg=COLORS["text_gray"], bg=COLORS["bg_dark"]).pack(side="left", padx=(5, 8))

        self.combo_desc = ttk.Combobox(toolbar, state="readonly",
                                        style="Custom.TCombobox", width=14)
        self.combo_desc.set("TODAS")
        self.combo_desc.pack(side="left")
        self.combo_desc.bind("<<ComboboxSelected>>", self._on_desc_filter)

        tk.Frame(toolbar, bg=COLORS["border"], width=2).pack(side="left", fill="y", padx=15, pady=5)

        # Buscar
        tk.Label(toolbar, text="🔍",
                 font=("Segoe UI", 12),
                 fg=COLORS["text_gray"], bg=COLORS["bg_dark"]).pack(side="left", padx=(5, 5))

        self.search_var = tk.StringVar()
        self.search_var.trace_add("write", self._on_search)
        self.entry_search = tk.Entry(toolbar, textvariable=self.search_var,
                                     font=("Segoe UI", 11),
                                     bg=COLORS["entry_bg"], fg=COLORS["text_white"],
                                     insertbackground=COLORS["text_white"],
                                     relief="flat", bd=5, width=20)
        self.entry_search.pack(side="left", padx=5)

        self.btn_clear = self._create_button(toolbar, " ✕ ", self._clear_search, small=True)
        self.btn_clear.pack(side="left", padx=2)

        # Botones (derecha a izquierda)
        self.btn_delete_group = self._create_button(toolbar, " ELIMINAR GRUPO ",
                                                     self._delete_import_group,
                                                     icon="🗑", color="red")
        self.btn_delete_group.pack(side="right", padx=5)

        self.btn_import = self._create_button(toolbar, " IMPORTAR ",
                                              self._import_points_csv,
                                              icon="📥", color="orange")
        self.btn_import.pack(side="right", padx=5)

        self.btn_export_points = self._create_button(toolbar, " EXPORTAR PUNTOS ",
                                                      self._export_points_csv,
                                                      icon="📍", color="green")
        self.btn_export_points.pack(side="right", padx=5)

        self.btn_export_csv = self._create_button(toolbar, " EXPORTAR CSV ",
                                                   self._export_csv, icon="📊")
        self.btn_export_csv.pack(side="right", padx=5)

        # === CONTENIDO PRINCIPAL ===
        main_frame = tk.Frame(self, bg=COLORS["bg_dark"])
        main_frame.pack(fill="both", expand=True, padx=15, pady=10)

        # Panel izquierdo
        left_panel = tk.Frame(main_frame, bg=COLORS["bg_medium"], width=220)
        left_panel.pack(side="left", fill="y", padx=(0, 10))
        left_panel.pack_propagate(False)

        tk.Label(left_panel, text="TABLAS",
                 font=("Segoe UI", 11, "bold"),
                 fg=COLORS["accent"], bg=COLORS["bg_medium"]).pack(pady=(15, 10), padx=10, anchor="w")

        self.table_listbox = tk.Listbox(left_panel,
                                         font=("Segoe UI", 10),
                                         bg=COLORS["bg_medium"],
                                         fg=COLORS["text_light"],
                                         selectbackground=COLORS["accent"],
                                         selectforeground=COLORS["text_white"],
                                         relief="flat", bd=0,
                                         activestyle="none",
                                         highlightthickness=0)
        self.table_listbox.pack(fill="both", expand=True, padx=10, pady=(0, 5))
        self.table_listbox.bind("<<ListboxSelect>>", self._on_listbox_select)

        # Info panel
        self.info_frame = tk.Frame(left_panel, bg=COLORS["stat_bg"])
        self.info_frame.pack(fill="x", padx=10, pady=(0, 5))

        tk.Label(self.info_frame, text="INFORMACIÓN",
                 font=("Segoe UI", 9, "bold"),
                 fg=COLORS["accent"], bg=COLORS["stat_bg"]).pack(pady=(8, 5), anchor="w", padx=8)

        self.lbl_rows = tk.Label(self.info_frame, text="Filas: -",
                                  font=("Segoe UI", 9),
                                  fg=COLORS["text_gray"], bg=COLORS["stat_bg"])
        self.lbl_rows.pack(anchor="w", padx=8)

        self.lbl_cols = tk.Label(self.info_frame, text="Columnas: -",
                                  font=("Segoe UI", 9),
                                  fg=COLORS["text_gray"], bg=COLORS["stat_bg"])
        self.lbl_cols.pack(anchor="w", padx=8, pady=(0, 8))

        # Estadisticas panel
        self.stats_frame = tk.Frame(left_panel, bg=COLORS["stat_bg"])
        self.stats_frame.pack(fill="x", padx=10, pady=(0, 5))

        tk.Label(self.stats_frame, text="ESTADÍSTICAS",
                 font=("Segoe UI", 9, "bold"),
                 fg=COLORS["accent"], bg=COLORS["stat_bg"]).pack(pady=(8, 5), anchor="w", padx=8)

        self.lbl_stat_elev = tk.Label(self.stats_frame, text="Elevación: -",
                                       font=("Segoe UI", 9),
                                       fg=COLORS["text_gray"], bg=COLORS["stat_bg"])
        self.lbl_stat_elev.pack(anchor="w", padx=8)

        self.lbl_stat_north = tk.Label(self.stats_frame, text="Norte: -",
                                        font=("Segoe UI", 9),
                                        fg=COLORS["text_gray"], bg=COLORS["stat_bg"])
        self.lbl_stat_north.pack(anchor="w", padx=8)

        self.lbl_stat_east = tk.Label(self.stats_frame, text="Este: -",
                                       font=("Segoe UI", 9),
                                       fg=COLORS["text_gray"], bg=COLORS["stat_bg"])
        self.lbl_stat_east.pack(anchor="w", padx=8, pady=(0, 8))

        # Resumen descripciones
        self.desc_frame = tk.Frame(left_panel, bg=COLORS["stat_bg"])
        self.desc_frame.pack(fill="both", expand=False, padx=10, pady=(0, 10))

        tk.Label(self.desc_frame, text="RESUMEN POR TIPO",
                 font=("Segoe UI", 9, "bold"),
                 fg=COLORS["accent"], bg=COLORS["stat_bg"]).pack(pady=(8, 5), anchor="w", padx=8)

        self.desc_list = tk.Listbox(self.desc_frame,
                                     font=("Consolas", 9),
                                     bg=COLORS["stat_bg"],
                                     fg=COLORS["text_gray"],
                                     selectbackground=COLORS["accent"],
                                     selectforeground=COLORS["text_white"],
                                     relief="flat", bd=0,
                                     activestyle="none",
                                     highlightthickness=0,
                                     height=8)
        self.desc_list.pack(fill="both", expand=True, padx=8, pady=(0, 8))

        # Panel derecho - Tabla de datos
        right_panel = tk.Frame(main_frame, bg=COLORS["table_bg"])
        right_panel.pack(side="right", fill="both", expand=True)

        tree_frame = tk.Frame(right_panel, bg=COLORS["table_bg"])
        tree_frame.pack(fill="both", expand=True)

        self.tree = ttk.Treeview(tree_frame, style="Custom.Treeview",
                                  show="headings", selectmode="extended")
        self.tree.pack(side="left", fill="both", expand=True)

        scrollbar_y = ttk.Scrollbar(tree_frame, orient="vertical",
                                     command=self.tree.yview,
                                     style="Custom.Vertical.TScrollbar")
        scrollbar_y.pack(side="right", fill="y")

        scrollbar_x = ttk.Scrollbar(right_panel, orient="horizontal",
                                     command=self.tree.xview,
                                     style="Custom.Horizontal.TScrollbar")
        scrollbar_x.pack(side="bottom", fill="x")

        self.tree.configure(yscrollcommand=scrollbar_y.set,
                            xscrollcommand=scrollbar_x.set)

        # === STATUS BAR ===
        status_bar = tk.Frame(self, bg=COLORS["bg_medium"], height=30)
        status_bar.pack(fill="x", side="bottom")
        status_bar.pack_propagate(False)

        self.lbl_status = tk.Label(status_bar,
                                    text="  Listo — Seleccione una base de datos SQLite para comenzar",
                                    font=("Segoe UI", 9),
                                    fg=COLORS["text_gray"], bg=COLORS["bg_medium"],
                                    anchor="w")
        self.lbl_status.pack(side="left", fill="x", expand=True, padx=5)

        self.lbl_count = tk.Label(status_bar, text="",
                                   font=("Segoe UI", 9, "bold"),
                                   fg=COLORS["accent"], bg=COLORS["bg_medium"])
        self.lbl_count.pack(side="right", padx=15)

        # Tags filas alternas
        self.tree.tag_configure("oddrow", background=COLORS["table_stripe"])
        self.tree.tag_configure("evenrow", background=COLORS["table_bg"])

    # --------------------------------------------------------
    # MENU CONTEXTUAL (CLIC DERECHO)
    # --------------------------------------------------------
    def _setup_context_menu(self):
        self.ctx_menu = tk.Menu(self, tearoff=0,
                                bg=COLORS["bg_medium"], fg=COLORS["text_white"],
                                activebackground=COLORS["accent"],
                                activeforeground=COLORS["text_white"],
                                font=("Segoe UI", 10))
        self.ctx_menu.add_command(label="Copiar celda", command=self._copy_cell)
        self.ctx_menu.add_command(label="Copiar fila completa", command=self._copy_row)
        self.ctx_menu.add_command(label="Copiar N, E, Z", command=self._copy_coords)
        self.ctx_menu.add_separator()
        self.ctx_menu.add_command(label="Copiar todas las filas visibles", command=self._copy_all_visible)

        self.tree.bind("<Button-3>", self._show_context_menu)
        self._right_click_col = None

    def _show_context_menu(self, event):
        item = self.tree.identify_row(event.y)
        col = self.tree.identify_column(event.x)
        if item:
            self.tree.selection_set(item)
            self._right_click_col = col
            self.ctx_menu.tk_popup(event.x_root, event.y_root)

    def _copy_cell(self):
        sel = self.tree.selection()
        if not sel or not self._right_click_col:
            return
        col_idx = int(self._right_click_col.replace("#", "")) - 1
        values = self.tree.item(sel[0], "values")
        if col_idx < len(values):
            self.clipboard_clear()
            self.clipboard_append(str(values[col_idx]))
            self._set_status("Celda copiada al portapapeles")

    def _copy_row(self):
        sel = self.tree.selection()
        if not sel:
            return
        values = self.tree.item(sel[0], "values")
        text = "\t".join(str(v) for v in values)
        self.clipboard_clear()
        self.clipboard_append(text)
        self._set_status("Fila copiada al portapapeles")

    def _copy_coords(self):
        sel = self.tree.selection()
        if not sel:
            return
        values = self.tree.item(sel[0], "values")
        # Buscar columnas de coordenadas
        coords = {}
        for i, col in enumerate(self.columns):
            if col in ("Northing", "Easting", "Elevation", "Number"):
                coords[col] = values[i] if i < len(values) else ""

        if coords:
            text = f"Pto: {coords.get('Number', '')}, N: {coords.get('Northing', '')}, E: {coords.get('Easting', '')}, Z: {coords.get('Elevation', '')}"
            self.clipboard_clear()
            self.clipboard_append(text)
            self._set_status("Coordenadas copiadas al portapapeles")
        else:
            self._set_status("No se encontraron columnas de coordenadas")

    def _copy_all_visible(self):
        children = self.tree.get_children()
        if not children:
            return
        lines = ["\t".join(self.columns)]
        for item in children:
            values = self.tree.item(item, "values")
            lines.append("\t".join(str(v) for v in values))
        self.clipboard_clear()
        self.clipboard_append("\n".join(lines))
        self._set_status(f"{len(children):,} filas copiadas al portapapeles")

    # --------------------------------------------------------
    # BOTONES PERSONALIZADOS
    # --------------------------------------------------------
    def _create_button(self, parent, text, command, icon=None, small=False, color=None):
        if small:
            font = ("Segoe UI", 9, "bold")
            padx, pady = 4, 2
        else:
            font = ("Segoe UI", 10, "bold")
            padx, pady = 12, 6

        if color == "green":
            bg = COLORS["button_green"]
            bg_hover = COLORS["button_green_hover"]
        elif color == "blue":
            bg = COLORS["button_blue"]
            bg_hover = COLORS["button_blue_hover"]
        elif color == "orange":
            bg = "#e67e22"
            bg_hover = "#f39c12"
        elif color == "red":
            bg = "#c0392b"
            bg_hover = "#e74c3c"
        else:
            bg = COLORS["button_bg"]
            bg_hover = COLORS["button_hover"]

        display_text = f"{icon} {text}" if icon else text

        btn = tk.Label(parent, text=display_text, font=font,
                       fg=COLORS["text_white"], bg=bg,
                       cursor="hand2", padx=padx, pady=pady)
        btn.bind("<Enter>", lambda e: btn.config(bg=bg_hover))
        btn.bind("<Leave>", lambda e: btn.config(bg=bg))
        btn.bind("<Button-1>", lambda e: command())
        return btn

    # --------------------------------------------------------
    # CENTRAR VENTANA
    # --------------------------------------------------------
    def _center_window(self):
        self.update_idletasks()
        w = self.winfo_width()
        h = self.winfo_height()
        x = (self.winfo_screenwidth() // 2) - (w // 2)
        y = (self.winfo_screenheight() // 2) - (h // 2) - 30
        self.geometry(f"+{x}+{y}")

    # --------------------------------------------------------
    # ABRIR BASE DE DATOS
    # --------------------------------------------------------
    def _open_database(self, path=None):
        if path is None:
            path = filedialog.askopenfilename(
                title="Seleccionar base de datos SQLite",
                filetypes=[
                    ("SQLite Database", "*.sqlite *.db *.sqlite3"),
                    ("Todos los archivos", "*.*")
                ],
                initialdir="C:\\Users\\Public\\Documents\\Autodesk"
            )
        if not path:
            return

        if self.conn:
            self.conn.close()

        try:
            self.conn = sqlite3.connect(path)
            self.db_path = path
            self._save_config()
        except Exception as e:
            messagebox.showerror("Error", f"No se pudo abrir la base de datos:\n{e}")
            return

        filename = os.path.basename(path)
        self.lbl_file.config(text=f"📁 {filename}", fg=COLORS["success"])
        self.title(f"Survey Database Viewer — {filename}")

        cursor = self.conn.cursor()
        cursor.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name")
        tables = [row[0] for row in cursor.fetchall()]

        self.table_listbox.delete(0, "end")
        for t in tables:
            cursor.execute(f'SELECT COUNT(*) FROM "{t}"')
            count = cursor.fetchone()[0]
            self.table_listbox.insert("end", f"  {t}  ({count})")

        self.combo_tables["values"] = tables
        self._set_status(f"Base de datos cargada: {len(tables)} tablas encontradas")

        if "Point" in tables:
            idx = tables.index("Point")
            self.combo_tables.current(idx)
            self.table_listbox.selection_set(idx)
            self._load_table("Point")
        elif tables:
            self.combo_tables.current(0)
            self.table_listbox.selection_set(0)
            self._load_table(tables[0])

    # --------------------------------------------------------
    # CARGAR TABLA
    # --------------------------------------------------------
    def _load_table(self, table_name):
        if not self.conn:
            return

        self.current_table = table_name
        self.search_var.set("")
        self.sort_column = None
        self.sort_reverse = False
        self.desc_filter = "TODAS"
        self.combo_desc.set("TODAS")

        cursor = self.conn.cursor()
        try:
            cursor.execute(f'SELECT * FROM "{table_name}"')
            self.all_rows = cursor.fetchall()
            self.columns = [desc[0] for desc in cursor.description]
        except Exception as e:
            messagebox.showerror("Error", f"Error al leer tabla:\n{e}")
            return

        # Configurar columnas del treeview
        self.tree["columns"] = self.columns
        for col in self.columns:
            self.tree.heading(col, text=col,
                              command=lambda c=col: self._sort_by_column(c))
            max_len = len(col)
            for row in self.all_rows[:100]:
                idx = self.columns.index(col)
                val_len = len(str(row[idx])) if row[idx] is not None else 0
                if val_len > max_len:
                    max_len = val_len
            width = min(max(max_len * 9, 80), 300)
            self.tree.column(col, width=width, minwidth=60)

        self._populate_tree(self.all_rows)

        # Info
        self.lbl_rows.config(text=f"Filas: {len(self.all_rows):,}")
        self.lbl_cols.config(text=f"Columnas: {len(self.columns)}")
        self._set_status(f"Tabla '{table_name}' — {len(self.all_rows):,} registros")
        self.lbl_count.config(text=f"{len(self.all_rows):,} registros")

        # Estadisticas y resumen (solo para tabla Point)
        self._update_stats()
        self._update_desc_summary()

    def _populate_tree(self, rows):
        self.tree.delete(*self.tree.get_children())
        for i, row in enumerate(rows):
            tag = "oddrow" if i % 2 else "evenrow"
            values = [v if v is not None else "" for v in row]
            self.tree.insert("", "end", values=values, tags=(tag,))
        self.lbl_count.config(text=f"{len(rows):,} registros")

    # --------------------------------------------------------
    # ESTADISTICAS
    # --------------------------------------------------------
    def _update_stats(self):
        # Solo calcular si las columnas existen
        has_elev = "Elevation" in self.columns
        has_north = "Northing" in self.columns
        has_east = "Easting" in self.columns

        if not any([has_elev, has_north, has_east]):
            self.lbl_stat_elev.config(text="Elevación: N/A")
            self.lbl_stat_north.config(text="Norte: N/A")
            self.lbl_stat_east.config(text="Este: N/A")
            return

        rows = self.all_rows
        if has_elev:
            idx = self.columns.index("Elevation")
            elevs = [r[idx] for r in rows if r[idx] is not None and isinstance(r[idx], (int, float))]
            if elevs:
                self.lbl_stat_elev.config(
                    text=f"Z: {min(elevs):.2f} ~ {max(elevs):.2f}\n   Prom: {sum(elevs)/len(elevs):.2f}")
            else:
                self.lbl_stat_elev.config(text="Elevación: -")

        if has_north:
            idx = self.columns.index("Northing")
            vals = [r[idx] for r in rows if r[idx] is not None and isinstance(r[idx], (int, float))]
            if vals:
                self.lbl_stat_north.config(text=f"N: {min(vals):.2f} ~ {max(vals):.2f}")
            else:
                self.lbl_stat_north.config(text="Norte: -")

        if has_east:
            idx = self.columns.index("Easting")
            vals = [r[idx] for r in rows if r[idx] is not None and isinstance(r[idx], (int, float))]
            if vals:
                self.lbl_stat_east.config(text=f"E: {min(vals):.2f} ~ {max(vals):.2f}")
            else:
                self.lbl_stat_east.config(text="Este: -")

    # --------------------------------------------------------
    # RESUMEN POR DESCRIPCION
    # --------------------------------------------------------
    def _update_desc_summary(self):
        self.desc_list.delete(0, "end")
        if "Description" not in self.columns:
            self.combo_desc["values"] = ["TODAS"]
            return

        idx = self.columns.index("Description")
        desc_count = {}
        for row in self.all_rows:
            desc = str(row[idx]).strip() if row[idx] else "(vacío)"
            desc_count[desc] = desc_count.get(desc, 0) + 1

        # Ordenar por cantidad descendente
        sorted_desc = sorted(desc_count.items(), key=lambda x: x[1], reverse=True)

        for desc, count in sorted_desc:
            self.desc_list.insert("end", f"  {desc:<12} {count:>5}")

        # Actualizar combo filtro
        desc_names = ["TODAS"] + [d[0] for d in sorted_desc]
        self.combo_desc["values"] = desc_names

    # --------------------------------------------------------
    # FILTRO POR DESCRIPCION
    # --------------------------------------------------------
    def _on_desc_filter(self, event=None):
        self.desc_filter = self.combo_desc.get()
        self._apply_filters()

    # --------------------------------------------------------
    # APLICAR FILTROS (busqueda + descripcion)
    # --------------------------------------------------------
    def _apply_filters(self):
        rows = self.all_rows

        # Filtro por descripcion
        if self.desc_filter != "TODAS" and "Description" in self.columns:
            desc_idx = self.columns.index("Description")
            if self.desc_filter == "(vacío)":
                rows = [r for r in rows if not r[desc_idx] or str(r[desc_idx]).strip() == ""]
            else:
                rows = [r for r in rows if str(r[desc_idx]).strip() == self.desc_filter]

        # Filtro por busqueda
        text = self.search_var.get().strip().lower()
        if text:
            filtered = []
            for row in rows:
                for val in row:
                    if text in str(val).lower():
                        filtered.append(row)
                        break
            rows = filtered

        self._populate_tree(rows)
        self._set_status(f"Mostrando {len(rows):,} de {len(self.all_rows):,} registros")

    # --------------------------------------------------------
    # ORDENAR POR COLUMNA
    # --------------------------------------------------------
    def _sort_by_column(self, col):
        idx = self.columns.index(col)

        if self.sort_column == col:
            self.sort_reverse = not self.sort_reverse
        else:
            self.sort_column = col
            self.sort_reverse = False

        def sort_key(row):
            val = row[idx]
            if val is None:
                return (1, "")
            if isinstance(val, (int, float)):
                return (0, val)
            try:
                return (0, float(val))
            except (ValueError, TypeError):
                return (0, str(val).lower())

        self.all_rows.sort(key=sort_key, reverse=self.sort_reverse)

        arrow = " ▼" if self.sort_reverse else " ▲"
        for c in self.columns:
            text = c + (arrow if c == col else "")
            self.tree.heading(c, text=text)

        self._apply_filters()

    # --------------------------------------------------------
    # BUSCAR
    # --------------------------------------------------------
    def _on_search(self, *args):
        self._apply_filters()

    def _clear_search(self):
        self.search_var.set("")
        self.entry_search.focus_set()

    # --------------------------------------------------------
    # EVENTOS COMBO / LISTBOX
    # --------------------------------------------------------
    def _on_table_select(self, event):
        table = self.combo_tables.get()
        if table:
            self._load_table(table)
            values = self.combo_tables["values"]
            if table in values:
                idx = list(values).index(table)
                self.table_listbox.selection_clear(0, "end")
                self.table_listbox.selection_set(idx)

    def _on_listbox_select(self, event):
        sel = self.table_listbox.curselection()
        if not sel:
            return
        text = self.table_listbox.get(sel[0]).strip()
        table_name = text.split("(")[0].strip()
        values = list(self.combo_tables["values"])
        if table_name in values:
            self.combo_tables.current(values.index(table_name))
            self._load_table(table_name)

    # --------------------------------------------------------
    # ELIMINAR GRUPO IMPORTADO
    # --------------------------------------------------------
    def _delete_import_group(self):
        if not self.conn or not self.db_path:
            messagebox.showwarning("Aviso", "Primero abra una base de datos.")
            return

        if self._is_civil3d_running():
            messagebox.showerror("⚠ Civil 3D está abierto",
                "Civil 3D está ejecutándose.\n"
                "Cierre Civil 3D antes de eliminar grupos.")
            return

        # Obtener lista de ImportEvents
        cursor = self.conn.cursor()
        cursor.execute("""SELECT ie.Id, ie.Name, COUNT(p.Id) as pts
            FROM ImportEvent ie
            LEFT JOIN Point p ON p.ImportEventId = ie.Id
            GROUP BY ie.Id ORDER BY ie.Id""")
        events = cursor.fetchall()

        if not events:
            messagebox.showinfo("Info", "No hay grupos de importación.")
            return

        # Ventana de selección
        dlg = tk.Toplevel(self)
        dlg.title("Eliminar Grupo de Importación")
        dlg.geometry("500x400")
        dlg.configure(bg=COLORS["bg_dark"])
        dlg.transient(self)
        dlg.grab_set()

        tk.Label(dlg, text="Seleccione el grupo a eliminar:",
                 font=("Segoe UI", 11, "bold"),
                 fg=COLORS["text_white"], bg=COLORS["bg_dark"]).pack(pady=(15, 10), padx=15, anchor="w")

        frame = tk.Frame(dlg, bg=COLORS["bg_dark"])
        frame.pack(fill="both", expand=True, padx=15, pady=5)

        listbox = tk.Listbox(frame, font=("Consolas", 10),
                             bg=COLORS["table_bg"], fg=COLORS["text_light"],
                             selectbackground=COLORS["accent"],
                             selectforeground=COLORS["text_white"],
                             relief="flat", bd=0, highlightthickness=0)
        scroll = tk.Scrollbar(frame, command=listbox.yview)
        listbox.configure(yscrollcommand=scroll.set)
        scroll.pack(side="right", fill="y")
        listbox.pack(side="left", fill="both", expand=True)

        for eid, name, pts in events:
            listbox.insert("end", f"  ID {eid:>3}  |  {name:<30}  |  {pts:,} puntos")

        def do_delete():
            sel = listbox.curselection()
            if not sel:
                messagebox.showwarning("Aviso", "Seleccione un grupo.", parent=dlg)
                return
            idx = sel[0]
            eid, name, pts = events[idx]

            confirm = messagebox.askyesno("Confirmar eliminación",
                f"¿ELIMINAR este grupo?\n\n"
                f"ID: {eid}\n"
                f"Nombre: {name}\n"
                f"Puntos: {pts:,}\n\n"
                f"Se eliminarán:\n"
                f"  • {pts:,} puntos de la tabla Point\n"
                f"  • Registros de List asociados\n"
                f"  • Registros de PointGroup\n"
                f"  • El ImportEvent\n\n"
                f"Esta acción NO se puede deshacer.",
                parent=dlg)
            if not confirm:
                return

            try:
                # Backup antes de eliminar
                self._create_backup("pre_delete")

                c = self.conn.cursor()
                # Obtener IDs de puntos a eliminar
                c.execute("SELECT Id FROM Point WHERE ImportEventId = ?", (eid,))
                point_ids = [r[0] for r in c.fetchall()]

                # Eliminar List entries de esos puntos
                if point_ids:
                    placeholders = ",".join("?" * len(point_ids))
                    c.execute(f"DELETE FROM List WHERE Id IN ({placeholders})", point_ids)

                # Eliminar PointGroup
                c.execute("DELETE FROM PointGroup WHERE ImportEventId = ?", (eid,))
                # Eliminar Points
                c.execute("DELETE FROM Point WHERE ImportEventId = ?", (eid,))
                # Eliminar ImportEvent
                c.execute("DELETE FROM ImportEvent WHERE Id = ?", (eid,))

                self.conn.commit()
                messagebox.showinfo("Eliminado",
                    f"Grupo eliminado exitosamente.\n\n"
                    f"Puntos eliminados: {len(point_ids):,}\n"
                    f"Se creó respaldo automático.",
                    parent=dlg)
                dlg.destroy()
                self._open_database(self.db_path)

            except Exception as e:
                self.conn.rollback()
                messagebox.showerror("Error", f"Error al eliminar:\n{e}", parent=dlg)

        btn_frame = tk.Frame(dlg, bg=COLORS["bg_dark"])
        btn_frame.pack(fill="x", padx=15, pady=15)
        self._create_button(btn_frame, " ELIMINAR GRUPO SELECCIONADO ",
                           do_delete, icon="🗑", color="red").pack(side="right")
        self._create_button(btn_frame, " Cancelar ",
                           dlg.destroy, small=True).pack(side="right", padx=10)

    # --------------------------------------------------------
    # BACKUP AUTOMÁTICO
    # --------------------------------------------------------
    def _create_backup(self, tag="backup"):
        """Crea respaldo de la base de datos con timestamp"""
        import datetime
        if not self.db_path:
            return None
        ts = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
        db_dir = os.path.dirname(self.db_path)
        db_name = os.path.splitext(os.path.basename(self.db_path))[0]
        db_ext = os.path.splitext(self.db_path)[1]
        backup_path = os.path.join(db_dir, f"{db_name}_{tag}_{ts}{db_ext}")
        import shutil
        shutil.copy2(self.db_path, backup_path)
        return backup_path

    # --------------------------------------------------------
    # IMPORTAR PUNTOS DESDE CSV A LA BASE DE DATOS
    # La app asigna números automáticamente (sin conflictos)
    # CSV puede ser: X,Y,Z,Desc o Num,X,Y,Z,Desc (ignora Num)
    # --------------------------------------------------------
    def _is_civil3d_running(self):
        """Verifica si Civil 3D o AutoCAD está corriendo"""
        import subprocess
        try:
            result = subprocess.run(
                ["tasklist", "/FI", "IMAGENAME eq acad.exe", "/NH"],
                capture_output=True, text=True, timeout=5)
            return "acad.exe" in result.stdout.lower()
        except Exception:
            return False

    def _import_points_csv(self):
        if not self.conn or not self.db_path:
            messagebox.showwarning("Aviso", "Primero abra una base de datos.")
            return

        # Verificar que Civil 3D NO esté corriendo
        if self._is_civil3d_running():
            messagebox.showerror(
                "⚠ Civil 3D está abierto",
                "Civil 3D está ejecutándose.\n\n"
                "NO se puede importar mientras Civil 3D\n"
                "esté abierto. Civil 3D monitorea el archivo\n"
                "Survey.sqlite y se cierra con FATAL ERROR\n"
                "si detecta cambios externos.\n\n"
                "CIERRE CIVIL 3D completamente\n"
                "e intente de nuevo."
            )
            return

        # Obtener info de IDs disponibles
        cursor = self.conn.cursor()
        cursor.execute("SELECT COALESCE(MAX(Number), 0) FROM Point")
        max_number = cursor.fetchone()[0]
        cursor.execute("SELECT COUNT(*) FROM Point")
        total_points = cursor.fetchone()[0]
        next_number = max_number + 1

        # Mostrar info antes de seleccionar archivo
        info = messagebox.askokcancel(
            "Importar Puntos al Survey Database",
            f"BASE DE DATOS: {os.path.basename(self.db_path)}\n"
            f"Puntos actuales: {total_points:,}\n"
            f"Último número: {max_number}\n\n"
            f"La numeración se asigna automáticamente\n"
            f"desde el número: {next_number}\n\n"
            f"Formatos CSV aceptados:\n"
            f"  • X, Y, Z, Descripción\n"
            f"  • Num, X, Y, Z, Descripción (ignora Num)\n\n"
            f"✅ Civil 3D no detectado - seguro para importar\n\n"
            f"¿Seleccionar archivo CSV?"
        )
        if not info:
            return

        path = filedialog.askopenfilename(
            title="Seleccionar archivo de puntos para importar",
            filetypes=[("CSV", "*.csv"), ("Texto", "*.txt"), ("Todos", "*.*")],
            initialdir=os.path.join(os.path.expanduser("~"), "OneDrive", "Escritorio")
        )
        if not path:
            return

        try:
            with open(path, "r", encoding="utf-8-sig") as f:
                lines = f.readlines()

            if not lines:
                messagebox.showerror("Error", "El archivo está vacío.")
                return

            # Detectar separador
            first = lines[0]
            if "\t" in first:
                sep = "\t"
            elif "," in first:
                sep = ","
            else:
                sep = " "

            # Detectar encabezado
            start = 0
            parts = [p.strip() for p in first.split(sep) if p.strip()]
            try:
                float(parts[0])
            except ValueError:
                start = 1

            # Detectar si tiene columna de número (5+ cols) o solo coords (4 cols)
            sample_parts = [p.strip() for p in lines[start].split(sep) if p.strip()] if len(lines) > start else []
            has_number_col = len(sample_parts) >= 5

            # Preguntar orden de coordenadas
            fmt = messagebox.askyesno(
                "Formato de coordenadas",
                f"Archivo: {os.path.basename(path)}\n"
                f"Líneas de datos: {len(lines) - start}\n"
                f"Separador: {'coma' if sep == ',' else 'tab' if sep == chr(9) else 'espacio'}\n"
                f"Columnas detectadas: {len(sample_parts)}\n"
                f"{'Con' if has_number_col else 'Sin'} columna de número "
                f"{'(se ignora, se renumera)' if has_number_col else ''}\n\n"
                f"¿El orden de coordenadas es ESTE, NORTE?\n\n"
                f"SI = Este (X), Norte (Y), Elev (Z)\n"
                f"NO = Norte (Y), Este (X), Elev (Z)"
            )

            # Parsear puntos (solo coords + desc, sin número)
            points = []  # Lista de (easting, northing, elev, desc)
            errors = 0
            for i in range(start, len(lines)):
                line = lines[i].strip()
                if not line:
                    continue
                if sep == " ":
                    parts = line.split()
                else:
                    parts = line.split(sep)
                parts = [p.strip() for p in parts if p.strip()]

                try:
                    if has_number_col:
                        # Formato: Num, X/Y, Y/X, Z, Desc → ignorar Num
                        if len(parts) < 4:
                            errors += 1
                            continue
                        coord_start = 1
                    else:
                        # Formato: X/Y, Y/X, Z, Desc
                        if len(parts) < 3:
                            errors += 1
                            continue
                        coord_start = 0

                    if fmt:  # Este, Norte
                        easting = float(parts[coord_start])
                        northing = float(parts[coord_start + 1])
                    else:  # Norte, Este
                        northing = float(parts[coord_start])
                        easting = float(parts[coord_start + 1])

                    elev = float(parts[coord_start + 2])
                    desc = parts[coord_start + 3].strip() if len(parts) > coord_start + 3 else ""
                    points.append((easting, northing, elev, desc))
                except (ValueError, IndexError):
                    errors += 1

            if not points:
                messagebox.showerror("Error",
                                     f"No se pudieron leer puntos del archivo.\nErrores: {errors}")
                return

            # Resumen con numeración automática
            es = [p[0] for p in points]
            ns = [p[1] for p in points]
            zs = [p[2] for p in points]
            last_number = next_number + len(points) - 1

            # --- VALIDACIÓN UTM ---
            utm_warning = ""
            # Rangos razonables para UTM (Easting: 100k-900k, Northing: 0-10M)
            if min(es) < 100000 or max(es) > 900000:
                utm_warning += "⚠ ESTE fuera de rango UTM (100,000-900,000)\n"
            if min(ns) < 0 or max(ns) > 10000000:
                utm_warning += "⚠ NORTE fuera de rango UTM (0-10,000,000)\n"
            # Detectar columnas posiblemente invertidas (Norte en Este o viceversa)
            if min(es) > 1000000 and max(ns) < 1000000:
                utm_warning += "⚠ Las coordenadas podrían estar INVERTIDAS\n   (Este parece Norte y viceversa)\n"
            if utm_warning:
                utm_warning = f"\n⚠ ALERTAS DE COORDENADAS:\n{utm_warning}\n"

            # --- VISTA PREVIA ---
            preview_ok = self._show_import_preview(
                points, next_number, os.path.basename(path),
                errors, utm_warning, es, ns, zs, last_number)
            if not preview_ok:
                return

            # === BACKUP AUTOMÁTICO ===
            backup_path = self._create_backup("pre_import")
            self._set_status(f"Backup creado: {os.path.basename(backup_path)}")

            # === INSERTAR EN LA BASE DE DATOS ===
            file_name = os.path.basename(path)
            self._insert_points_into_db(points, file_name, next_number)

            # Refrescar vista
            self._open_database(self.db_path)

        except Exception as e:
            messagebox.showerror("Error", f"Error al importar:\n{e}")

    def _show_import_preview(self, points, start_num, filename, errors, utm_warning, es, ns, zs, last_num):
        """Muestra ventana de vista previa con los primeros puntos. Retorna True si el usuario confirma."""
        result = [False]

        dlg = tk.Toplevel(self)
        dlg.title("Vista Previa - Confirmar Importación")
        dlg.geometry("750x520")
        dlg.configure(bg=COLORS["bg_dark"])
        dlg.transient(self)
        dlg.grab_set()

        # Info superior
        info_frame = tk.Frame(dlg, bg=COLORS["bg_medium"])
        info_frame.pack(fill="x", padx=10, pady=(10, 5))

        info_text = (
            f"Archivo: {filename}    |    Puntos: {len(points):,}    |    Errores: {errors}\n"
            f"Numeración: {start_num} al {last_num}\n"
            f"Este: {min(es):.3f} ~ {max(es):.3f}    |    "
            f"Norte: {min(ns):.3f} ~ {max(ns):.3f}    |    "
            f"Elev: {min(zs):.3f} ~ {max(zs):.3f}"
        )
        tk.Label(info_frame, text=info_text, font=("Consolas", 9),
                 fg=COLORS["text_light"], bg=COLORS["bg_medium"],
                 justify="left").pack(padx=10, pady=8, anchor="w")

        # Alerta UTM si hay
        if utm_warning:
            warn_frame = tk.Frame(dlg, bg="#4a1a1a")
            warn_frame.pack(fill="x", padx=10, pady=(0, 5))
            tk.Label(warn_frame, text=utm_warning.strip(), font=("Segoe UI", 9, "bold"),
                     fg="#ff6b6b", bg="#4a1a1a",
                     justify="left").pack(padx=10, pady=6, anchor="w")

        # Título tabla
        tk.Label(dlg, text=f"Primeros {min(15, len(points))} puntos (de {len(points):,}):",
                 font=("Segoe UI", 10, "bold"),
                 fg=COLORS["accent"], bg=COLORS["bg_dark"]).pack(padx=10, pady=(5, 3), anchor="w")

        # Tabla preview
        tree_frame = tk.Frame(dlg, bg=COLORS["table_bg"])
        tree_frame.pack(fill="both", expand=True, padx=10, pady=(0, 5))

        cols = ("Punto", "Easting", "Northing", "Elevation", "Description")
        tree = ttk.Treeview(tree_frame, columns=cols, show="headings",
                            style="Custom.Treeview", height=12)
        tree.pack(side="left", fill="both", expand=True)
        scroll = ttk.Scrollbar(tree_frame, orient="vertical", command=tree.yview)
        scroll.pack(side="right", fill="y")
        tree.configure(yscrollcommand=scroll.set)

        tree.column("Punto", width=70, anchor="center")
        tree.column("Easting", width=130, anchor="e")
        tree.column("Northing", width=130, anchor="e")
        tree.column("Elevation", width=90, anchor="e")
        tree.column("Description", width=120)
        for c in cols:
            tree.heading(c, text=c)

        for i, (east, north, elev, desc) in enumerate(points[:15]):
            num = start_num + i
            tag = "oddrow" if i % 2 else "evenrow"
            tree.insert("", "end", values=(num, f"{east:.3f}", f"{north:.3f}", f"{elev:.3f}", desc), tags=(tag,))

        if len(points) > 15:
            tree.insert("", "end", values=("...", "...", "...", "...", f"(+{len(points)-15:,} más)"))

        # Botones
        btn_frame = tk.Frame(dlg, bg=COLORS["bg_dark"])
        btn_frame.pack(fill="x", padx=10, pady=10)

        def on_confirm():
            result[0] = True
            dlg.destroy()

        self._create_button(btn_frame, "  Cancelar  ",
                           dlg.destroy, small=True).pack(side="right", padx=5)
        self._create_button(btn_frame, "  ✅ IMPORTAR  ",
                           on_confirm, color="green").pack(side="right", padx=5)

        dlg.wait_window()
        return result[0]

    def _insert_points_into_db(self, points, file_name, start_number):
        """Inserta puntos con estructura completa compatible con Civil 3D.
        points = lista de (easting, northing, elev, desc)
        start_number = primer número de punto a asignar
        """
        import datetime
        cursor = self.conn.cursor()

        try:
            # 1. ImportEvent
            cursor.execute("SELECT COALESCE(MAX(Id), 0) + 1 FROM ImportEvent")
            event_id = cursor.fetchone()[0]
            cursor.execute("SELECT COALESCE(MAX(ProcessOrder), 0) + 1 FROM ImportEvent")
            event_order = cursor.fetchone()[0]

            cursor.execute("""INSERT INTO ImportEvent
                (Id, Revision, Name, Description, DateTimeStamp, FilePath, ImportType,
                 UsePointIdOffset, PointIdOffset, ProcessLinework, F2FConventionName,
                 NetworkId, PointFormatName, SourceUnit, UserName,
                 ProcessLineworkSequence, ProcessOrder, EquipmentDbName,
                 EquipmentName, FigurePrefixDbName)
                VALUES
                (?, 1, ?, ?, ?, ?, 8,
                 0, 0, 0, 'Sample',
                 0, 'PENZD (comma delimited)', 2, ?,
                 1, ?, '', '', 'Sample')""",
                (event_id, file_name,
                 f"Importado desde Survey Viewer ({len(points)} puntos)",
                 datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
                 file_name, os.getlogin(), event_order))

            # 2. PointGroup (2 registros como Civil 3D)
            cursor.execute("SELECT COALESCE(MAX(Id), 0) + 1 FROM PointGroup")
            pg_id = cursor.fetchone()[0]
            cursor.execute("SELECT COALESCE(MAX(ProcessOrder), 0) + 1 FROM PointGroup")
            pg_order = cursor.fetchone()[0]

            file_no_ext = os.path.splitext(file_name)[0]
            hex_name = f"0x{event_id:08X}:{file_name}"

            cursor.execute("""INSERT INTO PointGroup
                (Id, Revision, Name, Description, ImportEventId, ImportEventPrimary, ProcessOrder)
                VALUES (?, 2, ?, '', ?, 1, ?)""",
                (pg_id, hex_name, event_id, pg_order))

            visible_pg_id = pg_id + 1
            cursor.execute("""INSERT INTO PointGroup
                (Id, Revision, Name, Description, ImportEventId, ImportEventPrimary, ProcessOrder)
                VALUES (?, 2, ?, ?, ?, 0, ?)""",
                (visible_pg_id, file_no_ext, file_name, event_id, pg_order + 1))

            # 3. Points + List
            cursor.execute("SELECT COALESCE(MAX(Id), 0) + 1 FROM Point")
            next_point_id = cursor.fetchone()[0]
            cursor.execute("SELECT COALESCE(MAX(ProcessOrder), 0) + 1 FROM Point")
            next_point_order = cursor.fetchone()[0]
            cursor.execute("SELECT COALESCE(MAX(ProcessOrder), 0) + 1 FROM List")
            list_order = cursor.fetchone()[0]

            for i, (east, north, elev, desc) in enumerate(points):
                point_id = next_point_id + i
                point_num = start_number + i

                cursor.execute("""INSERT INTO Point
                    (Id, Revision, Number, Name, Northing, Easting, Elevation, Description,
                     IsControlPoint, IsSetupPoint, IsNessPoint, NetworkId, Monument,
                     LandXML, ImportEventId, ProcessOrder, OriginalNumber, OriginalName,
                     IsFakePoint, InitialObservationId, ParseUnit)
                    VALUES
                    (?, 0, ?, '', ?, ?, ?, ?,
                     0, 0, 0, 0, 0,
                     '', ?, ?, ?, '',
                     0, 0, 2)""",
                    (point_id, point_num, north, east, elev, desc,
                     event_id, next_point_order + i, point_num))

                cursor.execute(
                    "INSERT INTO List (Id, ListOwnerId, ListName, ProcessOrder) VALUES (?, 0, 'Point', ?)",
                    (point_id, list_order))
                list_order += 1

                cursor.execute(
                    "INSERT INTO List (Id, ListOwnerId, ListName, ProcessOrder) VALUES (?, ?, 'Group', ?)",
                    (point_id, visible_pg_id, list_order))
                list_order += 1

            self.conn.commit()

            last_num = start_number + len(points) - 1
            messagebox.showinfo("Importación Completa",
                f"Importación exitosa!\n\n"
                f"Puntos importados: {len(points):,}\n"
                f"Numeración: {start_number} al {last_num}\n"
                f"Grupo: {file_no_ext}\n\n"
                f"Registros creados:\n"
                f"  • ImportEvent: 1\n"
                f"  • PointGroup: 2\n"
                f"  • Point: {len(points):,}\n"
                f"  • List: {len(points) * 2:,}\n\n"
                f"Estructura compatible con Civil 3D.\n"
                f"Abra Civil 3D para ver los puntos."
            )
            self._set_status(f"{len(points):,} puntos importados (#{start_number}-{last_num}) como '{file_no_ext}'")

        except Exception as e:
            self.conn.rollback()
            raise e

    # --------------------------------------------------------
    # EXPORTAR PUNTOS (solo PUNTO, X, Y, Z, DESC)
    # --------------------------------------------------------
    def _export_points_csv(self):
        if not self.current_table or not self.all_rows:
            messagebox.showwarning("Aviso", "No hay datos para exportar.")
            return

        # Verificar que existan las columnas de puntos
        missing = [c for c in POINT_EXPORT_COLS if c not in self.columns]
        if missing:
            messagebox.showwarning("Aviso",
                                   f"La tabla actual no tiene las columnas de puntos.\n"
                                   f"Columnas faltantes: {', '.join(missing)}\n\n"
                                   f"Use 'Exportar CSV' para exportar todas las columnas.")
            return

        col_indices = [self.columns.index(c) for c in POINT_EXPORT_COLS]

        # Usar filas filtradas (visibles)
        visible_items = self.tree.get_children()
        visible_rows = []
        for item in visible_items:
            vals = self.tree.item(item, "values")
            visible_rows.append(vals)

        db_name = os.path.splitext(os.path.basename(self.db_path))[0] if self.db_path else "puntos"
        default_name = f"Puntos_{db_name}.csv"

        path = filedialog.asksaveasfilename(
            title="Exportar Puntos (PUNTO, X, Y, Z, DESC)",
            defaultextension=".csv",
            initialfile=default_name,
            initialdir=os.path.join(os.path.expanduser("~"), "OneDrive", "Escritorio"),
            filetypes=[("CSV (Excel)", "*.csv"), ("Texto separado por tabulador", "*.txt"), ("Todos", "*.*")]
        )
        if not path:
            return

        try:
            sep = "\t" if path.endswith(".txt") else ","
            with open(path, "w", newline="", encoding="utf-8-sig") as f:
                writer = csv.writer(f, delimiter=sep)
                writer.writerow(POINT_EXPORT_HEADERS)
                for row in visible_rows:
                    export_row = [row[i] for i in col_indices]
                    writer.writerow(export_row)

            self._set_status(f"Puntos exportados: {path}")
            messagebox.showinfo("Exportación exitosa",
                                f"Se exportaron {len(visible_rows):,} puntos a:\n{path}\n\n"
                                f"Columnas: PUNTO, ESTE(X), NORTE(Y), ELEVACION(Z), DESCRIPCION")
        except Exception as e:
            messagebox.showerror("Error", f"Error al exportar:\n{e}")

    # --------------------------------------------------------
    # EXPORTAR CSV (tabla completa)
    # --------------------------------------------------------
    def _export_csv(self):
        if not self.current_table or not self.all_rows:
            messagebox.showwarning("Aviso", "No hay datos para exportar.")
            return

        default_name = f"{self.current_table}_completo.csv"
        path = filedialog.asksaveasfilename(
            title="Exportar tabla completa como CSV",
            defaultextension=".csv",
            initialfile=default_name,
            initialdir=os.path.join(os.path.expanduser("~"), "OneDrive", "Escritorio"),
            filetypes=[("CSV", "*.csv"), ("Todos", "*.*")]
        )
        if not path:
            return

        try:
            with open(path, "w", newline="", encoding="utf-8-sig") as f:
                writer = csv.writer(f)
                writer.writerow(self.columns)
                writer.writerows(self.all_rows)
            self._set_status(f"Exportado: {path}")
            messagebox.showinfo("Exportación exitosa",
                                f"Se exportaron {len(self.all_rows):,} registros a:\n{path}")
        except Exception as e:
            messagebox.showerror("Error", f"Error al exportar:\n{e}")

    # --------------------------------------------------------
    # STATUS BAR
    # --------------------------------------------------------
    def _set_status(self, text):
        self.lbl_status.config(text=f"  {text}")

    # --------------------------------------------------------
    # CERRAR
    # --------------------------------------------------------
    def destroy(self):
        if self.conn:
            self.conn.close()
        super().destroy()


# ============================================================
# MAIN
# ============================================================
if __name__ == "__main__":
    app = SurveyViewer()
    app.mainloop()
