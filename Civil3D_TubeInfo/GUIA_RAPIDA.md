# 🚀 GUÍA RÁPIDA DE COMPILACIÓN Y DESPLIEGUE

## ⚡ Compilación Rápida

### 1. Abrir en Visual Studio
```
Abrir: Civil3D_TubeInfo.csproj
```

### 2. Verificar Configuración
- **Plataforma**: x64 (NO usar Any CPU)
- **Configuración**: Release
- **Framework**: .NET Framework 4.8

### 3. Actualizar Rutas de DLLs

Edita `Civil3D_TubeInfo.csproj` y cambia las rutas según tu versión de Civil 3D:

```xml
<!-- Encuentra tu versión de Civil 3D -->
Civil 3D 2024: C:\Program Files\Autodesk\AutoCAD 2024\
Civil 3D 2023: C:\Program Files\Autodesk\AutoCAD 2023\
Civil 3D 2022: C:\Program Files\Autodesk\AutoCAD 2022\
Civil 3D 2021: C:\Program Files\Autodesk\AutoCAD 2021\
```

Reemplaza en estas líneas:
```xml
<HintPath>C:\Program Files\Autodesk\AutoCAD [TU_VERSION]\AcCoreMgd.dll</HintPath>
<HintPath>C:\Program Files\Autodesk\AutoCAD [TU_VERSION]\AcDbMgd.dll</HintPath>
<HintPath>C:\Program Files\Autodesk\AutoCAD [TU_VERSION]\AcMgd.dll</HintPath>
<HintPath>C:\Program Files\Autodesk\AutoCAD [TU_VERSION]\C3D\AeccDbMgd.dll</HintPath>
```

### 4. Compilar
```
Menu: Build > Build Solution
Atajo: Ctrl + Shift + B
```

### 5. Ubicar DLL Compilada
```
Ruta: bin\x64\Civil3D_TubeInfo.dll
```

---

## 📦 Despliegue en Civil 3D

### Opción 1: Prueba Rápida (Carga Manual)

1. Abre Civil 3D
2. Comando: `NETLOAD`
3. Selecciona: `Civil3D_TubeInfo.dll`
4. Prueba: `INFOTUBOS`

⚠️ **Nota**: Deberás cargar la DLL cada vez que abras Civil 3D

---

### Opción 2: Carga Automática (Recomendado)

#### Paso 1: Crear Carpeta de Plugins
```
C:\Civil3D_Plugins\Civil3D_TubeInfo\
```

#### Paso 2: Copiar DLL
Copia `Civil3D_TubeInfo.dll` a la carpeta creada

#### Paso 3: Crear/Editar acad.lsp

**Ubicación del archivo:**
```
C:\Users\[TU_USUARIO]\AppData\Roaming\Autodesk\C3D 2024\enu\Support\acad.lsp
```

**Contenido:**
```lisp
; Carga automática de InfoTubos
(command "NETLOAD" "C:\\Civil3D_Plugins\\Civil3D_TubeInfo\\Civil3D_TubeInfo.dll")
```

⚠️ **Importante**: 
- Usa doble backslash `\\` en la ruta
- Ajusta "C3D 2024" según tu versión
- Si el archivo no existe, créalo con Notepad

#### Paso 4: Reiniciar Civil 3D
La DLL se cargará automáticamente al iniciar

---

## 🧪 Verificación

### Comprobar que está cargado:
1. Abre Civil 3D
2. Escribe: `INFOTUBOS`
3. Debe aparecer: "Seleccione la Vista de Perfil donde están los tubos:"
4. Selecciona tu vista de perfil
5. Selecciona los bloques 'Punta1'
6. ¡Verás la ventana con las coordenadas correctas!

### Si no funciona:
1. Verifica la ruta en acad.lsp
2. Revisa que la DLL esté en la carpeta correcta
3. Comprueba que la versión de Civil 3D coincida

---

## 🔄 Actualización

Para actualizar a una nueva versión:

1. Compila la nueva versión
2. **Cierra Civil 3D completamente**
3. Reemplaza la DLL en `C:\Civil3D_Plugins\Civil3D_TubeInfo\`
4. Abre Civil 3D nuevamente

⚠️ **Importante**: Civil 3D bloquea las DLLs cargadas. Debes cerrarlo para poder reemplazar el archivo.

---

## 📋 Checklist de Despliegue

- [ ] DLL compilada para x64
- [ ] DLL copiada a carpeta permanente
- [ ] acad.lsp creado/editado con ruta correcta
- [ ] Ruta usa doble backslash `\\`
- [ ] Civil 3D reiniciado
- [ ] Comando `INFOTUBOS` funciona
- [ ] Probado con bloques reales

---

## 🆘 Troubleshooting Rápido

| Problema | Solución |
|----------|----------|
| "Comando no reconocido" | DLL no cargada - verifica acad.lsp |
| "Could not load assembly" | Rutas incorrectas en .csproj |
| No selecciona bloques | Verifica nombre "Punta1" exacto |
| Ventana vacía | Bloque sin atributo NUMERO |

---

## 📞 Comandos Útiles en Civil 3D

```
NETLOAD          - Cargar DLL manualmente
NETUNLOAD        - Descargar DLL
APPLOAD          - Cargar aplicaciones
LIST             - Ver propiedades de objeto seleccionado
```

---

**¿Listo para usar? ¡Empieza con `INFOTUBOS`!** 🎯
