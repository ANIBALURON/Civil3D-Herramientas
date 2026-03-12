#Requires AutoHotkey v2.0
#SingleInstance Force

MyGui := Gui("+AlwaysOnTop", "Detector de Teclas")
MyGui.SetFont("s14")
MyGui.Add("Text",, "Presiona cualquier tecla...")
MyGui.Add("Text",, "─────────────────────")
txtName := MyGui.Add("Text", "w400", "Nombre: (esperando)")
txtSC := MyGui.Add("Text", "w400", "ScanCode: (esperando)")
txtVK := MyGui.Add("Text", "w400", "VK Code: (esperando)")
MyGui.Add("Text",, "─────────────────────")
MyGui.Add("Text",, "Presiona ESC para cerrar")
MyGui.Show()

ih := InputHook("L0 I1")
ih.KeyOpt("{All}", "N")
ih.OnKeyDown := OnKey
ih.Start()

OnKey(ih, vk, sc) {
    name := GetKeyName(Format("vk{:x}sc{:x}", vk, sc))
    txtName.Value := "Nombre: " name
    txtSC.Value := "ScanCode: 0x" Format("{:04X}", sc) " (" sc ")"
    txtVK.Value := "VK Code: 0x" Format("{:02X}", vk) " (" vk ")"
}

MyGui.OnEvent("Close", (*) => ExitApp())
Hotkey("Escape", (*) => ExitApp())
