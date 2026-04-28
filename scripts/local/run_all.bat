@echo off
setlocal enabledelayedexpansion
REM ============================================================
REM  SGR -- Levanta el stack completo:
REM    1) API           (ventana separada, via invoke_run_service.bat)
REM    2) Frontend Web  (ventana separada, via invoke_run_front.bat)
REM    3) App movil     (build + install + launch en USB, via run_app.bat)
REM
REM  Uso: scripts\local\run_all.bat
REM ============================================================

echo.
echo ============================================================
echo   SGR -- Arrancando API + Frontend Web + Sgr.Frontend.Mobile
echo ============================================================
echo.

REM Matar procesos previos
echo Deteniendo procesos previos...
taskkill /F /IM Sgr.Backend.Api.exe /T 2>nul
taskkill /F /IM Sgr.Frontend.Web.exe /T 2>nul
echo.

REM ------------------------------------------------------------
REM  [1/3] API en ventana separada
REM ------------------------------------------------------------
echo [1/3] Iniciando Sgr.Backend.Api en http://localhost:5000 ...
start "Sgr.Backend.Api" cmd /k "call ""%~dp0invoke_run_service.bat"""

echo Esperando 8s a que el backend levante...
timeout /t 8 /nobreak > nul

REM ------------------------------------------------------------
REM  [2/3] Frontend Web en ventana separada
REM ------------------------------------------------------------
echo.
echo [2/3] Iniciando Sgr.Frontend.Web en http://localhost:5100 ...
start "Sgr.Frontend.Web" cmd /k "call ""%~dp0invoke_run_front.bat"""

echo Esperando 3s a que el frontend levante...
timeout /t 3 /nobreak > nul

REM ------------------------------------------------------------
REM  [3/3] App movil (en esta misma ventana, para ver el resultado del install ADB)
REM ------------------------------------------------------------
echo.
echo [3/3] Deploy de Sgr.Frontend.Mobile via USB ...
call "%~dp0run_app.bat"
if %ERRORLEVEL% neq 0 goto :error

echo.
echo ============================================================
echo   Stack levantado:
echo     API:     http://localhost:5000   (Swagger: /swagger)
echo     Web:     http://localhost:5100
echo     Mobile:  instalada y lanzada en el dispositivo USB
echo   Para detener: cerrar las ventanas "Sgr.Backend.Api" y "Sgr.Frontend.Web".
echo ============================================================
echo.

goto :end
:error
echo.
echo ============================================================
echo   ERROR: No se pudo levantar el stack completo.
echo ============================================================
exit /b 1
:end
endlocal
exit /b 0
