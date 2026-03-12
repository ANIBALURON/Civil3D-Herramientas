;; ============================================================
;;  ORDENAR_DIBUJO.lsp
;;
;;  FONDO:  Xrefs, Profiles (AECC_PROFILE), hatches, geometria
;;  ARRIBA: Alineamientos (AECC_ALIGNMENT) + Polilineas
;;
;;  Comando: ORDENAR
;;  Autor: TopografiaCivil3D
;; ============================================================

(defun c:ORDENAR ( / ss i ent blkname blkdef
                     ss_xref ss_profiles ss_hatches
                     ss_ali ss_poli )

  (princ "\n========================================")
  (princ "\n  ORDENAR DIBUJO - Iniciando...")
  (princ "\n========================================")

  ;; --------------------------------------------------------
  ;; PASO 1: Xrefs al fondo
  ;; --------------------------------------------------------
  (princ "\n>> [1/5] Xrefs al fondo...")
  (setq ss_xref (ssget "X" '((0 . "INSERT") (410 . "Model"))))
  (if ss_xref
    (progn
      (setq i 0)
      (while (< i (sslength ss_xref))
        (setq ent     (ssname ss_xref i)
              blkname (cdr (assoc 2 (entget ent)))
              blkdef  (tblsearch "BLOCK" blkname))
        (if (and blkdef
                 (/= 0 (logand (cdr (assoc 70 blkdef)) 4)))
          (command "_.DRAWORDER" ent "" "_B"))
        (setq i (1+ i))
      )
      (princ (strcat " OK (" (itoa (sslength ss_xref)) " inserts revisados)"))
    )
    (princ " Sin xrefs.")
  )

  ;; --------------------------------------------------------
  ;; PASO 2: Profiles al fondo
  ;; --------------------------------------------------------
  (princ "\n>> [2/5] Profiles al fondo...")
  (setq ss_profiles (ssget "X" '((-4 . "<OR")
                                    (0 . "AECC_PROFILE")
                                    (0 . "AECC_PROFILE_VIEW")
                                    (0 . "AECC_PROFILE_VIEW_BAND_SET")
                                  (-4 . "OR>")
                                  (410 . "Model"))))
  (if ss_profiles
    (progn
      (setq i 0)
      (while (< i (sslength ss_profiles))
        (command "_.DRAWORDER" (ssname ss_profiles i) "" "_B")
        (setq i (1+ i))
      )
      (princ (strcat " OK (" (itoa (sslength ss_profiles)) " profiles al fondo)"))
    )
    (princ " Sin profiles.")
  )

  ;; --------------------------------------------------------
  ;; PASO 3: Hatches y geometria basica al fondo
  ;; --------------------------------------------------------
  (princ "\n>> [3/5] Hatches y geometria al fondo...")
  (setq ss_hatches (ssget "X" '((-4 . "<OR")
                                   (0 . "HATCH")
                                   (0 . "TEXT")
                                   (0 . "MTEXT")
                                   (0 . "LINE")
                                   (0 . "ARC")
                                   (0 . "CIRCLE")
                                   (0 . "SPLINE")
                                   (0 . "SOLID")
                                 (-4 . "OR>")
                                 (410 . "Model"))))
  (if ss_hatches
    (progn
      (command "_.DRAWORDER" ss_hatches "" "_B")
      (princ (strcat " OK (" (itoa (sslength ss_hatches)) " objetos)"))
    )
    (princ " Nada.")
  )

  ;; --------------------------------------------------------
  ;; PASO 4: Polilineas al frente primero
  ;; --------------------------------------------------------
  (princ "\n>> [4/5] Polilineas al frente...")
  (setq ss_poli (ssget "X" '((-4 . "<OR")
                                (0 . "LWPOLYLINE")
                                (0 . "POLYLINE")
                                (0 . "2DPOLYLINE")
                                (0 . "3DPOLYLINE")
                              (-4 . "OR>")
                              (410 . "Model"))))
  (if ss_poli
    (progn
      (command "_.DRAWORDER" ss_poli "" "_F")
      (princ (strcat " OK (" (itoa (sslength ss_poli)) " polilineas)"))
    )
    (princ " Sin polilineas.")
  )

  ;; --------------------------------------------------------
  ;; PASO 5: Alineamientos al frente (ULTIMO = ENCIMA DE TODO)
  ;; --------------------------------------------------------
  (princ "\n>> [5/5] Alineamientos al frente (encima de todo)...")
  (setq ss_ali (ssget "X" '((0 . "AECC_ALIGNMENT") (410 . "Model"))))
  (if ss_ali
    (progn
      (setq i 0)
      (while (< i (sslength ss_ali))
        (command "_.DRAWORDER" (ssname ss_ali i) "" "_F")
        (setq i (1+ i))
      )
      (princ (strcat " OK (" (itoa (sslength ss_ali)) " alineamientos)"))
    )
    (princ " ADVERTENCIA: No se encontraron alineamientos AECC_ALIGNMENT.")
  )

  ;; --------------------------------------------------------
  ;; Regenerar
  ;; --------------------------------------------------------
  (command "_.REGEN")

  (princ "\n========================================")
  (princ "\n  FONDO  : Xrefs / Profiles / Hatches")
  (princ "\n  MEDIO  : Polilineas")
  (princ "\n  TOP    : Alineamientos (encima de todo)")
  (princ "\n  Listo.")
  (princ "\n========================================")
  (princ)
)

;; ============================================================
;;  AYUDA: detectar tipo exacto de objeto Civil 3D
;;  Comando: QUETIPO
;;  Haz clic en cualquier objeto para saber su tipo DXF
;; ============================================================

(defun c:QUETIPO ( / ent ed )
  (princ "\n>> Haz clic en el objeto que quieres identificar:")
  (setq ent (car (entsel)))
  (if ent
    (progn
      (setq ed (entget ent))
      (princ (strcat "\n   Tipo de objeto (DXF 0): "
                     (cdr (assoc 0 ed))))
      (princ (strcat "\n   Capa: "
                     (cdr (assoc 8 ed))))
    )
    (princ "\n   No se selecciono nada.")
  )
  (princ)
)

(princ "\n>> ORDENAR_DIBUJO cargado.")
(princ "\n   ORDENAR  -> Ordena todo el dibujo automaticamente")
(princ "\n   QUETIPO  -> Identifica el tipo DXF de cualquier objeto")
(princ)
