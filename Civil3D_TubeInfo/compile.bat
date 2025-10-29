@echo off
REM Script de Compilacion Automatica para Civil3D_TubeInfo
REM Este script debe ser ejecutado por Claude Code

echo ========================================
echo   COMPILACION AUTOMATICA
echo   Civil3D_TubeInfo
echo ========================================
echo.

REM ====================================
REM PASO 1: Detectar MSBuild
REM ====================================
echo [1/5] Buscando MSBuild...

set MSBUILD_PATH=
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe
    echo   ^> MSBuild encontrado: VS 2022 Community
    goto BUILD
)

if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe
    echo   ^> MSBuild encontrado: VS 2022 Professional
    goto BUILD
)

if exist "C:\Program Files\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe
    echo   ^> MSBuild encontrado: VS 2019 Community
    goto BUILD
)

echo.
echo ERROR: MSBuild no encontrado
echo Instala Visual Studio 2019/2022 o Build Tools
echo Descarga: https://visualstudio.microsoft.com/downloads/
pause
exit /b 1

:BUILD
REM ====================================
REM PASO 2: Limpiar compilaciones previas
REM ====================================
echo.
echo [2/5] Limpiando compilaciones anteriores...
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj
echo   ^> Limpieza completada

REM ====================================
REM PASO 3: Compilar proyecto
REM ====================================
echo.
echo [3/5] Compilando proyecto...
echo   Configuracion: Release
echo   Plataforma: x64
echo.

"%MSBUILD_PATH%" "Civil3D_TubeInfo.sln" /p:Configuration=Release /p:Platform=x64 /t:Build /v:minimal /nologo

if errorlevel 1 (
    echo.
    echo ERROR: La compilacion fallo
    echo Revisa los errores anteriores
    pause
    exit /b 1
)

echo   ^> Compilacion exitosa

REM ====================================
REM PASO 4: Verificar DLL
REM ====================================
echo.
echo [4/5] Verificando DLL generada...
if not exist "bin\x64\Civil3D_TubeInfo.dll" (
    echo ERROR: DLL no encontrada en bin\x64\
    pause
    exit /b 1
)
echo   ^> DLL encontrada: bin\x64\Civil3D_TubeInfo.dll

REM ====================================
REM PASO 5: Desplegar
REM ====================================
echo.
echo [5/5] Preparando despliegue...

set DEPLOY_PATH=C:\Civil3D_Plugins\Civil3D_TubeInfo

if not exist "%DEPLOY_PATH%" (
    mkdir "%DEPLOY_PATH%"
    echo   ^> Carpeta de despliegue creada
)

copy /Y "bin\x64\Civil3D_TubeInfo.dll" "%DEPLOY_PATH%\" >nul
if errorlevel 1 (
    echo ERROR: No se pudo copiar la DLL
    pause
    exit /b 1
)

echo   ^> DLL copiada a: %DEPLOY_PATH%

REM ====================================
REM FINALIZADO
REM ====================================
echo.
echo ========================================
echo   COMPILACION COMPLETADA
echo ========================================
echo.
echo DLL ubicada en:
echo   %DEPLOY_PATH%\Civil3D_TubeInfo.dll
echo.
echo COMO USAR:
echo   1. Abre Civil 3D
echo   2. Comando: NETLOAD
echo   3. Selecciona: %DEPLOY_PATH%\Civil3D_TubeInfo.dll
echo   4. Usa el comando: INFOTUBOS
echo.
echo Para carga automatica, consulta GUIA_RAPIDA.md
echo.
pause
