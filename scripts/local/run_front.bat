@echo off
setlocal enabledelayedexpansion

REM Matar procesos previos
echo Deteniendo procesos previos...
taskkill /F /IM Sgr.Frontend.Web.exe /T 2>nul
echo.

REM ============================================================
REM  SGR -- Build + run del frontend web en http://localhost:5100
REM  Uso: scripts\local\run_front.bat  (Ctrl+C para detener)
REM
REM  Hereda variables de entorno (BackendApi__BaseUrl, etc) del
REM  proceso que lo invoca. Para setearlas, llamar via
REM  invoke_run_front.bat.
REM ============================================================

set "PROJECT_ROOT=%~dp0..\.."
set "WEB_PROJECT=%PROJECT_ROOT%\src\Sgr.Frontend.Web\Sgr.Frontend.Web.csproj"

REM Build previo
call "%~dp0build_front.bat"
if %ERRORLEVEL% neq 0 goto :error

if not defined ASPNETCORE_URLS set "ASPNETCORE_URLS=http://localhost:5100"
if not defined ASPNETCORE_ENVIRONMENT set "ASPNETCORE_ENVIRONMENT=Development"

echo.
echo ============================================================
echo   SGR Frontend Web iniciando en:
echo     URL:         %ASPNETCORE_URLS%
echo     Environment: %ASPNETCORE_ENVIRONMENT%
echo     Backend URL: %BackendApi__BaseUrl%
echo     Ctrl+C para detener
echo ============================================================
echo.

dotnet run --project "%WEB_PROJECT%" --no-launch-profile --no-build
if %ERRORLEVEL% neq 0 goto :error

goto :end
:error
echo.
echo ============================================================
echo   ERROR: No se pudo iniciar Sgr.Frontend.Web
echo ============================================================
exit /b 1
:end
endlocal
exit /b 0
