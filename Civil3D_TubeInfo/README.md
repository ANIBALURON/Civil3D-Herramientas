# 📋 INFO TUBOS - CIVIL 3D

Aplicación para Civil 3D que permite seleccionar múltiples bloques "Punta1" y visualizar su información (NUMERO, X, Y, Z) en una ventana emergente de forma rápida y eficiente.

## 🎯 Características

- ✅ Selección múltiple de bloques "Punta1"
- ✅ Visualización de datos en tabla ordenada por número
- ✅ Exportación a CSV
- ✅ Interfaz moderna y amigable
- ✅ Filtrado automático de bloques
- ✅ Manejo de errores robusto

## 📦 Requisitos

- **AutoCAD Civil 3D** (2020 o superior recomendado)
- **Visual Studio 2019/2022** (Community, Professional o Enterprise)
- **.NET Framework 4.8**
- **Windows 10/11**

## 🛠️ Instalación

### Paso 1: Configurar el Proyecto

1. Abre el proyecto `Civil3D_TubeInfo.csproj` en Visual Studio

2. **IMPORTANTE**: Actualiza las rutas de las DLLs de AutoCAD según tu versión instalada.
   
   Edita el archivo `.csproj` y ajusta las rutas en la sección `<ItemGroup>`:

```xml
<Reference Include="AcCoreMgd">
  <HintPath>C:\Program Files\Autodesk\AutoCAD 2024\AcCoreMgd.dll</HintPath>
  <Private>False</Private>
</Reference>
<!-- Ajusta todas las rutas según tu versión -->
```

   Rutas comunes según versión:
   - **Civil 3D 2024**: `C:\Program Files\Autodesk\AutoCAD 2024\`
   - **Civil 3D 2023**: `C:\Program Files\Autodesk\AutoCAD 2023\`
   - **Civil 3D 2022**: `C:\Program Files\Autodesk\AutoCAD 2022\`

### Paso 2: Compilar

1. En Visual Studio, selecciona **Release** y **x64** en la barra de herramientas
2. Ve a **Build > Build Solution** (o presiona Ctrl+Shift+B)
3. Verifica que no haya errores en la ventana Output

La DLL compilada estará en: `bin\x64\Civil3D_TubeInfo.dll`

### Paso 3: Cargar en Civil 3D

#### Opción A: Carga Manual (Para pruebas)

1. Abre Civil 3D
2. Escribe el comando `NETLOAD`
3. Busca y selecciona `Civil3D_TubeInfo.dll`
4. Listo, ya puedes usar el comando `INFOTUBOS`

#### Opción B: Carga Automática (Recomendado)

1. Copia `Civil3D_TubeInfo.dll` a una carpeta permanente, por ejemplo:
   ```
   C:\Civil3D_Plugins\Civil3D_TubeInfo\Civil3D_TubeInfo.dll
   ```

2. Crea un archivo llamado `acad.lsp` (o edita el existente) en:
   ```
   C:\Users\[TuUsuario]\AppData\Roaming\Autodesk\C3D 2024\enu\Support\
   ```

3. Agrega esta línea al archivo:
   ```lisp
   (command "NETLOAD" "C:\\Civil3D_Plugins\\Civil3D_TubeInfo\\Civil3D_TubeInfo.dll")
   ```

4. Reinicia Civil 3D - la DLL se cargará automáticamente

## 🚀 Uso

### Comando Principal: `INFOTUBOS`

1. En Civil 3D, escribe el comando: `INFOTUBOS`

2. **PASO 1 - Selecciona la Vista de Perfil:**
   ```
   Seleccione la Vista de Perfil donde están los tubos:
   ```
   Haz clic en la vista de perfil que contiene tus bloques

3. **PASO 2 - Selecciona los bloques:**
   ```
   Seleccione los bloques 'Punta1':
   ```
   - **Clic individual**: Selecciona uno por uno
   - **Ventana**: Arrastra de derecha a izquierda para ventana de cruce
   - **Selección rectangular**: Arrastra de izquierda a derecha

4. Presiona **Enter** para confirmar la selección

5. Se abrirá una ventana mostrando:
   - **NÚMERO**: Tag NUMERO del bloque
   - **X (m)**: Coordenada X del alineamiento
   - **Y (m)**: Coordenada Y del alineamiento
   - **Z (m)**: Elevación del tubo

**IMPORTANTE:** Las coordenadas X, Y se calculan respecto al alineamiento asociado a la vista de perfil seleccionada. La coordenada Z corresponde a la elevación del tubo en el perfil.

### Funciones de la Ventana

- **📄 Exportar a CSV**: Guarda los datos en formato CSV
- **✖ Cerrar**: Cierra la ventana

## 📊 Ejemplo de Salida CSV

```csv
NUMERO;X;Y;Z;ALINEAMIENTO
7037;478213.480;7483210.953;1735.250;Eje Principal
7038;478231.685;7483211.382;1735.440;Eje Principal
7039;478250.123;7483212.015;1735.630;Eje Principal
```

## 🔧 Personalización

### Cambiar el nombre del bloque objetivo

Si tus bloques no se llaman "Punta1", edita el archivo `TubeInfoCommands.cs`:

```csharp
// Línea ~26
new TypedValue((int)DxfCode.BlockName, "TU_NOMBRE_DE_BLOQUE")

// Línea ~62
if (blockRef != null && blockRef.Name == "TU_NOMBRE_DE_BLOQUE")
```

### Cambiar el formato de decimales

En `TubeInfoCommands.cs`, líneas 98-100:

```csharp
public string XFormatted => $"{X:F3}";  // F3 = 3 decimales
public string YFormatted => $"{Y:F3}";  // Cambia a F2 para 2 decimales
public string ZFormatted => $"{Z:F3}";
```

### Agregar más atributos

Si quieres mostrar más información (IDNUM, PROXNUM, etc.), edita ambos archivos:

1. **TubeInfoCommands.cs**: Agrega propiedades a la clase `TubeData`
2. **TubeInfoForm.cs**: Agrega columnas al DataGridView

## ❗ Solución de Problemas

### Error: "Could not load file or assembly"
- Verifica que las rutas de las DLLs en el .csproj sean correctas
- Asegúrate de estar compilando para x64
- Verifica que la versión de .NET Framework coincida

### No aparece el comando INFOTUBOS
- Verifica que la DLL se haya cargado: comando `NETLOAD`
- Revisa si hay errores en la línea de comandos de Civil 3D
- Intenta recargar la DLL

### No selecciona ningún bloque
- Verifica que los bloques se llamen exactamente "Punta1"
- Revisa que el bloque tenga el atributo "NUMERO"
- Usa `LIST` en Civil 3D para verificar el nombre del bloque

### La ventana no muestra datos
- Verifica que los bloques tengan el atributo "NUMERO"
- Revisa la consola de Civil 3D para mensajes de error
- Asegúrate de que los bloques estén en el espacio modelo

## 📝 Notas Técnicas

- Se requiere seleccionar primero la **Vista de Perfil** donde están los bloques
- Las coordenadas X, Y se calculan usando `Alignment.PointLocation()` respecto al alineamiento
- La coordenada Z se obtiene de la elevación del tubo en el perfil usando `ProfileView.FindStationAndElevationAtXY()`
- Los bloques se ordenan numéricamente por el tag NUMERO
- Si un bloque no tiene NUMERO, se le asigna "S/N-X"
- El formato de exportación CSV usa punto y coma (;) como separador
- La precisión por defecto es de 3 decimales
- **IMPORTANTE**: Los bloques deben estar dentro de la vista de perfil seleccionada

## 🤝 Soporte

Para reportar problemas o sugerencias, documenta:
1. Versión de Civil 3D
2. Mensaje de error completo
3. Pasos para reproducir el problema

## 📄 Licencia

Código libre para uso personal y comercial.

---

**Desarrollado para optimizar el flujo de trabajo en Civil 3D** 🚀
