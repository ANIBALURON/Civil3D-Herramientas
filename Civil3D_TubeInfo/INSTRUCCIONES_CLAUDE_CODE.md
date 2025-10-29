# 🚀 COMPILACIÓN CON CLAUDE CODE

## PASO 1: Abre esta carpeta en Claude Code
```bash
cd ruta\a\Civil3D_TubeInfo
```

## PASO 2: Dile a Claude Code lo siguiente:

```
Lee el archivo README_CLAUDE_CODE.md y ejecuta todas las tareas.
Necesito que:
1. Detectes mi versión de Civil 3D instalada
2. Actualices las referencias en el archivo .csproj
3. Compiles el proyecto automáticamente
4. Copies la DLL a C:\Civil3D_Plugins\Civil3D_TubeInfo\
5. Me des instrucciones finales de cómo usarlo

Hazlo todo automáticamente.
```

## PASO 3: Claude Code hará todo solo

Claude Code:
- ✅ Detectará tu versión de Civil 3D
- ✅ Actualizará las rutas de las DLLs
- ✅ Compilará el proyecto
- ✅ Te dirá dónde quedó la DLL
- ✅ Te dará instrucciones de uso

---

## ALTERNATIVA: Usar el script compile.bat

Si prefieres hacerlo manual:

1. **IMPORTANTE**: Primero edita `Civil3D_TubeInfo.csproj`
   - Actualiza las rutas de las DLLs según tu versión de Civil 3D
   - Busca las líneas con `<HintPath>` y cambia la versión

2. Ejecuta: `compile.bat`
   - El script compilará todo automáticamente

---

## ¿Qué tiene este proyecto?

- ✅ Comando `INFOTUBOS` para Civil 3D
- ✅ Selección múltiple de bloques "Punta1"
- ✅ Ventana con tabla mostrando: NUMERO, X, Y, Z
- ✅ Exportación a CSV
- ✅ Interfaz moderna

---

## Documentación incluida:

- `README_CLAUDE_CODE.md` - Instrucciones detalladas para Claude Code
- `TAREAS_CLAUDE_CODE.txt` - Lista de tareas simplificada
- `compile.bat` - Script de compilación automática
- `README.md` - Documentación completa del proyecto
- `GUIA_RAPIDA.md` - Guía de instalación manual
- `PERSONALIZACION.md` - Cómo modificar el código

---

**¡Eso es todo! Claude Code se encargará del resto.** 🎉
