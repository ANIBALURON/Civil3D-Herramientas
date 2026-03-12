#Requires AutoHotkey v2.0
#SingleInstance Force

if !A_IsAdmin {
    try {
        Run('*RunAs "' A_AhkPath '" "' A_ScriptFullPath '"')
    }
    ExitApp()
}

; ============================================================
; MACROS CIVIL 3D - Teclado Vortex Core
; ============================================================
; Win IZQ (un toque) = Toggle Civil 3D ON/OFF
; Win DER (AppsKey) = Win real para atajos
; ============================================================

SetKeyDelay(30, 30)
civil3dMode := false

; --- Win izquierda = Toggle (bloqueada, sin filtrar L) ---
LWin:: {
    global civil3dMode
    civil3dMode := !civil3dMode
    if (civil3dMode) {
        TraySetIcon("Shell32.dll", 44)
        ToolTip(">>> CIVIL 3D: ON <<<")
        SetTimer(() => ToolTip(), -2000)
    } else {
        TraySetIcon("Shell32.dll", 173)
        ToolTip("CIVIL 3D: OFF")
        SetTimer(() => ToolTip(), -2000)
    }
}

; --- AppsKey = Win real (capturas, idioma, etc) ---
AppsKey & Space:: SendEvent("#{Space}")
AppsKey & e:: SendEvent("#e")
AppsKey & s:: SendEvent("#+s")
AppsKey:: SendEvent("{RWin}")

; Funcion para enviar comando
SendCmd(cmd) {
    Sleep(50)
    SendEvent(cmd)
    Sleep(50)
    SendEvent("{Enter}")
}

; ============================================================
; COMANDOS CIVIL 3D
; ============================================================
#HotIf civil3dMode

a:: SendCmd("PLINE")
s:: SendCmd("MOVE")
d:: SendCmd("COPY")
f:: SendCmd("DIMALIGNED")
g:: SendCmd("_DcAngular")

q:: SendEvent("^z")
w:: SendCmd("_.FreeSurveyPoints")
e:: SendCmd("CIRCLE")
r:: SendCmd("LAYERADD")
t:: SendCmd("REVERSE")

z:: SendCmd("CHAMFER")
x:: SendCmd("ERASE")
c:: SendCmd("SM_V0")
v:: SendCmd("SM_VT")
b:: SendCmd("BREAK")


i:: SendEvent("{Up}")
j:: SendEvent("{Left}")
k:: SendEvent("{Down}")
l:: SendEvent("{Right}")

Escape:: SendEvent("{Escape}")
Space:: SendEvent("{Enter}")
LAlt:: SendEvent("{Enter}")

#HotIf

; ============================================================
; BANDEJA DEL SISTEMA
; ============================================================

TraySetIcon("Shell32.dll", 173)
A_IconTip := "Civil 3D Macros`nWin izq = Toggle"

tray := A_TrayMenu
tray.Delete()
tray.Add("Civil 3D Macros", (*) => "")
tray.Add()
tray.Add("Ver comandos", ShowCmds)
tray.Add("Recargar", (*) => Reload())
tray.Add("Salir", (*) => ExitApp())

ShowCmds(*) {
    cmds := "
    (
=== COMANDOS CIVIL 3D ===
[Win izq] = Toggle ON/OFF (un toque)
[Win der + Space] = Cambiar idioma
[Shift + Win der + S] = Captura pantalla

A = PLINE          S = MOVE
D = COPY            F = DIMALIGNED
G = _DcAngular

Q = Ctrl+Z (Undo)  W = _.FreeSurveyPoints
E = CIRCLE          R = LAYERADD
T = REVERSE

Z = CHAMFER         X = ERASE
C = SM_V0           V = SM_VT
B = BREAK

Y = (libre)
U = (libre)

I=Arriba J=Izq K=Abajo L=Der
Space=Enter  Esc=Cancelar
Alt izq=Enter
    )"
    MsgBox(cmds, "Comandos Civil 3D", "0x40000")
}

ToolTip("Civil 3D Macros listo`n[Win izq] = Toggle ON/OFF")
SetTimer(() => ToolTip(), -3000)
