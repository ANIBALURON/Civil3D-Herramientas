;;; ================================================================
;;; CAJAPERF.lsp  -  Cajas de Perforación Horizontal
;;; Proyecto: CUXTAL II / Energía Mayakán
;;; Autor: generado para Aníbal - TopografiaCivil3D.com
;;;
;;; COMANDO: CAJAPERF
;;; ================================================================

(defun C:CAJAPERF (/ *error*
  nombre_cruce _pfx
  pts_eje largo ancho sep_ref lado_ch lado_gr elevacion
  st_inicio st_ataque st_recepcion
  d_ataque d_recepcion
  res_at pt_at ang_at
  res_rec pt_rec ang_rec
  layer_caja layer_txt layer_eje
  r_marca alt_txt
  _modo_eje _n _sig _pt _ss _ent _edata _p
  _cen_at _cen_rec
  csv_path csv_existe csv_file csv_linea)

  (defun *error* (msg)
    (if (not (member msg '("Function cancelled" "quit / exit abort")))
      (princ (strcat "\nError: " msg)))
    (princ))

  (defun _capa (nombre color)
    (if (not (tblsearch "LAYER" nombre))
      (entmake (list '(0 . "LAYER") '(100 . "AcDbSymbolTableRecord")
        '(100 . "AcDbLayerTableRecord")
        (cons 2 nombre) '(70 . 0) (cons 62 color) '(6 . "Continuous")))))

  (defun _len (p1 p2 / dx dy)
    (setq dx (- (car p2) (car p1)) dy (- (cadr p2) (cadr p1)))
    (sqrt (+ (* dx dx) (* dy dy))))

  (defun _ang (p1 p2)
    (atan (- (cadr p2) (cadr p1)) (- (car p2) (car p1))))

  (defun _pt_en_eje (pts dist / acum i p1 p2 ls t_ res)
    (setq acum 0.0 i 0 res nil)
    (while (and (null res) (< i (1- (length pts))))
      (setq p1 (nth i pts) p2 (nth (1+ i) pts) ls (_len p1 p2))
      (if (<= dist (+ acum ls))
        (progn
          (setq t_ (/ (- dist acum) ls))
          (setq res (list
            (+ (car p1) (* t_ (- (car p2) (car p1))))
            (+ (cadr p1) (* t_ (- (cadr p2) (cadr p1))))
            (_ang p1 p2))))
        (setq acum (+ acum ls) i (1+ i))))
    (if (null res)
      (setq res (list (car (last pts)) (cadr (last pts))
        (_ang (nth (- (length pts) 2) pts) (last pts)))))
    res)

  (defun _rpt (cx cy dl dt ang elev)
    (list (+ cx (- (* dl (cos ang)) (* dt (sin ang))))
          (+ cy (+ (* dl (sin ang)) (* dt (cos ang))))
          elev))

  (defun _caja (ref ang d_may d_men d_der d_izq capa elev / cx cy c1 c2 c3 c4)
    (setq cx (car ref) cy (cadr ref))
    (setq c1 (_rpt cx cy  d_may       d_der   ang elev)
          c2 (_rpt cx cy  d_may    (- d_izq)  ang elev)
          c3 (_rpt cx cy (- d_men) (- d_izq)  ang elev)
          c4 (_rpt cx cy (- d_men)    d_der   ang elev))
    (entmake (list '(0 . "POLYLINE") '(100 . "AcDbEntity")
      (cons 8 capa) (cons 62 1) '(100 . "AcDb3dPolyline") '(70 . 9)))
    (foreach _v (list c1 c2 c3 c4)
      (entmake (list '(0 . "VERTEX") '(100 . "AcDbEntity")
        (cons 8 capa) '(100 . "AcDb3dPolylineVertex")
        (cons 10 _v) '(70 . 32))))
    (entmake '((0 . "SEQEND"))))

  (defun _txt (pt texto alt ang capa color)
    (entmake (list '(0 . "TEXT") '(100 . "AcDbEntity")
      (cons 8 capa) (cons 62 color) '(100 . "AcDbText")
      (cons 10 pt) (cons 40 alt) (cons 1 texto) (cons 50 ang) (cons 72 1)
      '(100 . "AcDbText") (cons 11 pt) (cons 73 0))))

  (defun _circ (centro radio capa color)
    (entmake (list '(0 . "CIRCLE") '(100 . "AcDbEntity")
      (cons 8 capa) (cons 62 color) '(100 . "AcDbCircle")
      (cons 10 centro) (cons 40 radio))))

  ;; Formatear KP: 59076.84 → "59+076.84"
  (defun _kp (val / km m)
    (setq km (fix (/ val 1000.0))
          m  (- val (* km 1000.0)))
    (strcat (itoa km) "+" (rtos m 2 2)))

  ;; Limpiar nombre para capa (sin caracteres inválidos)
  (defun _limpiar (s / i c res validos)
    (setq res "" i 0)
    (while (< i (strlen s))
      (setq i (1+ i) c (substr s i 1))
      (if (member c '(" " "/" "\\" ":" "*" "?" "\"" "<" ">" "|"))
        (setq c "-"))
      (setq res (strcat res c)))
    (strcase res))

  ;; ════════════════════════════════════════════════════════
  (princ "\n╔══════════════════════════════════════════════╗\n")
  (princ   "║   CAJAS DE PERFORACIÓN HORIZONTAL            ║\n")
  (princ   "║   Mayakán CUXTAL II                          ║\n")
  (princ   "╚══════════════════════════════════════════════╝\n")

  ;; ── PASO 0: Nombre del cruce ───────────────────────────
  (princ "\nPASO 0 ─ Nombre del cruce (ej: CARR-188, ARROYO-NORTE)")
  (initget 1)
  (setq nombre_cruce (getstring T "\n  Nombre del cruce: "))
  (setq _pfx (strcat "PERF-" (_limpiar nombre_cruce)))

  (setq layer_caja (strcat _pfx "-CAJAS")
        layer_txt  (strcat _pfx "-TEXTOS")
        layer_eje  (strcat _pfx "-EJE"))
  (_capa layer_caja 1)
  (_capa layer_txt  2)
  (_capa layer_eje  2)
  (princ (strcat "\n  ✓ Capas: " layer_caja " | " layer_txt " | " layer_eje))

  ;; ── PASO 1: Eje de la tubería ──────────────────────────
  (princ "\n\nPASO 1 ─ EJE de la tubería")
  (princ "\n         S = Seleccionar polilínea existente")
  (princ "\n         C = Ingresar coordenadas manualmente")
  (initget 1 "S C")
  (setq _modo_eje (getkword "\n  Opción [S/C]: "))

  (if (= _modo_eje "S")
    (progn
      (setq _ss nil)
      (while (null _ss)
        (setq _ent (car (entsel "\n  Selecciona la polilínea del eje: ")))
        (if _ent
          (progn
            (setq _edata (entget _ent))
            (if (member (cdr (assoc 0 _edata)) '("LWPOLYLINE" "POLYLINE"))
              (progn
                (setq pts_eje '())
                (foreach _p _edata
                  (if (= (car _p) 10)
                    (setq pts_eje (append pts_eje
                      (list (list (cadr _p) (caddr _p) 0.0))))))
                (setq _ss T)
                (princ (strcat "\n  ✓ " (itoa (length pts_eje)) " vértices.")))
              (princ "\n  ✗ No es polilínea. Intenta de nuevo.")))
          (princ "\n  ✗ Nada seleccionado."))))
    (progn
      (princ "\n  Ingresa puntos del eje. ENTER vacío para terminar.\n")
      (setq pts_eje '() _n 0 _sig T)
      (while _sig
        (setq _n (1+ _n))
        (setq _pt (getpoint (strcat "\n  Punto " (itoa _n) " [ENTER=terminar]: ")))
        (if (null _pt) (setq _sig nil)
          (setq pts_eje (append pts_eje (list _pt)))))
      (if (< (length pts_eje) 2)
        (progn (princ "\n✗ Se necesitan al menos 2 puntos.") (exit)))))

  ;; ── PASO 2: Cadenamiento inicial ──────────────────────
  (princ "\n\nPASO 2 ─ Cadenamiento del PRIMER punto del eje")
  (princ "\n         (KM 58+500 → escribe 58500)")
  (initget 7)
  (setq st_inicio (getreal "\n  Estación inicial (metros): "))

  ;; ── PASO 3: Estaciones de cruce ───────────────────────
  (princ "\n\nPASO 3 ─ Estaciones de cruce (KM 59+076.84 → 59076.84)")
  (initget 7)
  (setq st_ataque    (getreal "\n  Estación POZO DE ATAQUE   : "))
  (initget 7)
  (setq st_recepcion (getreal "\n  Estación POZO DE RECEPCIÓN: "))

  ;; ── PASO 4: Dimensiones ───────────────────────────────
  (princ "\n\nPASO 4 ─ Dimensiones")
  (initget 7) (setq largo (getreal "\n  LARGO de la caja (ej: 19.0): "))
  (initget 7) (setq ancho (getreal "\n  ANCHO de la caja (ej:  5.0): "))
  (initget 7) (setq lado_ch (getreal "\n  Metros a la DERECHA del eje (ej: 1.5): "))
  (setq lado_gr (- ancho lado_ch))
  (initget 7) (setq elevacion (getreal "\n  ELEVACIÓN de la caja (ej: 14.50): "))

  (setq sep_ref 2.0
        alt_txt 0.50
        r_marca 0.30)

  ;; ── Polilínea 3D del eje (solo modo C) ────────────────
  (if (= _modo_eje "C")
    (progn
      (entmake (list '(0 . "POLYLINE") '(100 . "AcDbEntity")
        (cons 8 layer_eje) (cons 62 2)
        '(100 . "AcDb3dPolyline") '(70 . 8)))
      (foreach _p pts_eje
        (entmake (list '(0 . "VERTEX") '(100 . "AcDbEntity")
          (cons 8 layer_eje) '(100 . "AcDb3dPolylineVertex")
          (cons 10 (list (car _p) (cadr _p) elevacion)) '(70 . 32))))
      (entmake '((0 . "SEQEND")))))

  ;; ── PASO 5: Puntos en el eje ───────────────────────────
  (setq d_ataque    (- st_ataque    st_inicio)
        d_recepcion (- st_recepcion st_inicio))

  (if (< d_ataque 0.0)    (progn (princ "\n✗ KP ataque < inicio del eje.")    (exit)))
  (if (< d_recepcion 0.0) (progn (princ "\n✗ KP recepción < inicio del eje.") (exit)))

  (setq res_at  (_pt_en_eje pts_eje d_ataque)
        res_rec (_pt_en_eje pts_eje d_recepcion)
        pt_at   (list (car res_at)  (cadr res_at)  elevacion)
        ang_at  (caddr res_at)
        pt_rec  (list (car res_rec) (cadr res_rec) elevacion)
        ang_rec (caddr res_rec))

  ;; ── PASO 6: CAJA ATAQUE ───────────────────────────────
  (_caja pt_at ang_at sep_ref (- largo sep_ref) lado_gr lado_ch layer_caja elevacion)
  (_circ pt_at r_marca layer_txt 1)
  (setq _cen_at (_rpt (car pt_at) (cadr pt_at)
    (- sep_ref (* 0.5 largo)) 0.0 ang_at elevacion))
  (_txt _cen_at
    (strcat "POZO DE ATAQUE - " nombre_cruce "\nKM " (_kp st_ataque))
    alt_txt ang_at layer_txt 2)

  ;; ── PASO 7: CAJA RECEPCIÓN ────────────────────────────
  (_caja pt_rec ang_rec (- largo sep_ref) sep_ref lado_ch lado_gr layer_caja elevacion)
  (_circ pt_rec r_marca layer_txt 1)
  (setq _cen_rec (_rpt (car pt_rec) (cadr pt_rec)
    (* 0.5 (- largo sep_ref)) 0.0 ang_rec elevacion))
  (_txt _cen_rec
    (strcat "POZO DE RECEPCIÓN - " nombre_cruce "\nKM " (_kp st_recepcion))
    alt_txt ang_rec layer_txt 2)

  ;; ── PASO 8: EXPORTAR CSV ──────────────────────────────
  (princ "\n\nPASO 8 ─ Guardar datos en Excel (CSV)")
  (setq csv_path
    (getfiled "Guardar registro de perforaciones"
              (strcat (vl-filename-directory (getvar "DWGPREFIX"))
                      "PERFORACIONES_CUXTAL2.csv")
              "csv"
              1))   ; 1 = permite escribir nombre nuevo

  (if (null csv_path)
    (princ "\n  ⚠ Exportación cancelada por el usuario.")
    (progn
      (setq csv_existe (findfile csv_path))
      (setq csv_file   (open csv_path "a"))))

  (if (and csv_path csv_file)
    (progn
      (if (not csv_existe)
        (write-line
          "CRUCE;KP_ATAQUE;ESTE_ATAQUE;NORTE_ATAQUE;ELEV_ATAQUE;KP_RECEPCION;ESTE_RECEPCION;NORTE_RECEPCION;ELEV_RECEPCION;LARGO(m);ANCHO(m);DERECHA(m);IZQUIERDA(m);DIST_PERF(m);CAPA"
          csv_file))
      (write-line
        (strcat nombre_cruce                                    ";"
                (_kp st_ataque)                                 ";"
                (rtos (car  pt_at)  2 3)                        ";"
                (rtos (cadr pt_at)  2 3)                        ";"
                (rtos (caddr pt_at) 2 3)                        ";"
                (_kp st_recepcion)                              ";"
                (rtos (car  pt_rec)  2 3)                       ";"
                (rtos (cadr pt_rec)  2 3)                       ";"
                (rtos (caddr pt_rec) 2 3)                       ";"
                (rtos largo     2 2)                            ";"
                (rtos ancho     2 2)                            ";"
                (rtos lado_ch   2 2)                            ";"
                (rtos lado_gr   2 2)                            ";"
                (rtos (abs (- st_recepcion st_ataque)) 2 2)     ";"
                layer_caja)
        csv_file)
      (close csv_file)
      (princ (strcat "\n  ✓ Exportado a: " csv_path))))

  ;; ── Zoom ──────────────────────────────────────────────
  (command "_.ZOOM" "EXTENTS")

  (princ (strcat
    "\n\n╔══════════════════════════════════════════════╗"
    "\n║  ✓ CRUCE [" nombre_cruce "] DIBUJADO"
    "\n╠══════════════════════════════════════════════╣"
    "\n║  Pozo Ataque   : KM " (_kp st_ataque)
    "\n║  Pozo Recepción: KM " (_kp st_recepcion)
    "\n║  Dist. Perf.   : " (rtos (abs (- st_recepcion st_ataque)) 2 2) " m"
    "\n║  Caja          : " (rtos largo 2 2) "m x " (rtos ancho 2 2) "m"
    "\n║  Elevación     : " (rtos elevacion 2 2) " m"
    "\n║  Derecha / Izq : " (rtos lado_ch 2 2) "m / " (rtos lado_gr 2 2) "m"
    "\n╚══════════════════════════════════════════════╝"))
  (princ))
