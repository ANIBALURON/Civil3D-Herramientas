# 🤖 INSTRUCCIONES PARA CLAUDE CODE

## Objetivo del Proyecto
Compilar una aplicación .NET para Civil 3D que permite visualizar información de bloques "Punta1" mostrando NUMERO, X, Y, Z en una ventana emergente.

---

## 🎯 TAREAS PARA CLAUDE CODE

### TAREA 1: Detectar Versión de Civil 3D Instalada
1. Busca en `C:\Program Files\Autodesk\` las carpetas de AutoCAD
2. Identifica qué versiones de Civil 3D están instaladas (2020, 2021, 2022, 2023, 2024, etc.)
3. Reporta al usuario qué versiones encontraste

### TAREA 2: Actualizar Referencias del Proyecto
1. Abre el archivo `Civil3D_TubeInfo.csproj`
2. Identifica las líneas que contienen `<HintPath>` dentro de `<Reference>`
3. Actualiza TODAS las rutas según la versión de Civil 3D encontrada en TAREA 1

**Rutas a actualizar:**
```xml
<Reference Include="AcCoreMgd">
  <HintPath>C:\Program Files\Autodesk\AutoCAD [VERSION]\AcCoreMgd.dll</HintPath>
</Reference>
<Reference Include="AcDbMgd">
  <HintPath>C:\Program Files\Autodesk\AutoCAD [VERSION]\AcDbMgd.dll</HintPath>
</Reference>
<Reference Include="AcMgd">
  <HintPath>C:\Program Files\Autodesk\AutoCAD [VERSION]\AcMgd.dll</HintPath>
</Reference>
<Reference Include="AeccDbMgd">
  <HintPath>C:\Program Files\Autodesk\AutoCAD [VERSION]\C3D\AeccDbMgd.dll</HintPath>
</Reference>
```

**Ejemplo:** Si encuentras Civil 3D 2024, reemplaza `[VERSION]` con `2024`

### TAREA 3: Ubicar MSBuild
1. Busca MSBuild.exe en las siguientes ubicaciones (en orden de prioridad):
   - `C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe`
   - `C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe`
   - `C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe`
   - `C:\Program Files\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe`
   - `C:\Program Files\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe`
   - `C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe`

2. Si no encuentras MSBuild, informa al usuario que necesita instalar Visual Studio o Build Tools

### TAREA 4: Compilar el Proyecto
1. Ejecuta MSBuild con estos parámetros exactos:
```bash
MSBuild.exe "Civil3D_TubeInfo.sln" /p:Configuration=Release /p:Platform=x64 /t:Build /v:minimal
```

2. Verifica que la compilación sea exitosa (exit code = 0)
3. Confirma que existe el archivo: `bin\x64\Civil3D_TubeInfo.dll`

### TAREA 5: Preparar para Despliegue
1. Crea la carpeta: `C:\Civil3D_Plugins\Civil3D_TubeInfo\`
2. Copia `bin\x64\Civil3D_TubeInfo.dll` a esa carpeta
3. Muestra al usuario la ubicación final de la DLL

### TAREA 6: Configurar Carga Automática (Opcional)
1. Identifica la carpeta de soporte de Civil 3D según la versión detectada:
   - Patrón: `C:\Users\[USUARIO]\AppData\Roaming\Autodesk\C3D [VERSION]\enu\Support\`
   
2. Pregunta al usuario si desea configurar carga automática

3. Si el usuario acepta:
   - Crea o edita el archivo `acad.lsp` en esa carpeta
   - Agrega esta línea al final:
   ```lisp
   ; InfoTubos Auto-Load
   (command "NETLOAD" "C:\\Civil3D_Plugins\\Civil3D_TubeInfo\\Civil3D_TubeInfo.dll")
   ```

### TAREA 7: Reporte Final
Muestra al usuario:
- ✅ Versión de Civil 3D detectada
- ✅ Referencias actualizadas
- ✅ Compilación exitosa
- ✅ Ubicación de la DLL
- ✅ Estado de carga automática
- 📋 Instrucciones de uso del comando `INFOTUBOS`

---

## 📝 INFORMACIÓN TÉCNICA DEL PROYECTO

### Archivos del Proyecto
- `TubeInfoCommands.cs` - Comando principal INFOTUBOS
- `TubeInfoForm.cs` - Interfaz gráfica (ventana)
- `Civil3D_TubeInfo.csproj` - Configuración del proyecto
- `Civil3D_TubeInfo.sln` - Solución de Visual Studio

### Configuración de Compilación
- **Framework**: .NET Framework 4.8
- **Platform**: x64 (obligatorio)
- **Configuration**: Release
- **Output**: `bin\x64\Civil3D_TubeInfo.dll`

### Dependencies (DLLs de AutoCAD/Civil 3D)
- AcCoreMgd.dll
- AcDbMgd.dll
- AcMgd.dll
- AeccDbMgd.dll (Civil 3D específico)
- System.Windows.Forms
- System.Drawing

---

## 🔍 VALIDACIONES IMPORTANTES

Antes de compilar, verifica:
- [ ] MSBuild.exe existe y es accesible
- [ ] Las 4 DLLs de referencia existen en las rutas especificadas
- [ ] El proyecto está configurado para x64 (no Any CPU)
- [ ] El target framework es .NET Framework 4.8

Si falta algo, reporta claramente al usuario qué falta y dónde puede obtenerlo.

---

## ⚠️ MANEJO DE ERRORES

### Si no encuentra Civil 3D:
"No se detectó ninguna versión de Civil 3D instalada. Por favor instala Civil 3D o proporciona manualmente la ruta de instalación."

### Si no encuentra MSBuild:
"MSBuild no encontrado. Opciones:
1. Instalar Visual Studio 2019/2022 (Community es gratis)
2. Instalar Build Tools for Visual Studio
Descarga: https://visualstudio.microsoft.com/downloads/"

### Si la compilación falla:
"Error en la compilación. Verifica:
1. Que todas las referencias existan
2. Que el código no tenga errores de sintaxis
3. Que estés compilando para x64

Log completo: [mostrar el output de MSBuild]"

---

## 🎯 RESULTADO ESPERADO

Al finalizar todas las tareas, el usuario debe tener:
1. DLL compilada en: `C:\Civil3D_Plugins\Civil3D_TubeInfo\Civil3D_TubeInfo.dll`
2. (Opcional) Carga automática configurada
3. Instrucciones claras de cómo usar el comando `INFOTUBOS` en Civil 3D

---

## 📖 USO DEL COMANDO (Para informar al usuario)

```
1. Abre Civil 3D
2. Escribe: INFOTUBOS
3. Selecciona los bloques "Punta1" que deseas consultar
4. Presiona Enter
5. Se abrirá una ventana mostrando: NUMERO, X, Y, Z de cada bloque
6. Opcionalmente, exporta a CSV con el botón "Exportar"
```

---

## 🔧 COMANDOS ÚTILES PARA EJECUTAR

### Limpiar compilaciones anteriores
```bash
rmdir /s /q bin
rmdir /s /q obj
```

### Compilar
```bash
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" ^
  "Civil3D_TubeInfo.sln" ^
  /p:Configuration=Release ^
  /p:Platform=x64 ^
  /t:Build ^
  /v:minimal
```

### Verificar DLL generada
```bash
dir bin\x64\Civil3D_TubeInfo.dll
```

---

## 📋 CHECKLIST DE COMPILACIÓN

Usa este checklist para verificar cada paso:

- [ ] TAREA 1: Versión de Civil 3D detectada
- [ ] TAREA 2: Referencias actualizadas en .csproj
- [ ] TAREA 3: MSBuild localizado
- [ ] TAREA 4: Proyecto compilado exitosamente
- [ ] TAREA 5: DLL copiada a carpeta de despliegue
- [ ] TAREA 6: Carga automática configurada (opcional)
- [ ] TAREA 7: Reporte mostrado al usuario

---

## 💡 TIPS PARA CLAUDE CODE

1. **Siempre verifica** que los archivos existan antes de intentar usarlos
2. **Usa rutas absolutas** en los comandos de compilación
3. **Captura el output** de MSBuild para mostrar errores si falla
4. **Pregunta al usuario** cuando haya múltiples versiones de Civil 3D
5. **Valida** que la DLL se haya generado antes de reportar éxito
6. **Sé claro** en los mensajes de error - incluye qué faltó y cómo obtenerlo

---

**¡Claude Code, adelante! Compila este proyecto para el usuario.** 🚀
