@echo off
setlocal enabledelayedexpansion

REM Matar procesos previos
echo Deteniendo procesos previos...
taskkill /F /IM Sgr.Frontend.Web.exe /T 2>nul
echo.


REM ============================================================
REM  SGR -- Wrapper de build del frontend web. Mantiene simetria
REM  con invoke_run_front.bat. Para Blazor Server no hay env vars
REM  build-time relevantes; se setea Configuration y se delega en
REM  build_front.bat.
REM  Uso: scripts\local\invoke_build_front.bat
REM ============================================================

set "DOTNET_NOLOGO=1"
set "Configuration=Debug"

echo.
echo ============================================================
echo   SGR -- invoke_build_front
echo     Configuration: %Configuration%
echo ============================================================
echo.

call "%~dp0build_front.bat"
if %ERRORLEVEL% neq 0 goto :error

goto :end
:error
echo.
echo ============================================================
echo   ERROR: invoke_build_front fallo. Ver errores arriba.
echo ============================================================
exit /b 1
:end
endlocal
exit /b 0
