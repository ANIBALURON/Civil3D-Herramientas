# 🎨 GUÍA DE PERSONALIZACIÓN

Esta guía te ayudará a personalizar la aplicación según tus necesidades específicas.

---

## 📝 Cambiar el Nombre del Bloque

Si tus bloques no se llaman "Punta1", sigue estos pasos:

### Archivo: `TubeInfoCommands.cs`

**Ubicación 1** - Línea ~26 (Filtro de selección):
```csharp
// CAMBIAR ESTO:
new TypedValue((int)DxfCode.BlockName, "Punta1")

// POR ESTO (ejemplo con "MiBloque"):
new TypedValue((int)DxfCode.BlockName, "MiBloque")
```

**Ubicación 2** - Línea ~62 (Validación del bloque):
```csharp
// CAMBIAR ESTO:
if (blockRef != null && blockRef.Name == "Punta1")

// POR ESTO:
if (blockRef != null && blockRef.Name == "MiBloque")
```

**Ubicación 3** - Mensajes al usuario (líneas ~24, ~90):
```csharp
// Opcional - actualizar mensajes
pso.MessageForAdding = "\nSeleccione bloques 'MiBloque':";
```

---

## 🔢 Cambiar el Atributo a Leer

Por defecto se lee el atributo "NUMERO". Para cambiar:

### Archivo: `TubeInfoCommands.cs`

**Línea ~68** - Lectura de atributo:
```csharp
// CAMBIAR ESTO:
if (attRef != null && attRef.Tag.ToUpper() == "NUMERO")
{
    tubeData.Numero = attRef.TextString;
    break;
}

// POR ESTO (ejemplo con "ID"):
if (attRef != null && attRef.Tag.ToUpper() == "ID")
{
    tubeData.Numero = attRef.TextString;
    break;
}
```

---

## 📊 Cambiar Formato de Decimales

Por defecto se muestran 3 decimales (F3).

### Archivo: `TubeInfoCommands.cs`

**Líneas ~98-100**:
```csharp
// CAMBIAR ESTO (3 decimales):
public string XFormatted => $"{X:F3}";
public string YFormatted => $"{Y:F3}";
public string ZFormatted => $"{Z:F3}";

// POR ESTO (2 decimales):
public string XFormatted => $"{X:F2}";
public string YFormatted => $"{Y:F2}";
public string ZFormatted => $"{Z:F2}";

// O ESTO (sin decimales):
public string XFormatted => $"{X:F0}";
public string YFormatted => $"{Y:F0}";
public string ZFormatted => $"{Z:F0}";
```

---

## 🎨 Cambiar Colores de la Interfaz

### Archivo: `TubeInfoForm.cs`

**Color del Header** - Línea ~34:
```csharp
// Azul (por defecto):
BackColor = Color.FromArgb(0, 102, 204)

// Verde oscuro:
BackColor = Color.FromArgb(0, 102, 68)

// Rojo oscuro:
BackColor = Color.FromArgb(153, 0, 0)

// Gris oscuro:
BackColor = Color.FromArgb(45, 45, 48)
```

**Color de Botón Exportar** - Línea ~104:
```csharp
// Verde (por defecto):
BackColor = Color.FromArgb(0, 153, 76)

// Azul:
BackColor = Color.FromArgb(0, 120, 215)

// Naranja:
BackColor = Color.FromArgb(255, 140, 0)
```

**Color de Filas Alternadas** - Línea ~76:
```csharp
// Azul muy claro (por defecto):
BackColor = Color.FromArgb(240, 248, 255)

// Gris muy claro:
BackColor = Color.FromArgb(245, 245, 245)

// Verde muy claro:
BackColor = Color.FromArgb(240, 255, 240)
```

---

## 📏 Cambiar Tamaño de la Ventana

### Archivo: `TubeInfoForm.cs`

**Línea ~23**:
```csharp
// Por defecto: 900x650
this.Size = new Size(900, 650);

// Más grande:
this.Size = new Size(1200, 800);

// Más pequeña:
this.Size = new Size(700, 500);
```

**Tamaño mínimo** - Línea ~25:
```csharp
this.MinimumSize = new Size(700, 400);
```

---

## ➕ Agregar Más Atributos

Para mostrar atributos adicionales como IDNUM, PROXNUM, etc:

### 1. Actualizar la Clase TubeData

**Archivo**: `TubeInfoCommands.cs` - Línea ~92:

```csharp
public class TubeData
{
    public string Numero { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    
    // AGREGAR NUEVOS ATRIBUTOS AQUÍ:
    public string IdNum { get; set; }
    public string ProxNum { get; set; }
    public string Descripcion { get; set; }
    
    // Propiedades formateadas
    public string NumeroDisplay => Numero ?? "S/N";
    public string XFormatted => $"{X:F3}";
    public string YFormatted => $"{Y:F3}";
    public string ZFormatted => $"{Z:F3}";
}
```

### 2. Leer los Atributos

**Archivo**: `TubeInfoCommands.cs` - Línea ~65:

```csharp
AttributeCollection attCol = blockRef.AttributeCollection;
foreach (ObjectId attId in attCol)
{
    AttributeReference attRef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
    if (attRef != null)
    {
        switch (attRef.Tag.ToUpper())
        {
            case "NUMERO":
                tubeData.Numero = attRef.TextString;
                break;
            // AGREGAR MÁS CASOS:
            case "IDNUM":
                tubeData.IdNum = attRef.TextString;
                break;
            case "PROXNUM":
                tubeData.ProxNum = attRef.TextString;
                break;
            case "DESCRIPCION":
                tubeData.Descripcion = attRef.TextString;
                break;
        }
    }
}
```

### 3. Agregar Columnas a la Tabla

**Archivo**: `TubeInfoForm.cs` - Después de línea ~116:

```csharp
// Después de las columnas existentes, agregar:
dataGridView.Columns.Add(new DataGridViewTextBoxColumn
{
    HeaderText = "ID NUM",
    Name = "IdNum",
    DefaultCellStyle = new DataGridViewCellStyle 
    { 
        Alignment = DataGridViewContentAlignment.MiddleCenter
    }
});

dataGridView.Columns.Add(new DataGridViewTextBoxColumn
{
    HeaderText = "PRÓX. NUM",
    Name = "ProxNum",
    DefaultCellStyle = new DataGridViewCellStyle 
    { 
        Alignment = DataGridViewContentAlignment.MiddleCenter
    }
});
```

### 4. Agregar Datos a las Filas

**Archivo**: `TubeInfoForm.cs` - Línea ~143:

```csharp
// CAMBIAR ESTO:
dataGridView.Rows.Add(
    tube.NumeroDisplay,
    tube.XFormatted,
    tube.YFormatted,
    tube.ZFormatted
);

// POR ESTO:
dataGridView.Rows.Add(
    tube.NumeroDisplay,
    tube.XFormatted,
    tube.YFormatted,
    tube.ZFormatted,
    tube.IdNum,
    tube.ProxNum
);
```

### 5. Actualizar la Exportación CSV

**Archivo**: `TubeInfoForm.cs` - Líneas ~191-201:

```csharp
// Encabezado:
writer.WriteLine("NUMERO;X;Y;Z;ID_NUM;PROX_NUM");

// Datos:
writer.WriteLine($"{tube.NumeroDisplay};{tube.X:F3};{tube.Y:F3};{tube.Z:F3};{tube.IdNum};{tube.ProxNum}");
```

---

## 📄 Cambiar Separador CSV

Por defecto usa punto y coma (;). Para cambiar a coma (,):

### Archivo: `TubeInfoForm.cs`

**Línea ~191** (Encabezado):
```csharp
// Punto y coma:
writer.WriteLine("NUMERO;X;Y;Z");

// Coma:
writer.WriteLine("NUMERO,X,Y,Z");
```

**Línea ~196** (Datos):
```csharp
// Punto y coma:
writer.WriteLine($"{tube.NumeroDisplay};{tube.X:F3};{tube.Y:F3};{tube.Z:F3}");

// Coma:
writer.WriteLine($"{tube.NumeroDisplay},{tube.X:F3},{tube.Y:F3},{tube.Z:F3}");
```

---

## 🔤 Cambiar Fuentes

### Archivo: `TubeInfoForm.cs`

**Fuente de encabezados** - Línea ~86:
```csharp
// Por defecto: Segoe UI, 11, Bold
ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);

// Más grande:
ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 13, FontStyle.Bold);

// Arial:
ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 11, FontStyle.Bold);
```

**Fuente de celdas** - Línea ~81:
```csharp
DefaultCellStyle.Font = new Font("Segoe UI", 10);

// Más grande:
DefaultCellStyle.Font = new Font("Segoe UI", 11);
```

---

## 🔧 Cambiar el Nombre del Comando

Por defecto el comando es `INFOTUBOS`.

### Archivo: `TubeInfoCommands.cs`

**Línea ~11**:
```csharp
// Por defecto:
[CommandMethod("INFOTUBOS")]

// Cambiar a:
[CommandMethod("COORDTUBOS")]
// o
[CommandMethod("IT")]  // Comando corto
// o
[CommandMethod("LISTATUBOS")]
```

---

## 💾 Después de Cada Cambio

1. **Guardar** todos los archivos modificados
2. **Compilar** el proyecto: `Build > Build Solution` (Ctrl+Shift+B)
3. **Cerrar** Civil 3D completamente
4. **Reemplazar** la DLL en la carpeta de plugins
5. **Abrir** Civil 3D y probar los cambios

---

## ⚠️ Notas Importantes

- Siempre prueba los cambios en un archivo de prueba primero
- Guarda una copia de respaldo antes de modificar
- Si algo no funciona, revisa los mensajes de error en la consola de Civil 3D
- Los cambios en el código requieren recompilar la DLL

---

## 🆘 Si Algo Sale Mal

1. Revisa que no haya errores de sintaxis (Visual Studio los marca en rojo)
2. Verifica que los paréntesis, llaves y comillas estén balanceados
3. Comprueba que los nombres de variables coincidan exactamente
4. Lee los mensajes de error de compilación
5. Si nada funciona, restaura desde el código original

---

**¡Experimenta y personaliza a tu gusto!** 🚀
