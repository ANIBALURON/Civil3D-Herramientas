# Civil3D_TubeInfo - Plugin para Autodesk Civil 3D

![Version](https://img.shields.io/badge/version-1.0.0-blue)
![Framework](https://img.shields.io/badge/.NET-4.8-green)
![Platform](https://img.shields.io/badge/platform-x64-orange)
![Civil 3D](https://img.shields.io/badge/Civil%203D-2024-blue)

## 📋 Descripción

**Civil3D_TubeInfo** es un plugin profesional para Autodesk Civil 3D que permite extraer, procesar y visualizar información de tubos desde bloques CAD. El plugin facilita la gestión y análisis de datos de infraestructura con una interfaz intuitiva y potentes funcionalidades de exportación.

### Características Principales

✨ **Procesamiento de Bloques**
- Soporte para bloques `Punta1` e `INICIO`
- Extracción automática de atributos (NUMERO)
- Cálculo inteligente de coordenadas usando ProfileView y Alignment

🎯 **Interfaz Visual**
- Ventana emergente moderna con DataGridView
- Visualización clara de datos (NÚMERO, X, Y, Z, ESTACIÓN)
- Formato profesional de estaciones (PK10+500)

📊 **Análisis Estadístico**
- Cálculo automático de min/max/promedio
- Análisis de elevaciones
- Rango de estaciones

💾 **Exportación de Datos**
- Exportar a CSV con delimitador `;`
- Copiar datos a portapapeles (tab-delimited)
- Incluye alineamiento en exportación

🔍 **Registro y Logging**
- Sistema centralizado de logs
- Almacenamiento en AppData
- Registro completo de operaciones y errores

## 🚀 Requisitos Previos

- **Autodesk Civil 3D 2024** (o compatible)
- **.NET Framework 4.8**
- **Visual Studio 2022** (para compilación)
- **PowerShell** (para instalación/actualización)

## 📦 Instalación

### Paso 1: Descargar el Plugin

Descarga el archivo compilado o compila desde el código fuente.

### Paso 2: Crear Carpeta de Despliegue

```cmd
mkdir C:\Civil3D_Plugins\Civil3D_TubeInfo
```

### Paso 3: Copiar DLL

Copia el archivo `Civil3D_TubeInfo.dll` a:
```
C:\Civil3D_Plugins\Civil3D_TubeInfo\Civil3D_TubeInfo.dll
```

### Paso 4: Cargar en Civil 3D

En Civil 3D, ejecuta en la línea de comando:

```
NETLOAD
```

Selecciona el archivo compilado:
```
C:\Civil3D_Plugins\Civil3D_TubeInfo\Civil3D_TubeInfo.dll
```

## 🎮 Uso

### Comando Principal

En Civil 3D ejecuta:
```
INFOTUBOS
```

### Proceso de Uso

1. **Seleccionar Vista de Perfil**
   - Se te pedirá que selecciones una ProfileView
   - Esta debe tener un alineamiento asociado

2. **Seleccionar Bloques**
   - Selecciona los bloques `Punta1` o `INICIO`
   - Puedes seleccionar múltiples bloques

3. **Ver Información**
   - La ventana mostrará todos los datos extraídos
   - Números de tubo, coordenadas X, Y, Z y estación

4. **Funcionalidades Disponibles**
   - **📊 Estadísticas**: Ver análisis min/max/promedio
   - **📋 Copiar**: Copiar datos a portapapeles
   - **📄 Exportar a CSV**: Guardar datos en archivo
   - **✖ Cerrar**: Cerrar la ventana

## 📋 Estructura de Datos

### Formato de Entrada

Los bloques deben contener un atributo llamado `NUMERO` con el identificador del tubo.

Ejemplo:
```
Bloque: Punta1
Atributo NUMERO: 5818
```

### Formato de Salida - CSV

```
NUMERO;X;Y;Z;ALINEAMIENTO
5818;455224.958;7463250.786;1571.351;AL_96+500-143
5819;455243.028;7463248.167;1571.170;AL_96+500-143
```

### Formato de Estación

Las estaciones se muestran en formato de Punto Kilométrico (PK):
- **10500 metros** → **PK10+500**
- **2300 metros** → **PK2+300**
- **500 metros** → **PK0+500**

## 🛠️ Compilación desde el Código Fuente

### Requisitos

- Visual Studio 2022 Community
- .NET SDK 4.8+
- Autodesk Civil 3D 2024 instalado

### Pasos

1. **Clonar Repositorio**
   ```bash
   git clone https://github.com/ANIBALURON/01_Utilidades.git
   cd 01_Utilidades/Civil3D_TubeInfo
   ```

2. **Actualizar Referencias (si es necesario)**

   Abre `Civil3D_TubeInfo.csproj` y verifica las rutas:
   ```xml
   <HintPath>C:\Program Files\Autodesk\AutoCAD 2024\AcCoreMgd.dll</HintPath>
   ```

3. **Compilar**
   ```bash
   dotnet build -c Release -p:Platform=x64
   ```

4. **Ubicación del DLL**
   ```
   bin\x64\Release\Civil3D_TubeInfo.dll
   ```

5. **Copiar a Carpeta de Despliegue**
   ```powershell
   Copy-Item -Path "bin\x64\Release\Civil3D_TubeInfo.dll" -Destination "C:\Civil3D_Plugins\Civil3D_TubeInfo\" -Force
   ```

## 📂 Estructura del Proyecto

```
Civil3D_TubeInfo/
├── Civil3D_TubeInfo.csproj          # Archivo de proyecto
├── Civil3D_TubeInfo.sln             # Solución Visual Studio
├── TubeInfoCommands.cs              # Comando principal y clase TubeData
├── TubeInfoForm.cs                  # Formulario Windows Forms
├── TubeProcessingService.cs         # Lógica de procesamiento
├── TubeStatistics.cs                # Cálculos estadísticos
├── Logger.cs                        # Sistema de logging
├── bin/                             # DLL compilado
└── obj/                             # Archivos temporales
```

### Descripción de Archivos

| Archivo | Descripción |
|---------|------------|
| **TubeInfoCommands.cs** | Comando `INFOTUBOS` y clase de datos `TubeData` |
| **TubeInfoForm.cs** | Interfaz gráfica con DataGridView |
| **TubeProcessingService.cs** | Extracción de datos desde bloques |
| **TubeStatistics.cs** | Análisis estadístico de datos |
| **Logger.cs** | Sistema centralizado de logging |

## 🔍 Características Detalladas

### Logger (Sistema de Registro)

El plugin registra todas las operaciones en:
```
C:\Users\[Usuario]\AppData\Roaming\Civil3D_TubeInfo\Logs\TubeInfo_yyyyMMdd.log
```

**Niveles de Log:**
- INFO: Operaciones normales
- WARNING: Advertencias
- ERROR: Errores con stack trace
- DEBUG: Información de depuración (solo en compilación Debug)

### TubeStatistics (Análisis)

Calcula automáticamente:
- Mínimo, máximo y promedio de coordenadas X, Y, Z
- Diferencia de elevación
- Rango de estaciones

**Ejemplo de Salida:**
```
ESTADÍSTICAS DE TUBOS
═════════════════════════════════════════
Total de tubos: 10

Coordenada X (m):
  Mínimo: 455224.958
  Máximo: 455387.419
  Promedio: 455305.689

Coordenada Y (m):
  Mínimo: 7463227.233
  Máximo: 7463250.786
  Promedio: 7463239.510

Coordenada Z - Elevación (m):
  Mínimo: 1568.797
  Máximo: 1571.351
  Promedio: 1570.519
  Diferencia: 2.554

Estaciones (m):
  Mínima: 10470.261
  Máxima: 10833.856
═════════════════════════════════════════
```

## 🐛 Solución de Problemas

### Error: "No se encuentra AecBaseMgd.dll"

**Solución:** Actualiza las referencias en `Civil3D_TubeInfo.csproj` con la ruta correcta de tu instalación de Civil 3D:

```xml
<HintPath>C:\Program Files\Autodesk\AutoCAD 2024\AecBaseMgd.dll</HintPath>
```

### Error: "Seleccione una Vista de Perfil válida"

**Solución:** La ProfileView debe tener un alineamiento asociado. Verifica que:
1. La ProfileView exista
2. Esté basada en un alineamiento
3. El alineamiento sea accesible

### Los botones están en posición incorrecta

**Solución:** Actualiza el DLL siguiendo los pasos de actualización:
1. En Civil 3D ejecuta: `NETUNLOAD`
2. Reemplaza el DLL con la versión nueva
3. Ejecuta: `NETLOAD` nuevamente

### DLL bloqueado "en uso por otro proceso"

**Solución:** El DLL está cargado en Civil 3D
1. En Civil 3D ejecuta: `NETUNLOAD`
2. Espera a que se complete
3. Ahora puedes reemplazar el DLL

## 📊 Cambios por Versión

### v1.0.0 (Actual)

✨ **Nuevas Características**
- Interfaz completamente rediseñada
- Soporte para bloques `Punta1` e `INICIO`
- Formato de estaciones PK (PK10+500)
- Sistema de logging centralizado
- Análisis estadístico automático

🎨 **Mejoras Visuales**
- Casilla de búsqueda eliminada para mejor distribución
- Botones repositionados (distribución uniforme)
- DataGridView optimizado
- Panel de información con estadísticas resumidas

🔧 **Mejoras Técnicas**
- Separación de responsabilidades (Service Pattern)
- Logging thread-safe
- Manejo robusto de errores
- Documentación de código

## 📝 Licencia

Este proyecto es de uso privado. Todos los derechos reservados.

## 📧 Soporte y Contacto

Para reportar problemas o sugerencias:
- Usuario GitHub: **ANIBALURON**
- Repositorio: **01_Utilidades**

## 🤝 Contribuciones

Este es un proyecto privado. Para modificaciones, contacta con el propietario.

---

**Última actualización:** 29 de Octubre de 2025
**Versión:** 1.0.0
**Estado:** Producción
