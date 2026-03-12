;; ============================================================
;;  COLOREAR_CAPAS.lsp
;;  Asigna colores Y tipos de linea por grupo a capas C3D
;;  Comando: COLOREARCAPAS
;;  Autor: TopografiaCivil3D
;; ============================================================

(defun c:COLOREARCAPAS ( / )

  (princ "\n========================================")
  (princ "\n  COLOREAR CAPAS C3D - Pipeline")
  (princ "\n========================================")

  ;; --------------------------------------------------------
  ;; Funcion auxiliar: cambiar color de una capa
  ;; --------------------------------------------------------
  (defun set-layer-color ( layname color / )
    (if (tblsearch "LAYER" layname)
      (progn
        (command "_.LAYER" "_Color" color layname "")
        (princ (strcat "\n   OK  " layname " -> color " (itoa color)))
      )
      (princ (strcat "\n   --  (no existe) " layname))
    )
  )

  ;; --------------------------------------------------------
  ;; Funcion auxiliar: cambiar tipo de linea de una capa
  ;; Carga el linetype si no esta cargado todavia
  ;; --------------------------------------------------------
  (defun set-layer-ltype ( layname ltype / )
    (if (tblsearch "LAYER" layname)
      (progn
        (if (not (tblsearch "LTYPE" ltype))
          (command "_.LINETYPE" "_Load" ltype "acadiso.lin" "")
        )
        (command "_.LAYER" "_LType" ltype layname "")
        (princ (strcat "\n   OK  " layname " -> " ltype))
      )
      (princ (strcat "\n   --  (no existe) " layname))
    )
  )

  ;; ========================================================
  ;; GRUPO 1: ALINEAMIENTO — Rojo calido (10) / Continuous
  ;; ========================================================
  (princ "\n\n>> [1] ALINEAMIENTO -> Rojo (10) / Continuous")
  (set-layer-color "C3D_ALIN.EJE"                10)
  (set-layer-color "C3D_CURVA_CIRCULAR"           10)
  (set-layer-color "C3D_CURVAS_VERTI."            10)
  (set-layer-color "C3D_INICIO_TRAMOS"            10)
  (set-layer-color "C3D_LABEL_ALIN"               10)
  (set-layer-color "C3D_LABEL_CRUCES"             10)
  (set-layer-color "C3D_PEND_TUBO"                10)
  (set-layer-color "C3D_ETIQ_ESTACION"            10)
  (set-layer-color "C3D_ETIQUETA...TUBO_TENDIDO"  10)

  (set-layer-ltype "C3D_ALIN.EJE"                "Continuous")
  (set-layer-ltype "C3D_CURVA_CIRCULAR"           "Continuous")
  (set-layer-ltype "C3D_CURVAS_VERTI."            "Continuous")
  (set-layer-ltype "C3D_INICIO_TRAMOS"            "Continuous")
  (set-layer-ltype "C3D_LABEL_ALIN"               "Continuous")
  (set-layer-ltype "C3D_LABEL_CRUCES"             "Continuous")
  (set-layer-ltype "C3D_PEND_TUBO"                "Continuous")
  (set-layer-ltype "C3D_ETIQ_ESTACION"            "Continuous")

  ;; ========================================================
  ;; GRUPO 2: PERFIL — Amarillo (2) / Continuous
  ;; ========================================================
  (princ "\n\n>> [2] PERFIL -> Amarillo (2) / Continuous")
  (set-layer-color "C3D_PERFIL"              2)
  (set-layer-color "C3D_PROFILE_BASICO"      2)
  (set-layer-color "C3D_PROFILE_PREDOB."     2)
  (set-layer-color "C3D_PROFILE_VIEW"        2)
  (set-layer-color "C3D_PROFILE_VIEW_LABEL"  2)
  (set-layer-color "C3D_RASANTE"             2)
  (set-layer-color "C3D_RASANTE_DISEÑO"      2)
  (set-layer-color "C3D_TEXTO_PERFIL"        2)

  (set-layer-ltype "C3D_PERFIL"              "Continuous")
  (set-layer-ltype "C3D_PROFILE_BASICO"      "Continuous")
  (set-layer-ltype "C3D_PROFILE_PREDOB."     "Continuous")
  (set-layer-ltype "C3D_PROFILE_VIEW"        "Continuous")
  (set-layer-ltype "C3D_PROFILE_VIEW_LABEL"  "Continuous")
  (set-layer-ltype "C3D_RASANTE"             "Continuous")
  (set-layer-ltype "C3D_RASANTE_DISEÑO"      "Continuous")
  (set-layer-ltype "C3D_TEXTO_PERFIL"        "Continuous")

  ;; ========================================================
  ;; GRUPO 3: PUNTOS COGO — Gris (253) / Continuous
  ;; ========================================================
  (princ "\n\n>> [3] PUNTOS COGO -> Gris (253) / Continuous")
  (set-layer-color "C3D_PTS_ARROYOS"            253)
  (set-layer-color "C3D_PTS_AUTOPISTA"          253)
  (set-layer-color "C3D_PTS_CAMINOS"            253)
  (set-layer-color "C3D_PTS_CANALES"            253)
  (set-layer-color "C3D_PTS_CATAS"              253)
  (set-layer-color "C3D_PTS_CERCAS"             253)
  (set-layer-color "C3D_PTS_CUNETA"             253)
  (set-layer-color "C3D_PTS_ESTACION"           253)
  (set-layer-color "C3D_PTS_FERROCARRIL"        253)
  (set-layer-color "C3D_PTS_FIBRA_OPTICA"       253)
  (set-layer-color "C3D_PTS_LINEA_AGUA"         253)
  (set-layer-color "C3D_PTS_LINEA_EXISTENTE"    253)
  (set-layer-color "C3D_PTS_RIO"                253)
  (set-layer-color "C3D_PTS_TUBERIA_EXISTENTE"  253)
  (set-layer-color "C3D_PTS_VIA_PAVIMENTADA"    253)
  (set-layer-color "C3D_PUNTOS_T...ENO_NATURAL" 253)

  (set-layer-ltype "C3D_PTS_ARROYOS"            "Continuous")
  (set-layer-ltype "C3D_PTS_AUTOPISTA"          "Continuous")
  (set-layer-ltype "C3D_PTS_CAMINOS"            "Continuous")
  (set-layer-ltype "C3D_PTS_CANALES"            "Continuous")
  (set-layer-ltype "C3D_PTS_CATAS"              "Continuous")
  (set-layer-ltype "C3D_PTS_CERCAS"             "Continuous")
  (set-layer-ltype "C3D_PTS_CUNETA"             "Continuous")
  (set-layer-ltype "C3D_PTS_ESTACION"           "Continuous")
  (set-layer-ltype "C3D_PTS_FERROCARRIL"        "Continuous")
  (set-layer-ltype "C3D_PTS_FIBRA_OPTICA"       "Continuous")
  (set-layer-ltype "C3D_PTS_LINEA_AGUA"         "Continuous")
  (set-layer-ltype "C3D_PTS_LINEA_EXISTENTE"    "Continuous")
  (set-layer-ltype "C3D_PTS_RIO"                "Continuous")
  (set-layer-ltype "C3D_PTS_TUBERIA_EXISTENTE"  "Continuous")
  (set-layer-ltype "C3D_PTS_VIA_PAVIMENTADA"    "Continuous")
  (set-layer-ltype "C3D_PUNTOS_T...ENO_NATURAL" "Continuous")

  ;; ========================================================
  ;; GRUPO 4: TIN / SUPERFICIE — Verde (90) / DASHED
  ;; ========================================================
  (princ "\n\n>> [4] TIN / SUPERFICIE -> Verde (90) / DASHED")
  (set-layer-color "C3D_TIN_SUPERFICIE"    90)
  (set-layer-color "C3D_TIN_EXISTENTE"     90)
  (set-layer-color "C3D_TIN_SUPERF_LABEL"  90)

  (set-layer-ltype "C3D_TIN_SUPERFICIE"    "DASHED")
  (set-layer-ltype "C3D_TIN_EXISTENTE"     "DASHED")
  (set-layer-ltype "C3D_TIN_SUPERF_LABEL"  "DASHED")

  ;; ========================================================
  ;; GRUPO 5: TUBO TENDIDO — Cyan (4) / Continuous
  ;; ========================================================
  (princ "\n\n>> [5] TUBO TENDIDO -> Cyan (4) / Continuous")
  (set-layer-color "C3D_TUBO_TENDIDO"   4)
  (set-layer-color "C3D_TUBOS_TENDIDOS" 4)
  (set-layer-color "C3D_FEATURE_LINE"   4)

  (set-layer-ltype "C3D_TUBO_TENDIDO"   "Continuous")
  (set-layer-ltype "C3D_TUBOS_TENDIDOS" "Continuous")
  (set-layer-ltype "C3D_FEATURE_LINE"   "Continuous")

  ;; ========================================================
  ;; GRUPO 6: SECUNDARIO — Gris oscuro (251) / Continuous
  ;; ========================================================
  (princ "\n\n>> [6] SECUNDARIO -> Gris (251) / Continuous")
  (set-layer-color "C3D_CATAS"       251)
  (set-layer-color "C3D_FONDO_ZANJA" 251)

  (set-layer-ltype "C3D_CATAS"       "Continuous")
  (set-layer-ltype "C3D_FONDO_ZANJA" "Continuous")

  ;; --------------------------------------------------------
  ;; Regenerar para aplicar cambios
  ;; --------------------------------------------------------
  (command "_.REGEN")

  (princ "\n\n========================================")
  (princ "\n  RESUMEN FINAL:")
  (princ "\n  Rojo  (10)  Continuous -> Alineamiento")
  (princ "\n  Amari (2)   Continuous -> Perfiles")
  (princ "\n  Gris  (253) Continuous -> Puntos COGO")
  (princ "\n  Verde (90)  DASHED     -> TIN/Superficie")
  (princ "\n  Cyan  (4)   Continuous -> Tubo tendido")
  (princ "\n  Gris  (251) Continuous -> Secundario")
  (princ "\n========================================")
  (princ "\n  Listo.")
  (princ "\n========================================")
  (princ)
)

(princ "\n>> COLOREAR_CAPAS cargado.")
(princ "\n   Comando: COLOREARCAPAS")
(princ)
