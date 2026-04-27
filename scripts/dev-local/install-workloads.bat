@echo off
REM install-workloads.bat
REM Instala los workloads .NET necesarios para el proyecto.
REM IMPORTANTE: ejecutar como ADMINISTRADOR.
REM
REM El cd a la raiz del repo asegura que dotnet tome global.json
REM y aplique los workloads al SDK pinneado (.NET 8.0.x), NO al
REM SDK mas reciente. Esto evita el bug clasico de instalar maui-android
REM en un feature band distinto al que usan los proyectos.

setlocal
cd /d %~dp0..\..

echo === Verificando version de SDK que usa este repo ===
dotnet --version
echo.

echo === Instalando maui-android (workload requerido para Sgr.Frontend.Mobile) ===
dotnet workload install maui-android
if %errorlevel% neq 0 (
    echo.
    echo *** Instalacion FALLO. Verifica:
    echo     1. Estas en una PowerShell/CMD ADMIN
    echo     2. dotnet --version arriba muestra 8.0.x ^(no 10.x^)
    exit /b %errorlevel%
)

echo.
echo === Workloads instalados ===
dotnet workload list

endlocal
