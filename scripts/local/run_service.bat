@echo off
setlocal enabledelayedexpansion


REM Matar procesos previos
echo Deteniendo procesos previos...
taskkill /F /IM Sgr.Backend.Api.exe /T 2>nul
echo.


REM ============================================================
REM  SGR -- Build + run del servicio API en http://localhost:5000
REM  Uso: scripts\local\run_service.bat  (Ctrl+C para detener)
REM
REM  Hereda variables de entorno (DB, JWT, etc) del proceso que
REM  lo invoca. Para setearlas, llamar via invoke_run_service.bat.
REM ============================================================

set "PROJECT_ROOT=%~dp0..\.."
set "API_PROJECT=%PROJECT_ROOT%\src\Sgr.Backend.Api\Sgr.Backend.Api.csproj"

REM Build previo (omitir si SGR_SKIP_BUILD=1 -- lo usa run_all.bat
REM cuando ya hizo build_all.bat serial antes de arrancar el stack)
if "%SGR_SKIP_BUILD%"=="1" (
    echo Saltando build de Sgr.Backend.Api ^(SGR_SKIP_BUILD=1^)...
    echo.
) else (
    call "%~dp0build_service.bat"
    if !ERRORLEVEL! neq 0 goto :error
)

REM Matar instancia previa si existe
taskkill /F /IM Sgr.Backend.Api.exe /T 2>nul

if not defined ASPNETCORE_URLS set "ASPNETCORE_URLS=http://localhost:5000"
if not defined ASPNETCORE_ENVIRONMENT set "ASPNETCORE_ENVIRONMENT=Development"

echo.
echo ============================================================
echo   SGR Backend API iniciando en:
echo     URL:         %ASPNETCORE_URLS%
echo     Swagger:     %ASPNETCORE_URLS%/swagger
echo     Environment: %ASPNETCORE_ENVIRONMENT%
echo     Ctrl+C para detener
echo ============================================================
echo.

dotnet run --project "%API_PROJECT%" --no-launch-profile --no-build
if %ERRORLEVEL% neq 0 goto :error

goto :end
:error
echo.
echo ============================================================
echo   ERROR: No se pudo iniciar Sgr.Backend.Api
echo ============================================================
exit /b 1
:end
endlocal
exit /b 0
