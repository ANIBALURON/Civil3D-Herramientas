"""
╔══════════════════════════════════════════════════════════════════╗
║  COORDENADAS DESDE LANDXML                                      ║
║  Lee un XML exportado de Civil 3D, interpola coordenadas        ║
║  cada N metros y exporta CSV: ID, ESTE, NORTE, COTA, ESTACION  ║
║                                                                  ║
║  TopografiaCivil3d.com                                          ║
║  USO: Doble clic o: python Coordenadas_XML.py                  ║
╚══════════════════════════════════════════════════════════════════╝
"""

import tkinter as tk
from tkinter import filedialog, messagebox, ttk
import xml.etree.ElementTree as ET
import math
import csv
import os
import sys


# ============================================================================
# UTILIDADES XML (manejo de namespace)
# ============================================================================
class XMLHelper:
    """Busca elementos con o sin namespace de LandXML"""

    def __init__(self, root):
        tag = root.tag
        self.ns = tag[tag.index('{'):tag.index('}') + 1] if '{' in tag else ''

    def find(self, parent, tag):
        result = parent.find(f'{self.ns}{tag}')
        if result is None:
            result = parent.find(tag)
        return result

    def findall(self, parent, tag):
        result = parent.findall(f'{self.ns}{tag}')
        if not result:
            result = parent.findall(tag)
        return result

    def find_deep(self, parent, tag):
        result = parent.find(f'.//{self.ns}{tag}')
        if result is None:
            result = parent.find(f'.//{tag}')
        return result


# ============================================================================
# PARSER DE LANDXML
# ============================================================================
class LandXMLParser:

    def __init__(self):
        self.alignments = []
        self.log_messages = []

    def log(self, msg):
        self.log_messages.append(msg)

    def get_and_clear_log(self):
        msgs = list(self.log_messages)
        self.log_messages.clear()
        return msgs

    def parse(self, filepath):
        self.alignments = []
        self.log(f"Leyendo: {os.path.basename(filepath)}")

        tree = ET.parse(filepath)
        root = tree.getroot()
        xh = XMLHelper(root)

        self.log(f"  Namespace: {xh.ns if xh.ns else '(ninguno)'}")

        aligns_elem = xh.find_deep(root, 'Alignments')
        if aligns_elem is None:
            self.log("  ERROR: No se encontro <Alignments>")
            return 0

        for align_elem in xh.findall(aligns_elem, 'Alignment'):
            name = align_elem.get('name', 'Sin nombre')
            sta_start = float(align_elem.get('staStart', '0'))
            length = float(align_elem.get('length', '0'))

            self.log(f"\n  Alineamiento: {name}")
            self.log(f"  Estacion inicio: {sta_start:.3f}")
            self.log(f"  Longitud: {length:.3f} m")

            # Geometría
            segments = []
            cg = xh.find(align_elem, 'CoordGeom')

            if cg is not None:
                for elem in cg:
                    tag_name = elem.tag.replace(xh.ns, '')

                    if tag_name == 'Line':
                        seg = self._parse_line(xh, elem)
                        if seg: segments.append(seg)

                    elif tag_name == 'Curve':
                        seg = self._parse_curve(xh, elem)
                        if seg: segments.append(seg)

                    elif tag_name == 'Spiral':
                        seg = self._parse_spiral(xh, elem)
                        if seg: segments.append(seg)

            # Profile
            profile = []
            pa = xh.find_deep(align_elem, 'ProfAlign')
            if pa is not None:
                for pvi in xh.findall(pa, 'PVI'):
                    vals = pvi.text.strip().split()
                    if len(vals) >= 2:
                        profile.append((float(vals[0]), float(vals[1])))
                if profile:
                    self.log(f"  Rasante: {len(profile)} PVIs")

            total_len = sum(s['length'] for s in segments)
            self.log(f"  Segmentos: {len(segments)}")
            self.log(f"  Longitud calculada: {total_len:.3f} m")

            self.alignments.append({
                'name': name,
                'staStart': sta_start,
                'length': length if length > 0 else total_len,
                'segments': segments,
                'profile': profile,
            })

        self.log(f"\n  Total alineamientos: {len(self.alignments)}")
        return len(self.alignments)

    def _parse_line(self, xh, elem):
        try:
            sta = float(elem.get('staStart', '0'))
            ln = float(elem.get('length', '0'))
            se = xh.find(elem, 'Start')
            ee = xh.find(elem, 'End')
            if se is None or ee is None: return None
            sv = se.text.strip().split()
            ev = ee.text.strip().split()
            sn, sest = float(sv[0]), float(sv[1])
            en, eest = float(ev[0]), float(ev[1])
            if ln <= 0:
                ln = math.sqrt((eest - sest)**2 + (en - sn)**2)
            return {'type': 'Line', 'staStart': sta, 'length': ln,
                    'startEste': sest, 'startNorte': sn,
                    'endEste': eest, 'endNorte': en}
        except:
            return None

    def _parse_curve(self, xh, elem):
        try:
            sta = float(elem.get('staStart', '0'))
            ln = float(elem.get('length', '0'))
            r = float(elem.get('radius', '0'))
            rot = elem.get('rot', 'cw')
            se = xh.find(elem, 'Start')
            ee = xh.find(elem, 'End')
            ce = xh.find(elem, 'Center')
            if se is None or ee is None: return None
            sv = se.text.strip().split()
            ev = ee.text.strip().split()
            cn, cest = 0, 0
            if ce is not None:
                cv = ce.text.strip().split()
                cn, cest = float(cv[0]), float(cv[1])
            return {'type': 'Curve', 'staStart': sta, 'length': ln,
                    'radius': r, 'rot': rot,
                    'startEste': float(sv[1]), 'startNorte': float(sv[0]),
                    'endEste': float(ev[1]), 'endNorte': float(ev[0]),
                    'centerEste': cest, 'centerNorte': cn}
        except:
            return None

    def _parse_spiral(self, xh, elem):
        try:
            sta = float(elem.get('staStart', '0'))
            ln = float(elem.get('length', '0'))
            se = xh.find(elem, 'Start')
            ee = xh.find(elem, 'End')
            if se is None or ee is None: return None
            sv = se.text.strip().split()
            ev = ee.text.strip().split()
            return {'type': 'Spiral', 'staStart': sta, 'length': ln,
                    'startEste': float(sv[1]), 'startNorte': float(sv[0]),
                    'endEste': float(ev[1]), 'endNorte': float(ev[0])}
        except:
            return None


# ============================================================================
# INTERPOLADOR
# ============================================================================
class AlignmentInterpolator:

    def __init__(self):
        self.log_messages = []

    def log(self, msg):
        self.log_messages.append(msg)

    def get_and_clear_log(self):
        msgs = list(self.log_messages)
        self.log_messages.clear()
        return msgs

    def interpolate(self, alignment, interval, include_vertices=True):
        segments = alignment['segments']
        profile = alignment.get('profile', [])
        if not segments: return []

        # Construir lista de puntos con estación acumulada
        points = []
        for seg in segments:
            if seg['type'] == 'Curve':
                curve_pts = self._discretize_curve(seg, num_points=60)
                step = seg['length'] / (len(curve_pts) - 1)
                for j, cp in enumerate(curve_pts):
                    points.append({
                        'sta': seg['staStart'] + j * step,
                        'este': cp[0], 'norte': cp[1],
                        'is_vertex': (j == 0)
                    })
            else:
                points.append({
                    'sta': seg['staStart'],
                    'este': seg['startEste'], 'norte': seg['startNorte'],
                    'is_vertex': True
                })

        # Punto final
        last = segments[-1]
        points.append({
            'sta': last['staStart'] + last['length'],
            'este': last['endEste'], 'norte': last['endNorte'],
            'is_vertex': True
        })

        sta_ini = points[0]['sta']
        sta_fin = points[-1]['sta']

        self.log(f"  Estacion inicio: {self._fmt(sta_ini)}")
        self.log(f"  Estacion fin:    {self._fmt(sta_fin)}")
        self.log(f"  Longitud total:  {sta_fin - sta_ini:.3f} m")
        self.log(f"  Intervalo:       {interval} m")

        # Generar estaciones objetivo
        targets = [sta_ini]

        first_round = math.ceil(sta_ini / interval) * interval
        if first_round <= sta_ini:
            first_round += interval
        s = first_round
        while s < sta_fin:
            targets.append(s)
            s += interval
        if targets[-1] < sta_fin:
            targets.append(sta_fin)

        if include_vertices:
            for p in points:
                if p['is_vertex']:
                    already = False
                    for t in targets:
                        if abs(t - p['sta']) < 0.01:
                            already = True
                            break
                    if not already:
                        targets.append(p['sta'])
            targets.sort()

        # Limpiar duplicados
        clean = [targets[0]]
        for t in targets[1:]:
            if abs(t - clean[-1]) > 0.01:
                clean.append(t)
        targets = clean

        self.log(f"  Puntos a generar: {len(targets)}")

        # Interpolar
        result = []
        for idx, tgt in enumerate(targets):
            este, norte = self._interp_pos(points, tgt)
            if este is None:
                continue
            cota = self._interp_profile(profile, tgt)
            result.append({
                'id': idx + 1,
                'este': este,
                'norte': norte,
                'cota': cota,
                'estacion': tgt,
                'estacion_fmt': self._fmt(tgt),
            })

        self.log(f"  Puntos generados: {len(result)}")
        return result

    def _interp_pos(self, points, tgt):
        if tgt <= points[0]['sta']:
            return points[0]['este'], points[0]['norte']
        if tgt >= points[-1]['sta']:
            return points[-1]['este'], points[-1]['norte']

        for i in range(len(points) - 1):
            if points[i]['sta'] <= tgt <= points[i + 1]['sta']:
                sl = points[i + 1]['sta'] - points[i]['sta']
                if sl < 0.001:
                    return points[i]['este'], points[i]['norte']
                t = (tgt - points[i]['sta']) / sl
                e = points[i]['este'] + t * (points[i + 1]['este'] - points[i]['este'])
                n = points[i]['norte'] + t * (points[i + 1]['norte'] - points[i]['norte'])
                return e, n
        return None, None

    def _interp_profile(self, profile, tgt):
        if not profile:
            return None
        if tgt <= profile[0][0]:
            return profile[0][1]
        if tgt >= profile[-1][0]:
            return profile[-1][1]
        for i in range(len(profile) - 1):
            if profile[i][0] <= tgt <= profile[i + 1][0]:
                sl = profile[i + 1][0] - profile[i][0]
                if sl < 0.001:
                    return profile[i][1]
                t = (tgt - profile[i][0]) / sl
                return profile[i][1] + t * (profile[i + 1][1] - profile[i][1])
        return None

    def _discretize_curve(self, seg, num_points=60):
        cx, cy = seg['centerEste'], seg['centerNorte']
        r = seg['radius']
        ang_s = math.atan2(seg['startEste'] - cx, seg['startNorte'] - cy)
        ang_e = math.atan2(seg['endEste'] - cx, seg['endNorte'] - cy)
        if seg['rot'] == 'cw':
            if ang_e >= ang_s: ang_e -= 2 * math.pi
        else:
            if ang_e <= ang_s: ang_e += 2 * math.pi
        pts = []
        for i in range(num_points):
            t = i / (num_points - 1)
            ang = ang_s + t * (ang_e - ang_s)
            pts.append((cx + r * math.sin(ang), cy + r * math.cos(ang)))
        return pts

    def _fmt(self, sta):
        km = int(sta / 1000)
        m = sta - km * 1000
        return f"{km}+{m:07.3f}"


# ============================================================================
# EXPORTAR CSV
# ============================================================================
def export_csv(points, output_path, include_cota=False):
    with open(output_path, 'w', newline='', encoding='utf-8-sig') as f:
        w = csv.writer(f, delimiter=';')
        if include_cota and any(p.get('cota') is not None for p in points):
            w.writerow(['ID', 'ESTE', 'NORTE', 'COTA', 'ESTACION'])
            for pt in points:
                c = f"{pt['cota']:.3f}" if pt['cota'] is not None else ""
                w.writerow([pt['id'], f"{pt['este']:.3f}", f"{pt['norte']:.3f}", c, pt['estacion_fmt']])
        else:
            w.writerow(['ID', 'ESTE', 'NORTE', 'ESTACION'])
            for pt in points:
                w.writerow([pt['id'], f"{pt['este']:.3f}", f"{pt['norte']:.3f}", pt['estacion_fmt']])
    return len(points)


# ============================================================================
# INTERFAZ GRÁFICA
# ============================================================================
class App(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title("Coordenadas desde LandXML  |  TopografiaCivil3d.com")
        self.geometry("750x650")
        self.configure(bg='#1e1e2e')
        self.resizable(False, False)

        self.parser = LandXMLParser()
        self.interpolator = AlignmentInterpolator()

        self.xml_path = tk.StringVar()
        self.interval = tk.IntVar(value=50)
        self.include_vertices = tk.BooleanVar(value=True)
        self.include_cota = tk.BooleanVar(value=True)
        self.selected_alignment = tk.StringVar()

        self._build_ui()

    def _build_ui(self):
        BG = '#1e1e2e'; FG = '#cdd6f4'; ACCENT = '#89b4fa'
        BTN_BG = '#313244'; ENTRY_BG = '#181825'
        GREEN = '#a6e3a1'; YELLOW = '#f9e2af'

        tk.Label(self, text="COORDENADAS DESDE LANDXML",
                 font=('Consolas', 15, 'bold'), fg=ACCENT, bg=BG).pack(anchor='w', padx=20, pady=(15, 0))
        tk.Label(self, text="Interpola coordenadas cada N metros sobre el alineamiento y exporta CSV",
                 font=('Segoe UI', 9), fg='#6c7086', bg=BG).pack(anchor='w', padx=20, pady=(0, 10))

        # ARCHIVO
        f1 = tk.LabelFrame(self, text=" 1. ARCHIVO XML ", font=('Segoe UI', 10, 'bold'),
                           fg=ACCENT, bg=BG, bd=1)
        f1.pack(fill='x', padx=20, pady=5)

        row1 = tk.Frame(f1, bg=BG); row1.pack(fill='x', padx=10, pady=8)
        tk.Label(row1, text="Archivo XML:", font=('Segoe UI', 9, 'bold'),
                 fg=YELLOW, bg=BG, width=14, anchor='w').pack(side='left')
        tk.Entry(row1, textvariable=self.xml_path, font=('Consolas', 9),
                 bg=ENTRY_BG, fg=YELLOW, insertbackground=YELLOW,
                 bd=0, highlightthickness=1, highlightcolor=ACCENT).pack(side='left', fill='x', expand=True, padx=5)
        tk.Button(row1, text="Buscar", command=self._browse_xml,
                  bg=BTN_BG, fg=FG, bd=0, padx=12, cursor='hand2').pack(side='left')

        row_al = tk.Frame(f1, bg=BG); row_al.pack(fill='x', padx=10, pady=(0, 8))
        tk.Label(row_al, text="Alineamiento:", font=('Segoe UI', 9, 'bold'),
                 fg=FG, bg=BG, width=14, anchor='w').pack(side='left')
        self.align_combo = ttk.Combobox(row_al, textvariable=self.selected_alignment,
                                         state='readonly', width=55)
        self.align_combo.pack(side='left', padx=5)

        # OPCIONES
        f2 = tk.LabelFrame(self, text=" 2. OPCIONES ", font=('Segoe UI', 10, 'bold'),
                           fg=ACCENT, bg=BG, bd=1)
        f2.pack(fill='x', padx=20, pady=5)

        int_row = tk.Frame(f2, bg=BG); int_row.pack(fill='x', padx=10, pady=5)
        tk.Label(int_row, text="Intervalo:", font=('Segoe UI', 9, 'bold'),
                 fg=FG, bg=BG).pack(side='left', padx=(0, 15))
        for val in [10, 20, 25, 50, 100, 200]:
            tk.Radiobutton(int_row, text=f"{val}m", variable=self.interval, value=val,
                           font=('Segoe UI', 9), fg=GREEN, bg=BG,
                           selectcolor=ENTRY_BG, activebackground=BG).pack(side='left', padx=3)

        int_row2 = tk.Frame(f2, bg=BG); int_row2.pack(fill='x', padx=10, pady=(0, 5))
        tk.Label(int_row2, text="Personalizado:", font=('Segoe UI', 9),
                 fg='#6c7086', bg=BG).pack(side='left', padx=(95, 5))
        self.custom_interval = tk.Entry(int_row2, font=('Consolas', 9), width=8,
                                         bg=ENTRY_BG, fg=YELLOW, insertbackground=YELLOW, bd=0)
        self.custom_interval.pack(side='left')
        tk.Label(int_row2, text="m", font=('Segoe UI', 8), fg='#585b70', bg=BG).pack(side='left', padx=3)

        opt_row = tk.Frame(f2, bg=BG); opt_row.pack(fill='x', padx=10, pady=5)
        tk.Checkbutton(opt_row, text="Incluir vertices (PIs)",
                       variable=self.include_vertices,
                       font=('Segoe UI', 9), fg=FG, bg=BG,
                       selectcolor=ENTRY_BG, activebackground=BG).pack(side='left', padx=5)
        tk.Checkbutton(opt_row, text="Incluir COTA (si hay rasante)",
                       variable=self.include_cota,
                       font=('Segoe UI', 9), fg=FG, bg=BG,
                       selectcolor=ENTRY_BG, activebackground=BG).pack(side='left', padx=15)

        # BOTON
        tk.Button(self, text="GENERAR CSV", command=self._process,
                  font=('Segoe UI', 13, 'bold'), bg='#89b4fa', fg='#1e1e2e',
                  activebackground='#b4d0fb', bd=0, padx=20, pady=10,
                  cursor='hand2').pack(fill='x', padx=20, pady=10)

        # LOG
        f3 = tk.LabelFrame(self, text=" LOG ", font=('Segoe UI', 10, 'bold'),
                           fg=ACCENT, bg=BG, bd=1)
        f3.pack(fill='both', expand=True, padx=20, pady=(5, 15))

        self.log_text = tk.Text(f3, font=('Consolas', 9), bg=ENTRY_BG, fg=GREEN,
                                insertbackground=GREEN, bd=0, wrap='word')
        sb = tk.Scrollbar(f3, command=self.log_text.yview)
        self.log_text.configure(yscrollcommand=sb.set)
        sb.pack(side='right', fill='y')
        self.log_text.pack(fill='both', expand=True, padx=5, pady=5)

        self._log("=== Coordenadas desde LandXML ===")
        self._log("TopografiaCivil3d.com\n")
        self._log("1. Buscar el archivo XML")
        self._log("2. Elegir intervalo (50, 100, 200...)")
        self._log("3. GENERAR CSV")
        self._log("4. El CSV se guarda junto al XML\n")

    def _log(self, msg):
        self.log_text.insert('end', msg + '\n')
        self.log_text.see('end')
        self.update_idletasks()

    def _browse_xml(self):
        p = filedialog.askopenfilename(
            title="Seleccionar LandXML",
            filetypes=[("XML", "*.xml"), ("Todos", "*.*")])
        if not p: return

        self.xml_path.set(p)
        self._log(f"\nArchivo: {os.path.basename(p)}")

        self.parser = LandXMLParser()
        try:
            n = self.parser.parse(p)
            for m in self.parser.get_and_clear_log(): self._log(m)

            if n > 0:
                names = [a['name'] for a in self.parser.alignments]
                self.align_combo['values'] = names
                self.align_combo.current(0)
                self._log(f"\nListo. Seleccione intervalo y presione GENERAR CSV.")
            else:
                self._log("\nNo se encontraron alineamientos.")
        except Exception as e:
            self._log(f"\nError: {e}")

    def _process(self):
        if not self.parser.alignments:
            messagebox.showerror("Error", "Cargue un archivo XML primero.")
            return

        custom = self.custom_interval.get().strip()
        if custom:
            try:
                interval = float(custom)
                if interval <= 0: raise ValueError()
            except:
                messagebox.showerror("Error", "Intervalo invalido."); return
        else:
            interval = self.interval.get()

        sel_name = self.selected_alignment.get()
        alignment = None
        for a in self.parser.alignments:
            if a['name'] == sel_name:
                alignment = a; break
        if not alignment:
            messagebox.showerror("Error", "Seleccione alineamiento."); return

        self._log(f"\n{'=' * 50}")
        self._log(f"PROCESANDO: {sel_name}")
        self._log(f"{'=' * 50}")

        try:
            self.interpolator = AlignmentInterpolator()
            points = self.interpolator.interpolate(
                alignment, interval, self.include_vertices.get())
            for m in self.interpolator.get_and_clear_log(): self._log(m)

            if not points:
                self._log("\nSin puntos."); return

            xml_dir = os.path.dirname(self.xml_path.get())
            xml_base = os.path.splitext(os.path.basename(self.xml_path.get()))[0]
            csv_name = f"{xml_base}_COORD_{int(interval)}m.csv"
            csv_path = os.path.join(xml_dir, csv_name)

            has_cota = self.include_cota.get()
            export_csv(points, csv_path, include_cota=has_cota)

            self._log(f"\n  CSV: {csv_name}")
            self._log(f"  Puntos: {len(points)}")

            # Muestra
            show_cota = has_cota and any(p.get('cota') is not None for p in points)
            if show_cota:
                self._log(f"\n  {'ID':>4}  {'ESTE':>12}  {'NORTE':>13}  {'COTA':>8}  ESTACION")
            else:
                self._log(f"\n  {'ID':>4}  {'ESTE':>12}  {'NORTE':>13}  ESTACION")

            for pt in points[:15]:
                if show_cota:
                    c = f"{pt['cota']:.3f}" if pt['cota'] is not None else "   -   "
                    self._log(f"  {pt['id']:>4}  {pt['este']:>12.3f}  {pt['norte']:>13.3f}  {c:>8}  {pt['estacion_fmt']}")
                else:
                    self._log(f"  {pt['id']:>4}  {pt['este']:>12.3f}  {pt['norte']:>13.3f}  {pt['estacion_fmt']}")

            if len(points) > 15:
                self._log(f"  ... y {len(points) - 15} puntos mas")

            self._log(f"\n{'=' * 50}")
            self._log("LISTO")
            self._log(f"{'=' * 50}")

            if messagebox.askyesno("Listo",
                    f"{len(points)} puntos exportados a:\n{csv_name}\n\nAbrir carpeta?"):
                if sys.platform == 'win32': os.startfile(xml_dir)

        except Exception as e:
            self._log(f"\nError: {e}")
            import traceback; self._log(traceback.format_exc())


# ============================================================================
if __name__ == '__main__':
    app = App()
    app.mainloop()
