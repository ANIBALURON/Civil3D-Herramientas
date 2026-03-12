# ============================================================
# Build Script - Loteo Civil 3D
# ============================================================

$ErrorActionPreference = "Stop"
$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Compilador Roslyn (VS 2022)
$csc = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = Get-ChildItem "C:\Program Files\Microsoft Visual Studio" -Recurse -Filter "csc.exe" -ErrorAction SilentlyContinue |
           Where-Object { $_.FullName -like "*Roslyn*" } |
           Select-Object -First 1 -ExpandProperty FullName
    if (-not $csc) {
        Write-Host "ERROR: No se encontro compilador C#" -ForegroundColor Red
        exit 1
    }
}

# Referencias AutoCAD/Civil 3D
$acad = "C:\Program Files\Autodesk\AutoCAD 2024"
$wpf = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF"
$netfx = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"

$refs = @(
    "$acad\accoremgd.dll",
    "$acad\Acdbmgd.dll",
    "$acad\Acmgd.dll",
    "$acad\AcWindows.dll",
    "$acad\AdWindows.dll",
    "$acad\C3D\AeccDbMgd.dll",
    "$acad\AecBaseMgd.dll"
)
$sysRefs = @(
    "$netfx\System.dll",
    "$netfx\System.Core.dll",
    "$netfx\System.Drawing.dll",
    "$netfx\System.Windows.Forms.dll",
    "$netfx\System.Xml.dll",
    "$wpf\WindowsBase.dll",
    "$wpf\PresentationCore.dll",
    "$wpf\PresentationFramework.dll"
)

$allRefs = $refs + $sysRefs
$refArgs = ($allRefs | ForEach-Object { "/reference:`"$_`"" }) -join " "

# Buscar archivos fuente
$sources = Get-ChildItem -Path "$ProjectDir\src" -Filter "*.cs" -Recurse | ForEach-Object { "`"$($_.FullName)`"" }
$srcArgs = $sources -join " "

$outDir = "$ProjectDir\bin\Release"
if (!(Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  COMPILANDO LOTEO CIVIL 3D" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$cmd = "& `"$csc`" /target:library /out:`"$outDir\LoteoCivil3D.dll`" /optimize+ /nologo /platform:x64 /langversion:7.3 $refArgs $srcArgs"
Write-Host "Compilando..." -ForegroundColor Yellow
Invoke-Expression $cmd

if ($LASTEXITCODE -eq 0) {
    Write-Host "Compilacion exitosa!" -ForegroundColor Green

    $dest = "C:\ProgramData\Autodesk\ApplicationPlugins\LoteoCivil3D.Bundle\Contents\Win64"
    if (!(Test-Path $dest)) { New-Item -ItemType Directory -Path $dest -Force | Out-Null }
    Copy-Item "$outDir\LoteoCivil3D.dll" $dest -Force

    $size = (Get-Item "$outDir\LoteoCivil3D.dll").Length / 1KB
    Write-Host "DLL: $outDir\LoteoCivil3D.dll ($([math]::Round($size,1)) KB)" -ForegroundColor Green
    Write-Host "Copiado a: $dest" -ForegroundColor Green
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  INSTALACION COMPLETA" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Comando en Civil 3D: LOTEO" -ForegroundColor Cyan
    Write-Host "NOTA: Reinicie Civil 3D para cargar el plugin." -ForegroundColor Yellow
    Write-Host ""
} else {
    Write-Host "ERROR en compilacion" -ForegroundColor Red
    exit 1
}
