;; ============================================================
;;  VISTAS_KP.lsp
;;  Navegacion rapida entre vistas con ventana grafica DCL
;;
;;  Comandos:
;;    GV      -> Guardar vista actual
;;    RV      -> Recuperar vista (solo las tuyas)
;;    BV      -> Borrar una vista tuya
;;    LV      -> Listar en consola
;;    LIMPIAR -> Ver Y borrar TODAS las vistas (incluso Civil 3D)
;;
;;  Autor: TopografiaCivil3D
;; ============================================================


;; ============================================================
;;  FUNCION: Obtener TODAS las vistas sin filtro
;; ============================================================
(defun get-all-views ( / entry lst )
  (setq lst '())
  (setq entry (tblnext "VIEW" T))
  (while entry
    (setq lst (append lst (list (cdr (assoc 2 entry)))))
    (setq entry (tblnext "VIEW"))
  )
  lst
)


;; ============================================================
;;  FUNCION: Obtener solo vistas del usuario (modelo)
;;  Filtra vistas de paper space y prefijos de Civil 3D
;; ============================================================
(defun get-model-views ( / entry lst nombre flags )
  (setq lst '())
  (setq entry (tblnext "VIEW" T))
  (while entry
    (setq nombre (cdr (assoc 2 entry)))
    (setq flags  (cdr (assoc 70 entry)))
    (if (and
          ;; Excluir paper space (flag 1)
          (or (not flags) (= (logand flags 1) 0))
          ;; Excluir prefijos conocidos de Civil 3D / AEC
          (not (wcmatch nombre
                 (strcat "*CIVIL*,*civil*,*_AEC*,*AEC_*,"
                         "*Profile*,*PROFILE*,"
                         "*Alignment*,*ALIGNMENT*,"
                         "*Section*,*SECTION*,"
                         "*Corridor*,*CORRIDOR*,"
                         "_*,*_VIEW,*View*")))
        )
      (setq lst (append lst (list nombre)))
    )
    (setq entry (tblnext "VIEW"))
  )
  lst
)


;; ============================================================
;;  FUNCION: Escribir archivo DCL temporal
;; ============================================================
(defun write-dcl-file ( dcl-path titulo boton-ok / f )
  (setq f (open dcl-path "w"))
  (write-line "vistas_dlg : dialog {" f)
  (write-line (strcat "  label = \"" titulo "\";") f)
  (write-line "  : list_box {" f)
  (write-line "    key = \"lst_vistas\";" f)
  (write-line "    label = \"Selecciona una vista:\";" f)
  (write-line "    width = 45;" f)
  (write-line "    height = 14;" f)
  (write-line "    multiple_select = false;" f)
  (write-line "  }" f)
  (write-line "  spacer;" f)
  (write-line "  : row {" f)
  (write-line (strcat "    : button { key = \"btn_ok\"; label = \"" boton-ok "\"; is_default = true; width = 20; }") f)
  (write-line "    : button { key = \"btn_cancel\"; label = \"  Cancelar  \"; is_cancel = true; width = 20; }" f)
  (write-line "  }" f)
  (write-line "}" f)
  (close f)
)


;; ============================================================
;;  FUNCION GENERICA: Mostrar dialogo y retornar vista elegida
;; ============================================================
(defun show-view-dialog ( vistas titulo boton / dcl-path dcl-id seleccion resultado nombre )

  (setq dcl-path (strcat (getvar "TEMPPREFIX") "vistas_kp.dcl"))
  (write-dcl-file dcl-path titulo boton)

  (setq dcl-id (load_dialog dcl-path))
  (if (not (new_dialog "vistas_dlg" dcl-id))
    (progn (princ "\n   Error al abrir dialogo.") (exit))
  )

  (start_list "lst_vistas")
  (foreach v vistas (add_list v))
  (end_list)
  (set_tile "lst_vistas" "0")
  (setq seleccion 0)

  (action_tile "lst_vistas"
    "(setq seleccion (atoi (get_tile \"lst_vistas\")))"
  )
  (action_tile "btn_ok"
    "(setq seleccion (atoi (get_tile \"lst_vistas\"))) (done_dialog 1)"
  )
  (action_tile "accept"
    "(setq seleccion (atoi (get_tile \"lst_vistas\"))) (done_dialog 1)"
  )
  (action_tile "btn_cancel" "(done_dialog 0)")

  (setq resultado (start_dialog))
  (unload_dialog dcl-id)

  (if (= resultado 1)
    (nth seleccion vistas)
    nil
  )
)


;; ============================================================
;;  GV - GUARDAR VISTA ACTUAL
;; ============================================================
(defun c:GV ( / nombre )
  (princ "\n>> GUARDAR VISTA ACTUAL")
  (princ "\n   Ejemplos: KP5+500  CRUCE-RIO  INICIO-T3")
  (setq nombre (getstring "\n   Nombre para esta vista: "))
  (if (and nombre (/= nombre ""))
    (progn
      (command "_.VIEW" "_Save" nombre)
      (princ (strcat "\n   OK Vista guardada: [" nombre "]"))
      (princ "\n   Usa RV para recuperarla cuando quieras.")
    )
    (princ "\n   Cancelado.")
  )
  (princ)
)


;; ============================================================
;;  RV - RECUPERAR VISTA (solo las del usuario)
;; ============================================================
(defun c:RV ( / vistas nombre )
  (setq vistas (get-model-views))
  (if (null vistas)
    (progn
      (princ "\n   No tienes vistas guardadas.")
      (princ "\n   Usa GV para guardar tu posicion actual.")
      (princ)
      (exit)
    )
  )
  (setq nombre (show-view-dialog vistas
                 "Ir a Vista  |  Doble clic o selecciona y OK"
                 "   IR A VISTA   "))
  (if nombre
    (progn
      (command "_.VIEW" "_Restore" nombre)
      (princ (strcat "\n   OK Vista restaurada: [" nombre "]"))
    )
    (princ "\n   Cancelado.")
  )
  (princ)
)


;; ============================================================
;;  BV - BORRAR VISTA (solo las del usuario)
;; ============================================================
(defun c:BV ( / vistas nombre )
  (setq vistas (get-model-views))
  (if (null vistas)
    (progn
      (princ "\n   No hay vistas tuyas para borrar.")
      (princ "\n   Para borrar vistas de Civil 3D usa: LIMPIAR")
      (princ)
      (exit)
    )
  )
  (setq nombre (show-view-dialog vistas
                 "Borrar Vista Guardada"
                 "     BORRAR     "))
  (if nombre
    (progn
      (command "_.VIEW" "_Delete" nombre)
      (princ (strcat "\n   OK Vista eliminada: [" nombre "]"))
    )
    (princ "\n   Cancelado.")
  )
  (princ)
)


;; ============================================================
;;  LIMPIAR - VER Y BORRAR TODAS LAS VISTAS SIN FILTRO
;;  Incluye las generadas por Civil 3D
;; ============================================================
(defun c:LIMPIAR ( / vistas nombre seguir )
  (princ "\n>> LIMPIAR VISTAS")
  (princ "\n   Mostrando TODAS las vistas incluyendo Civil 3D...")

  (setq seguir T)

  (while seguir
    (setq vistas (get-all-views))

    (if (null vistas)
      (progn
        (princ "\n   No quedan vistas en el dibujo.")
        (setq seguir nil)
      )
      (progn
        (setq nombre (show-view-dialog vistas
                       (strcat "LIMPIAR - Todas las vistas (" (itoa (length vistas)) " total)")
                       "     BORRAR ESTA     "))
        (if nombre
          (progn
            (command "_.VIEW" "_Delete" nombre)
            (princ (strcat "\n   Eliminada: [" nombre "]"))
            ;; Preguntar si quiere seguir borrando
            (initget "Si No")
            (setq resp (getkword "\n   Borrar otra? [Si/No] <Si>: "))
            (if (= resp "No")
              (setq seguir nil)
            )
          )
          (setq seguir nil)
        )
      )
    )
  )
  (princ "\n   Limpieza terminada.")
  (princ)
)


;; ============================================================
;;  LV - LISTAR EN CONSOLA
;; ============================================================
(defun c:LV ( / vistas todas i )
  (setq vistas (get-model-views))
  (setq todas  (get-all-views))
  (princ "\n========================================")
  (princ "\n  TUS VISTAS:")
  (princ "\n========================================")
  (if vistas
    (progn
      (setq i 1)
      (foreach v vistas
        (princ (strcat "\n  [" (itoa i) "] " v))
        (setq i (1+ i))
      )
    )
    (princ "\n  Ninguna todavia - usa GV para crear.")
  )
  (princ (strcat "\n----------------------------------------"
                 "\n  Total en dibujo (con Civil 3D): "
                 (itoa (length todas))))
  (princ "\n========================================")
  (princ)
)


;; ============================================================
;;  Mensaje de carga
;; ============================================================
(princ "\n========================================")
(princ "\n  VISTAS_KP cargado")
(princ "\n----------------------------------------")
(princ "\n  GV      -> Guardar vista actual")
(princ "\n  RV      -> Recuperar (menu grafico)")
(princ "\n  BV      -> Borrar vista tuya")
(princ "\n  LIMPIAR -> Borrar TODAS (incl. Civil 3D)")
(princ "\n  LV      -> Listar en consola")
(princ "\n========================================")
(princ)
