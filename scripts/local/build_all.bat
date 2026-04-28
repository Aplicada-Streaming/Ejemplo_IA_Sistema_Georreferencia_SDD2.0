@echo off
setlocal enabledelayedexpansion
REM ============================================================
REM  SGR -- Build completo (servicio + frontend web + app movil)
REM  Uso: scripts\local\build_all.bat
REM ============================================================

echo.
echo ============================================================
echo   SGR -- Build Completo
echo ============================================================
echo.

call "%~dp0build_service.bat"
if %ERRORLEVEL% neq 0 goto :error

call "%~dp0invoke_build_front.bat"
if %ERRORLEVEL% neq 0 goto :error

call "%~dp0build_app.bat"
if %ERRORLEVEL% neq 0 goto :error

echo.
echo ============================================================
echo   Build completo OK:
echo     - Sgr.Backend.Api (Debug)
echo     - Sgr.Frontend.Web (Debug)
echo     - Sgr.Frontend.Mobile (net10.0-android, Debug)
echo ============================================================
echo.

goto :end
:error
echo.
echo ============================================================
echo   ERROR: El build completo fallo. Ver errores arriba.
echo ============================================================
exit /b 1
:end
endlocal
exit /b 0
