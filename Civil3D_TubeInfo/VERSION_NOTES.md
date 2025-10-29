# 📝 NOTAS DE LA VERSIÓN

## Versión 1.0.0 - Release Inicial

### ✨ Características Principales

1. **Selección de Vista de Perfil**
   - Selecciona primero la vista de perfil donde están los bloques
   - Detecta automáticamente el alineamiento asociado

2. **Cálculo Correcto de Coordenadas**
   - Coordenadas X, Y calculadas respecto al alineamiento usando `Alignment.PointLocation()`
   - Coordenada Z obtenida de la elevación en el perfil usando `ProfileView.FindStationAndElevationAtXY()`
   - **IMPORTANTE**: Las coordenadas son reales del proyecto, no las del perfil

3. **Selección Múltiple de Bloques**
   - Selecciona varios bloques "Punta1" simultáneamente
   - Filtrado automático por nombre de bloque
   - Validación y manejo de errores robusto

4. **Visualización de Datos**
   - Tabla moderna y profesional
   - Muestra: NÚMERO, X, Y, Z
   - Ordenamiento automático por número
   - Formato de 3 decimales para coordenadas
   - Muestra el nombre del alineamiento en el header

5. **Exportación a CSV**
   - Botón de exportación integrado
   - Formato compatible con Excel
   - Incluye columna de ALINEAMIENTO
   - Separador: punto y coma (;)
   - Encoding UTF-8

### 🔧 Características Técnicas

- Compatible con Civil 3D 2020+
- Arquitectura x64
- .NET Framework 4.8
- Manejo de excepciones completo
- Logging de errores en consola

### 📊 Datos Mostrados

| Campo | Descripción | Formato |
|-------|-------------|---------|
| NÚMERO | Tag NUMERO del bloque | Texto |
| X | Coordenada X del alineamiento | 3 decimales |
| Y | Coordenada Y del alineamiento | 3 decimales |
| Z | Elevación del tubo en el perfil | 3 decimales |

**Nota importante:** Las coordenadas X, Y son las coordenadas reales del proyecto calculadas respecto al alineamiento, no las coordenadas de la vista de perfil.

### 🎯 Casos de Uso

1. **Revisión rápida de tubos**
   - Ver coordenadas de múltiples tubos sin seleccionar uno por uno
   - Identificar rangos de coordenadas

2. **Exportación de datos**
   - Generar reportes para Excel
   - Compartir información con otros software

3. **Control de calidad**
   - Verificar numeración de tubos
   - Detectar duplicados o saltos en numeración

---

## 🔄 CHANGELOG

### [1.0.0] - 2024-10-28

#### Added
- ✅ Comando `INFOTUBOS` para selección múltiple
- ✅ Ventana de visualización con DataGridView
- ✅ Exportación a CSV con formato personalizado
- ✅ Filtro automático de bloques "Punta1"
- ✅ Ordenamiento por número de tubo
- ✅ Manejo de bloques sin atributo NUMERO
- ✅ Interfaz moderna con paneles de color
- ✅ Estadísticas en tiempo real (total, rangos)
- ✅ Documentación completa (README, GUÍA RÁPIDA)

#### Technical Details
- Framework: .NET 4.8
- Language: C#
- Target: x64
- Dependencies: AutoCAD/Civil 3D APIs
- UI: Windows Forms

---

## 🚀 Próximas Versiones (Roadmap)

### Versión 1.1.0 (Planeada)
- [ ] Filtro de búsqueda en la tabla
- [ ] Opción para mostrar/ocultar columnas
- [ ] Cálculo de distancias entre tubos
- [ ] Exportación a Excel (.xlsx) directo

### Versión 1.2.0 (Planeada)
- [ ] Selección de bloques por rango de números
- [ ] Resaltado de tubos en el dibujo al hacer clic en la tabla
- [ ] Zoom automático a tubo seleccionado
- [ ] Copia de celdas al portapapeles

### Versión 2.0.0 (Futura)
- [ ] Soporte para múltiples tipos de bloques
- [ ] Cálculo de estaciones y offsets (opcional)
- [ ] Integración con alineamientos y perfiles
- [ ] Generación de reportes PDF
- [ ] Base de datos local para histórico

---

## 🐛 Problemas Conocidos

### Versión 1.0.0

1. **Limitación de nombre de bloque**
   - Solo funciona con bloques llamados exactamente "Punta1"
   - **Workaround**: Editar el código fuente para cambiar el nombre

2. **Atributo NUMERO requerido**
   - Si el bloque no tiene el atributo NUMERO, se asigna "S/N-X"
   - No afecta el funcionamiento, solo la visualización

3. **Sin validación de duplicados**
   - Si hay dos bloques con el mismo número, ambos se muestran
   - **Planeado para**: Versión 1.1.0

---

## 📋 Requerimientos del Sistema

### Mínimos
- Windows 10 (64-bit)
- Civil 3D 2020 o superior
- 4 GB RAM
- .NET Framework 4.8

### Recomendados
- Windows 11 (64-bit)
- Civil 3D 2023 o superior
- 8 GB RAM
- SSD

---

## 🤝 Contribuciones

Si quieres mejorar esta aplicación:

1. **Reporta bugs**: Documenta el problema con capturas
2. **Sugiere features**: Describe el caso de uso
3. **Comparte mejoras**: Documenta los cambios realizados

---

## 📞 Soporte

Para asistencia técnica, ten a mano:
- Versión de Civil 3D
- Sistema operativo
- Mensaje de error completo
- Pasos para reproducir el problema

---

**Última actualización**: 28 de Octubre, 2024
**Versión**: 1.0.0
**Estado**: Stable Release ✅
