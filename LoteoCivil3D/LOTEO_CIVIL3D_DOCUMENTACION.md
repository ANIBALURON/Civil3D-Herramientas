# LOTEO CIVIL3D - Documentacion Completa del Proyecto

## Informacion General

| Campo | Valor |
|-------|-------|
| **Nombre** | Loteo Civil3D |
| **Version** | 1.0.0.0 |
| **Autor** | Anibal Uron |
| **Target** | AutoCAD Civil 3D 2024 (compatible 2024-2026) |
| **Framework** | .NET Framework 4.8 |
| **Lenguaje** | C# 7.3 |
| **DLL compilado** | 66 KB |
| **Fecha creacion** | 2026-03-05 |

## Descripcion

Plugin para AutoCAD Civil 3D que automatiza el grading (nivelacion) y etiquetado de lotes residenciales. Soporta dos modos de trabajo:

1. **Con Pad** (estilo norteamericano): Lote con casa interior, retiros, reveal
2. **Espalda-Espalda** (estilo latinoamericano): Lotes pegados por detras, sin pad, calle al frente de cada fila

---

## Estructura de Archivos

```
C:\Users\Anibal\LoteoCivil3D\
|
|-- LoteoCivil3D.csproj          # Proyecto .NET (SDK style, net48)
|-- build.ps1                    # Script de compilacion PowerShell
|
|-- src\
|   |-- AssemblyInfo.cs          # Metadatos del assembly
|   |-- Commands.cs              # Comandos AutoCAD + inicializacion del plugin
|   |
|   |-- Core\
|   |   |-- LoteoSettings.cs    # Settings, enums, SettingsManager (XML persistencia)
|   |   |-- LotGrader.cs        # Motor principal de grading
|   |   |-- LotLabeler.cs       # Motor de etiquetado (MText)
|   |   |-- FeatureLineHelper.cs # Utilidades para FeatureLine
|   |   |-- SurfaceHelper.cs    # Utilidades para TinSurface
|   |   |-- SelectionHelper.cs  # Utilidades de seleccion de objetos
|   |
|   |-- Forms\
|   |   |-- FrmGraderSettings.cs # Dialogo de configuracion (3 tabs)
|   |   |-- FrmLabeler.cs       # Dialogo de etiquetado
|   |   |-- FrmConsole.cs       # Visor de log/resultados
|   |   |-- FrmAbout.cs         # Acerca de
|   |
|   |-- Ribbon\
|       |-- RibbonHelper.cs     # Crea pestana Ribbon en AutoCAD
|
|-- Bundle\
|   |-- PackageContents.xml     # Manifiesto para autoload del plugin
|
|-- tutorial\
|   |-- tutorial_loteo.html     # Tutorial interactivo HTML (10 slides)
|
|-- bin\Release\
    |-- LoteoCivil3D.dll        # DLL compilado
```

### Ubicacion del Plugin Instalado

```
C:\ProgramData\Autodesk\ApplicationPlugins\LoteoCivil3D.Bundle\
|-- PackageContents.xml
|-- Contents\
    |-- Win64\
        |-- LoteoCivil3D.dll
```

---

## Comandos Disponibles

| Comando | Descripcion |
|---------|-------------|
| `LOTEO_SETTINGS` | Abre dialogo de configuracion de grading |
| `LOTEO_GRADER` | Ejecuta grading de lotes seleccionados |
| `LOTEO_LABELER` | Etiqueta elevaciones y pendientes |
| `LOTEO_UPDATE` | Actualiza/refresca superficie de grading |
| `LOTEO_ABOUT` | Informacion del plugin |
| `LOTEO_HELP` | Lista de comandos en linea de comandos |

---

## Compilacion

### Requisitos

- **Visual Studio 2022 Community** (solo se necesita el compilador Roslyn csc.exe)
- **AutoCAD Civil 3D 2024** instalado (para las DLLs de referencia)

### Compilador

Se usa el Roslyn csc.exe de VS 2022 (NO el csc.exe de .NET Framework 4.0 porque no soporta C# 6+):

```
C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe
```

### Script de compilacion: `build.ps1`

Ejecutar desde la raiz del proyecto:

```powershell
cd C:\Users\Anibal\LoteoCivil3D
powershell -ExecutionPolicy Bypass -File build.ps1
```

El script:
1. Compila todos los .cs en `src\` recursivamente
2. Genera `bin\Release\LoteoCivil3D.dll`
3. Copia el DLL a `C:\ProgramData\Autodesk\ApplicationPlugins\LoteoCivil3D.Bundle\Contents\Win64\`

### Referencias (DLLs)

**AutoCAD / Civil 3D** (todas con `Private=false`):

| DLL | Ruta |
|-----|------|
| accoremgd.dll | `C:\Program Files\Autodesk\AutoCAD 2024\accoremgd.dll` |
| Acdbmgd.dll | `C:\Program Files\Autodesk\AutoCAD 2024\Acdbmgd.dll` |
| Acmgd.dll | `C:\Program Files\Autodesk\AutoCAD 2024\Acmgd.dll` |
| AcWindows.dll | `C:\Program Files\Autodesk\AutoCAD 2024\AcWindows.dll` |
| AdWindows.dll | `C:\Program Files\Autodesk\AutoCAD 2024\AdWindows.dll` |
| **AeccDbMgd.dll** | `C:\Program Files\Autodesk\AutoCAD 2024\C3D\AeccDbMgd.dll` |
| AecBaseMgd.dll | `C:\Program Files\Autodesk\AutoCAD 2024\AecBaseMgd.dll` |

> **IMPORTANTE**: AeccDbMgd.dll esta en la subcarpeta `C3D\`, NO en la raiz de AutoCAD 2024.

**Sistema (.NET Framework / WPF)**:

| DLL | Ruta |
|-----|------|
| System.dll | `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\` |
| System.Core.dll | (misma carpeta) |
| System.Drawing.dll | (misma carpeta) |
| System.Windows.Forms.dll | (misma carpeta) |
| System.Xml.dll | (misma carpeta) |
| WindowsBase.dll | `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\` |
| PresentationCore.dll | (misma carpeta WPF) |
| PresentationFramework.dll | (misma carpeta WPF) |

### Flags del compilador

```
/target:library /optimize+ /platform:x64 /langversion:7.3
```

### Warning conocido (ignorable)

```
CS0067: El evento 'RibbonCommandHandler.CanExecuteChanged' nunca se usa
```
Esto es porque `ICommand` requiere el evento pero AutoCAD no lo usa. No afecta funcionalidad.

---

## Enums del Proyecto

### LotLayoutType
```csharp
public enum LotLayoutType
{
    WithPad,     // Lote con pad/casa interior, retiros, reveal
    BackToBack   // Lotes espalda con espalda, sin pad, calle al frente
}
```

### StreetElevationSource
```csharp
public enum StreetElevationSource
{
    Surface,      // Leer de una superficie (EG, corredor, etc)
    FeatureLine   // Leer de un Feature Line de calle (borde sardinel)
}
```

### GradeDirection
```csharp
public enum GradeDirection
{
    FrontToBack, BackToFront, HighToLow, Custom
}
```

---

## Arquitectura y Flujo de Datos

### Flujo LOTEO_GRADER (modo Con Pad)

```
1. Seleccionar lineas de lote (Feature Lines)
2. Seleccionar pads (Feature Lines, opcional)
3. Si StreetSource = FeatureLine: seleccionar FL de calle
4. LotGrader.Execute():
   a. Obtener superficie EG (o cargar FL de calle)
   b. Para cada lote:
      - IdentifyFrontVertices() → vertices mas cercanos a calle
      - SetFrontElevations() → empatar con calle + offset
      - SetRearElevations() → aplicar pendiente posterior
      - InterpolateSideElevations() → interpolar laterales
      - ProcessPad() → elevar pad, aplicar pendiente, crear reveal
   c. AddBreaklinesToSurface() → agregar a superficie FG
```

### Flujo LOTEO_GRADER (modo Espalda-Espalda)

```
1. Seleccionar lineas de lote (Feature Lines)
2. NO pide pads
3. Si StreetSource = FeatureLine: seleccionar FL de calle
4. LotGrader.Execute():
   a. Para cada lote:
      - IdentifyFrontVertices() → por cercania a calle o elevacion baja
      - Frente: empatar con elevacion de calle
      - Trasero (divisoria): frontAvg + (dist * pendiente%) + extra
      - InterpolateSideElevations()
   b. AddBreaklinesToSurface()
```

### Identificacion de Vertices Frontales

El metodo `IdentifyFrontVertices()` tiene 3 estrategias:

1. **StreetSource = FeatureLine**: Los vertices mas cercanos geometricamente al FL de calle son el frente
2. **StreetSource = Surface**: Los vertices con elevacion mas baja en la superficie EG son el frente
3. **Sin superficie/FL**: Usa vertices 0 y 1 (primeros del feature line)

Cantidad de vertices frontales: `Max(2, totalVertices / 2)`

### Elevacion desde Feature Line de Calle

`FeatureLineHelper.GetElevationAtClosestPoint(fl, x, y)`:
- Proyecta el punto (x,y) perpendicularmente al segmento mas cercano del FL
- Usa proyeccion parametrica: `t = dot(toQuery, segVec) / |segVec|^2`
- Clampea t al rango [0,1] (dentro del segmento)
- Interpola linealmente la elevacion: `elevA + (elevB - elevA) * t`
- Esto captura correctamente la pendiente longitudinal (rasante) de la calle

---

## API de Civil 3D Usada

### FeatureLine (Autodesk.Civil.DatabaseServices)

```csharp
// Obtener vertices con elevaciones
Point3dCollection pts = fl.GetPoints(Autodesk.Civil.FeatureLinePointType.AllPoints);

// Establecer elevacion de un vertice por indice
fl.SetPointElevation(int index, double elevation);

// Cantidad de puntos
int count = fl.PointsCount;

// Longitud 2D
double len = fl.Length2D;

// Estilo
ObjectId styleId = fl.StyleId;
```

> **NOTA CRITICA**: La API correcta es `GetPoints(FeatureLinePointType)`, NO `GetPointAtIndex()` (no existe).
> El enum completo es `Autodesk.Civil.FeatureLinePointType` con valores: PIPoint=0, ElevationPoint=1, AllPoints=2.

### TinSurface (Autodesk.Civil.DatabaseServices)

```csharp
// Obtener elevacion en un punto XY
double elev = surface.FindElevationAtXY(x, y);

// Agregar breaklines
surface.BreaklinesDefinition.AddStandardBreaklines(idCollection, 1.0, 1.0, 0.0, 0.0);

// Rebuild
surface.Rebuild();
```

### CivilDocument

```csharp
var civilDoc = Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument;
ObjectIdCollection surfIds = civilDoc.GetSurfaceIds();
var flStyles = civilDoc.Styles.FeatureLineStyles;
var surfStyles = civilDoc.Styles.SurfaceStyles;
```

### Ribbon (Autodesk.Windows)

```csharp
var ribbonCtrl = ComponentManager.Ribbon;
var tab = new RibbonTab { Title = "...", Id = "..." };
var panel = new RibbonPanelSource { Title = "..." };
var btn = new RibbonButton { Text = "...", CommandParameter = "COMANDO" };
btn.CommandHandler = new ICommand_implementation;
```

---

## Settings (Persistencia XML)

Archivos en `%AppData%\LoteoCivil3D\`:
- `GraderSettings.xml`
- `LabelerSettings.xml`

### GraderSettings - Propiedades

| Propiedad | Default | Descripcion |
|-----------|---------|-------------|
| LayoutType | WithPad | Tipo de distribucion |
| StreetSource | Surface | Fuente de elevacion de calle |
| LotSlopeMin | 2.0 | Pendiente minima (%) |
| LotSlopeMax | 5.0 | Pendiente maxima (%) |
| LotSlopeDefault | 2.0 | Pendiente por defecto (%) |
| PadOffsetFromLot | 1.0 | Offset del pad (m) |
| PadElevationOffset | 0.15 | Elevacion sobre terreno (m) |
| PadSlopeAway | true | Pendiente desde centro del pad |
| PadSlope | 2.0 | Pendiente del pad (%) |
| UseReveal | true | Crear reveal |
| RevealDistance | 0.30 | Distancia reveal (m) |
| RevealDrop | 0.05 | Caida reveal (m) |
| FrontSetback | 3.0 | Retiro frontal (m) |
| FrontSlopeToStreet | 2.0 | Pendiente a calle (%) |
| MatchStreetElevation | true | Empatar con calle |
| RearSlope | 2.0 | Pendiente posterior (%) |
| RearPatioSlope | 1.5 | Pendiente patio (%) |
| DrainToRear | true | Drenar hacia atras |
| SideSlope | 2.0 | Pendiente lateral (%) |
| BackToBackSlope | 2.0 | Pendiente a divisoria (%) |
| RidgeExtraElevation | 0.0 | Elevacion extra divisoria (m) |
| SurfaceName | "EG" | Superficie existente |
| GradingSurfaceName | "FG-LOTES" | Superficie de grading |
| AddToSurface | true | Agregar breaklines auto |
| BreaklineGroupName | "Loteo_Breaklines" | Nombre grupo breaklines |
| Direction | FrontToBack | Direccion de grading |

### LabelerSettings - Propiedades

| Propiedad | Default | Descripcion |
|-----------|---------|-------------|
| LabelLotElevations | true | Etiquetar elevaciones de vertices |
| LabelHighPoints | true | Etiquetar puntos altos |
| LabelPadElevations | true | Etiquetar elevacion de pad |
| LabelRevealElevations | false | Etiquetar elevacion de reveal |
| LabelSlopes | true | Etiquetar pendientes |
| LabelLotNumbers | true | Etiquetar numeros de lote |
| TextHeight | 0.10 | Altura de texto (m) |
| DecimalPlaces | 2 | Decimales |
| TextStyle | "Standard" | Estilo de texto |

---

## Formularios (Windows Forms)

### FrmGraderSettings (580x800)
- **Tab "Grading"**: Tipo distribucion, pendientes, calle frontal (fuente elevacion), drenaje, laterales, back-to-back
- **Tab "Pad / Reveal"**: Configuracion pad y reveal (deshabilitado en modo back-to-back)
- **Tab "Superficie"**: Superficies EG/FG, breaklines, estilos FL
- **Botones**: Aceptar, Cancelar, Defaults, Exportar, Importar

### FrmLabeler (420x420)
- Checkboxes para que etiquetar
- Formato: altura texto, decimales, estilo

### FrmConsole (600x450)
- ListBox con log de resultados
- Boton copiar al clipboard

### FrmAbout (400x280)
- Nombre, version, descripcion, autor

---

## Layers Creados

| Layer | Color | Uso |
|-------|-------|-----|
| LOTEO-LABELS | RGB(0,200,200) | Etiquetas normales |
| LOTEO-LABELS-SPECIAL | RGB(255,100,100) | Etiquetas especiales (HP, PAD) |

---

## Ribbon

Pestana: **"Loteo Civil3D"** (ID: LOTEO_TAB)

| Panel | Boton | Comando | Tamano |
|-------|-------|---------|--------|
| Grading | Configuracion | LOTEO_SETTINGS | Large |
| Grading | Lot Grader | LOTEO_GRADER | Large |
| Etiquetado | Etiquetar | LOTEO_LABELER | Large |
| Superficie | Actualizar | LOTEO_UPDATE | Large |
| Informacion | Acerca de | LOTEO_ABOUT | Standard |
| Informacion | Ayuda | LOTEO_HELP | Standard |

Los iconos se generan dinamicamente con GDI+ (texto sobre fondo azul), sin archivos .ico externos.

---

## Errores Resueltos Durante Desarrollo

### 1. AeccDbMgd.dll no encontrado
**Causa**: Esta en `C:\Program Files\Autodesk\AutoCAD 2024\C3D\` no en la raiz.
**Fix**: Cambiar HintPath a incluir `\C3D\`.

### 2. AecBaseMgd.dll faltante (CS0012)
**Causa**: Tipos de `AecBaseMgd` requeridos indirectamente por la API Civil 3D.
**Fix**: Agregar referencia a `C:\Program Files\Autodesk\AutoCAD 2024\AecBaseMgd.dll`.

### 3. FeatureLine.GetPointAtIndex() no existe
**Causa**: API incorrecta. La API real es `GetPoints(FeatureLinePointType)`.
**Fix**: Se uso `ildasm` para inspeccionar AeccDbMgd.dll y descubrir la API correcta.
El enum es `Autodesk.Civil.FeatureLinePointType` (PIPoint=0, ElevationPoint=1, AllPoints=2).

### 4. CS0104 Ambiguous Exception
**Causa**: `Autodesk.AutoCAD.Runtime.Exception` vs `System.Exception` en RibbonHelper.cs.
**Fix**: Usar `System.Exception` explicitamente.

### 5. csc.exe de .NET Framework 4.0 no soporta C# 6+
**Causa**: `$"interpolated strings"` y auto-properties con inicializadores requieren C# 6+.
**Fix**: Usar Roslyn csc.exe de VS 2022 con `/langversion:7.3`.

### 6. PowerShell COM object escaping en bash
**Causa**: Bash escapaba `$` en variables PowerShell.
**Fix**: Escribir scripts a archivos .ps1 y ejecutar con `powershell -File`.

---

## Conceptos de Grading

### Modo Con Pad (WithPad)
```
         calle
    ┌─────────────────┐ ← frente (empata con calle)
    │   ┌─────────┐   │
    │   │  CASA   │   │ ← pad (elevado sobre terreno)
    │   │  (pad)  │   │
    │   └─────────┘   │ ← reveal (caida alrededor del pad)
    │                  │
    └─────────────────┘ ← trasero (pendiente hacia calle o atras)
```

### Modo Espalda-Espalda (BackToBack)
```
         calle (elevacion baja)
    ┌───┬───┬───┬───┐  ← frente: empata con calle
    │ v │ v │ v │ v │  ← agua baja hacia la calle
    │   │   │   │   │
    ├───┼───┼───┼───┤  ← DIVISORIA (punto mas alto)
    │   │   │   │   │
    │ v │ v │ v │ v │  ← agua baja hacia la calle
    └───┴───┴───┴───┘  ← frente: empata con calle
         calle (elevacion baja)
```

Lotes tipicos: 6m x 10m, el lote completo ES la casa.

### Fuentes de Elevacion de Calle

**Superficie**: `surface.FindElevationAtXY(x, y)` - Lee directo de la superficie.

**Feature Line de calle**: Proyecta perpendicularmente al borde de sardinel/pavimento y obtiene la elevacion interpolada. Esto es mas preciso cuando la calle tiene rasante con pendiente longitudinal.

```
                    calle con pendiente 3%
                    ↗ ↗ ↗ ↗ ↗ ↗ ↗ ↗ ↗ ↗
    FL sardinel: ──●────●────●────●────●──  (elevaciones 3D)
                   |    |    |    |    |
    ┌──────┐  ┌──────┐  ┌──────┐  ┌──────┐
    │ lote │  │ lote │  │ lote │  │ lote │
    └──────┘  └──────┘  └──────┘  └──────┘
```

---

## PackageContents.xml

- Compatible: Civil3D R24.0 (2024) a R25.1 (2026)
- LoadOnAutoCADStartup="True"
- LoadOnCommandInvocation="True"

---

## Tutorial HTML

Archivo: `tutorial\tutorial_loteo.html`
- 10 slides interactivos con navegacion por teclado (flechas)
- Diagramas SVG animados
- Cubre: concepto, problema, frente/trasero/pad/reveal, seccion transversal, pasos en Civil 3D

---

## Para Futuras Modificaciones

### Agregar un nuevo comando
1. Agregar metodo con `[CommandMethod("LOTEO_NUEVO")]` en `src\Commands.cs`
2. Agregar `<Command>` en `Bundle\PackageContents.xml`
3. Agregar boton en `src\Ribbon\RibbonHelper.cs`
4. Recompilar con `build.ps1`

### Agregar una nueva setting
1. Agregar propiedad en `GraderSettings` en `src\Core\LoteoSettings.cs`
2. Agregar control en `src\Forms\FrmGraderSettings.cs` (BuildXxxTab, LoadSettings, SaveSettings, BtnDefaults_Click)
3. Usar la setting en `LotGrader.cs` o donde corresponda

### Agregar un nuevo modo de grading
1. Agregar valor al enum `LotLayoutType`
2. Agregar metodo `ProcessNuevoModo()` en `LotGrader.cs`
3. Agregar caso en el `if` de `Execute()`
4. Agregar radio button en `FrmGraderSettings`

### Recompilar rapido
```powershell
cd C:\Users\Anibal\LoteoCivil3D
powershell -ExecutionPolicy Bypass -File build.ps1
```

### Probar en Civil 3D
1. Cerrar Civil 3D si esta abierto
2. Recompilar con build.ps1
3. Abrir Civil 3D 2024
4. El plugin se carga automaticamente (aparece en Ribbon)
5. Ejecutar `LOTEO_HELP` para verificar

---

## Herramientas de Debug Utiles

### Inspeccionar DLLs de Civil 3D con ildasm
```
"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\x64\ildasm.exe" "C:\Program Files\Autodesk\AutoCAD 2024\C3D\AeccDbMgd.dll" /out=AeccDbMgd.il
```
Genera un archivo IL de ~72MB que se puede buscar con grep para encontrar metodos y propiedades de la API.

### Ver log de errores en Civil 3D
El plugin escribe errores en la linea de comandos de AutoCAD. Tambien muestra un dialogo FrmConsole con el log detallado despues de cada operacion.
