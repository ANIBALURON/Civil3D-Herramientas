# Civil3D-Herramientas

Herramientas y plugins para Autodesk Civil 3D, topografia y GIS.

## Proyectos

| Carpeta | Descripcion | Lenguaje |
|---------|-------------|----------|
| **SurveyViewerC3D** | Plugin C# - Visor de base de datos Survey en Civil 3D | C# |
| **SurveyViewer** | App Python - Visor e importador de puntos (Civil 3D cerrado) | Python |
| **LoteoCivil3D** | Plugin C# - Loteo automatico de terrenos | C# |
| **Civil3D_TubeInfo** | Plugin C# - Extractor de informacion de tubos | C# |
| **LISP** | Scripts AutoLISP para Civil 3D | LISP |
| **Macros_AHK** | Macros AutoHotkey v2 para teclado Vortex Core 40% | AHK |
| **Cartera_GIS** | Procesador cartera predoblado gasoducto (QGIS + Mapa HTML) | Python |

## Requisitos

- **Autodesk Civil 3D 2024/2026**
- **.NET Framework 4.8** (plugins C#)
- **Visual Studio 2022** (compilacion)
- **Python 3.x** (SurveyViewer, Cartera_GIS)
- **AutoHotkey v2** (Macros_AHK)

## Compilacion (plugins C#)

Cada plugin tiene su propio `build.ps1`:

```powershell
cd SurveyViewerC3D
powershell -ExecutionPolicy Bypass -File build.ps1

cd LoteoCivil3D
powershell -ExecutionPolicy Bypass -File build.ps1
```

## Autor

**Anibal** - [GitHub](https://github.com/ANIBALURON)
