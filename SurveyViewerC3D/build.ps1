# ============================================================
# Build Script - Survey Viewer para Civil 3D
# ============================================================

$ErrorActionPreference = "Stop"

# Rutas
$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SourceFile = "$ProjectDir\SurveyViewerPlugin.cs"
$OutputDll = "$ProjectDir\SurveyViewerC3D.dll"
$SQLiteDll = "$ProjectDir\libs\System.Data.SQLite.dll"
$SQLiteInterop = "$ProjectDir\libs\SQLite.Interop.dll"

# Bundle destino
$BundleDir = "C:\ProgramData\Autodesk\ApplicationPlugins\SurveyViewerC3D.bundle\Contents\Win64"
$BundleRoot = "C:\ProgramData\Autodesk\ApplicationPlugins\SurveyViewerC3D.bundle"

# Compilador Roslyn (VS 2022)
$csc = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
if (-not (Test-Path $csc)) {
    Write-Host "ERROR: No se encontro Roslyn csc.exe" -ForegroundColor Red
    Write-Host "Buscando alternativas..."
    $csc = Get-ChildItem "C:\Program Files\Microsoft Visual Studio" -Recurse -Filter "csc.exe" -ErrorAction SilentlyContinue |
           Where-Object { $_.FullName -like "*Roslyn*" } |
           Select-Object -First 1 -ExpandProperty FullName
    if (-not $csc) {
        Write-Host "ERROR: No se encontro ningun compilador C#" -ForegroundColor Red
        exit 1
    }
    Write-Host "Usando: $csc" -ForegroundColor Yellow
}

# Referencias AutoCAD/Civil 3D
$AcadDir = "C:\Program Files\Autodesk\AutoCAD 2024"
$C3DDir = "$AcadDir\C3D"

$refs = @(
    "$AcadDir\acmgd.dll",
    "$AcadDir\acdbmgd.dll",
    "$AcadDir\accoremgd.dll",
    "$C3DDir\AeccDbMgd.dll",
    "$SQLiteDll"
)

# Verificar referencias
foreach ($ref in $refs) {
    if (-not (Test-Path $ref)) {
        Write-Host "ERROR: No se encontro: $ref" -ForegroundColor Red
        exit 1
    }
}

# .NET Framework references
$fxDir = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
$refAssemblies = "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"

$fxRefs = @(
    "$refAssemblies\PresentationCore.dll",
    "$refAssemblies\PresentationFramework.dll",
    "$refAssemblies\WindowsBase.dll",
    "$refAssemblies\System.Xaml.dll",
    "$refAssemblies\System.dll",
    "$refAssemblies\System.Core.dll",
    "$refAssemblies\System.Data.dll",
    "$refAssemblies\System.Data.DataSetExtensions.dll"
)

foreach ($ref in $fxRefs) {
    if (-not (Test-Path $ref)) {
        Write-Host "WARN: No se encontro: $ref" -ForegroundColor Yellow
    }
}

$allRefs = $refs + $fxRefs
$refArgs = ($allRefs | ForEach-Object { "/reference:`"$_`"" }) -join " "

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  COMPILANDO SURVEY VIEWER C3D" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Compilar
$args = @(
    "/target:library",
    "/out:`"$OutputDll`"",
    "/langversion:7.3",
    "/platform:x64",
    "/optimize+",
    "/nologo"
)

$cmdLine = "`"$csc`" $($args -join ' ') $refArgs `"$SourceFile`""
Write-Host "Compilando..." -ForegroundColor Yellow
Invoke-Expression "& $cmdLine"

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "ERROR: La compilacion fallo." -ForegroundColor Red
    exit 1
}

Write-Host "Compilacion exitosa!" -ForegroundColor Green
Write-Host "DLL: $OutputDll" -ForegroundColor Green

# Crear directorio del bundle
if (-not (Test-Path $BundleDir)) {
    New-Item -ItemType Directory -Path $BundleDir -Force | Out-Null
}

# Copiar archivos al bundle
Copy-Item $OutputDll $BundleDir -Force
Copy-Item $SQLiteDll $BundleDir -Force
if (Test-Path $SQLiteInterop) {
    Copy-Item $SQLiteInterop $BundleDir -Force
}

Write-Host ""
Write-Host "Archivos copiados a:" -ForegroundColor Green
Write-Host "  $BundleDir" -ForegroundColor Green

# Crear PackageContents.xml si no existe
$pkgXml = "$BundleRoot\PackageContents.xml"
if (-not (Test-Path $pkgXml)) {
    @"
<?xml version="1.0" encoding="utf-8"?>
<ApplicationPackage SchemaVersion="1.0"
                    Name="SurveyViewerC3D"
                    Description="Survey Database Viewer para Civil 3D"
                    Author="Anibal"
                    AppVersion="1.0.0"
                    ProductCode="{B2C3D4E5-F6A7-8901-B2C3-D4E5F6A78901}">
  <CompanyDetails Name="Anibal" />
  <RuntimeRequirements OS="Win64"
                       Platform="AutoCAD|Civil3D"
                       SeriesMin="R24.0"
                       SeriesMax="R25.1" />
  <Components>
    <ComponentEntry AppName="SurveyViewerC3D"
                    Version="1.0.0"
                    ModuleName="./Contents/Win64/SurveyViewerC3D.dll"
                    AppDescription="Survey Database Viewer"
                    LoadOnAutoCADStartup="True"
                    LoadOnCommandInvocation="False">
      <Commands>
        <Command Global="SURVEYVIEWER" Local="SURVEYVIEWER" />
      </Commands>
    </ComponentEntry>
  </Components>
</ApplicationPackage>
"@ | Out-File -FilePath $pkgXml -Encoding utf8
    Write-Host "PackageContents.xml creado" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  INSTALACION COMPLETA" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Comando en Civil 3D: SURVEYVIEWER" -ForegroundColor Cyan
Write-Host ""
Write-Host "NOTA: Reinicie Civil 3D para cargar el plugin." -ForegroundColor Yellow
Write-Host ""
