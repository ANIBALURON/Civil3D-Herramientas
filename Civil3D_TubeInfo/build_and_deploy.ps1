# Script de compilación y despliegue para Civil3D_TubeInfo
# Autor: Script automático
# Fecha: 2024

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  COMPILACIÓN Y DESPLIEGUE" -ForegroundColor Cyan
Write-Host "  Civil3D_TubeInfo" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configuración
$projectPath = $PSScriptRoot
$projectFile = Join-Path $projectPath "Civil3D_TubeInfo.csproj"
$solutionFile = Join-Path $projectPath "Civil3D_TubeInfo.sln"
$deployPath = "C:\Civil3D_Plugins\Civil3D_TubeInfo"

# Verificar que exista MSBuild
$msbuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

if (-not (Test-Path $msbuildPath)) {
    $msbuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
}

if (-not (Test-Path $msbuildPath)) {
    $msbuildPath = "C:\Program Files\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
}

if (-not (Test-Path $msbuildPath)) {
    Write-Host "❌ ERROR: No se encontró MSBuild.exe" -ForegroundColor Red
    Write-Host "   Asegúrate de tener Visual Studio 2019/2022 instalado" -ForegroundColor Yellow
    Read-Host "Presiona Enter para salir"
    exit 1
}

Write-Host "✓ MSBuild encontrado en:" -ForegroundColor Green
Write-Host "  $msbuildPath" -ForegroundColor Gray
Write-Host ""

# Limpiar compilaciones anteriores
Write-Host "🧹 Limpiando compilaciones anteriores..." -ForegroundColor Yellow
if (Test-Path (Join-Path $projectPath "bin")) {
    Remove-Item -Path (Join-Path $projectPath "bin") -Recurse -Force
}
if (Test-Path (Join-Path $projectPath "obj")) {
    Remove-Item -Path (Join-Path $projectPath "obj") -Recurse -Force
}
Write-Host "✓ Limpieza completada" -ForegroundColor Green
Write-Host ""

# Compilar proyecto
Write-Host "🔨 Compilando proyecto..." -ForegroundColor Yellow
$buildArgs = @(
    $solutionFile,
    "/p:Configuration=Release",
    "/p:Platform=x64",
    "/t:Build",
    "/v:minimal",
    "/nologo"
)

& $msbuildPath $buildArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "❌ ERROR: La compilación falló" -ForegroundColor Red
    Write-Host "   Revisa los errores anteriores" -ForegroundColor Yellow
    Read-Host "Presiona Enter para salir"
    exit 1
}

Write-Host "✓ Compilación exitosa" -ForegroundColor Green
Write-Host ""

# Verificar que exista la DLL
$dllPath = Join-Path $projectPath "bin\x64\Civil3D_TubeInfo.dll"
if (-not (Test-Path $dllPath)) {
    Write-Host "❌ ERROR: No se encontró la DLL compilada" -ForegroundColor Red
    Write-Host "   Ruta esperada: $dllPath" -ForegroundColor Yellow
    Read-Host "Presiona Enter para salir"
    exit 1
}

Write-Host "✓ DLL compilada encontrada" -ForegroundColor Green
Write-Host "  $dllPath" -ForegroundColor Gray
Write-Host ""

# Preguntar si desea desplegar
Write-Host "¿Deseas desplegar la DLL a $deployPath? (S/N): " -ForegroundColor Cyan -NoNewline
$response = Read-Host

if ($response -eq "S" -or $response -eq "s") {
    Write-Host ""
    Write-Host "📦 Desplegando DLL..." -ForegroundColor Yellow
    
    # Crear carpeta de despliegue si no existe
    if (-not (Test-Path $deployPath)) {
        New-Item -ItemType Directory -Path $deployPath -Force | Out-Null
        Write-Host "✓ Carpeta de despliegue creada" -ForegroundColor Green
    }
    
    # Verificar si Civil 3D está ejecutándose
    $civil3dProcess = Get-Process | Where-Object { $_.ProcessName -like "*acad*" }
    if ($civil3dProcess) {
        Write-Host ""
        Write-Host "⚠ ADVERTENCIA: Civil 3D está en ejecución" -ForegroundColor Yellow
        Write-Host "  Cierra Civil 3D antes de continuar para poder reemplazar la DLL" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "¿Continuar de todos modos? (S/N): " -ForegroundColor Cyan -NoNewline
        $continueResponse = Read-Host
        
        if ($continueResponse -ne "S" -and $continueResponse -ne "s") {
            Write-Host "❌ Despliegue cancelado" -ForegroundColor Red
            Read-Host "Presiona Enter para salir"
            exit 0
        }
    }
    
    # Copiar DLL
    try {
        Copy-Item -Path $dllPath -Destination $deployPath -Force
        Write-Host "✓ DLL copiada exitosamente" -ForegroundColor Green
        Write-Host "  Destino: $deployPath\Civil3D_TubeInfo.dll" -ForegroundColor Gray
        Write-Host ""
        
        # Verificar si existe acad.lsp
        $supportPath = "$env:APPDATA\Autodesk\C3D 2024\enu\Support"
        $acadLspPath = Join-Path $supportPath "acad.lsp"
        
        Write-Host "📝 Configuración de carga automática:" -ForegroundColor Cyan
        Write-Host "   Ruta de acad.lsp: $acadLspPath" -ForegroundColor Gray
        
        if (Test-Path $acadLspPath) {
            Write-Host "   ✓ Archivo acad.lsp encontrado" -ForegroundColor Green
            
            $acadLspContent = Get-Content $acadLspPath -Raw
            $loadCommand = '(command "NETLOAD" "' + $deployPath.Replace('\', '\\') + '\\Civil3D_TubeInfo.dll")'
            
            if ($acadLspContent -notmatch [regex]::Escape($loadCommand)) {
                Write-Host ""
                Write-Host "¿Agregar comando de carga automática a acad.lsp? (S/N): " -ForegroundColor Cyan -NoNewline
                $lispResponse = Read-Host
                
                if ($lispResponse -eq "S" -or $lispResponse -eq "s") {
                    Add-Content -Path $acadLspPath -Value "`r`n; InfoTubos Auto-Load"
                    Add-Content -Path $acadLspPath -Value $loadCommand
                    Write-Host "   ✓ Comando agregado a acad.lsp" -ForegroundColor Green
                }
            } else {
                Write-Host "   ✓ Comando de carga ya existe en acad.lsp" -ForegroundColor Green
            }
        } else {
            Write-Host "   ⚠ acad.lsp no encontrado" -ForegroundColor Yellow
            Write-Host "   Puedes crear uno manualmente según el README" -ForegroundColor Gray
        }
        
    } catch {
        Write-Host "❌ ERROR al copiar DLL: $_" -ForegroundColor Red
        Read-Host "Presiona Enter para salir"
        exit 1
    }
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  ✓ DESPLIEGUE COMPLETADO" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Próximos pasos:" -ForegroundColor Cyan
    Write-Host "1. Abre (o reinicia) Civil 3D" -ForegroundColor White
    Write-Host "2. Escribe el comando: INFOTUBOS" -ForegroundColor White
    Write-Host "3. Selecciona los bloques 'Punta1'" -ForegroundColor White
    Write-Host ""
    
} else {
    Write-Host ""
    Write-Host "ℹ Despliegue omitido" -ForegroundColor Yellow
    Write-Host "  La DLL se encuentra en: $dllPath" -ForegroundColor Gray
    Write-Host ""
}

Read-Host "Presiona Enter para salir"
