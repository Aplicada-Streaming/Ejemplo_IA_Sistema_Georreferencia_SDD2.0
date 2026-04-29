@echo off
setlocal enabledelayedexpansion
REM ============================================================
REM  SGR -- Levanta el stack completo:
REM    1) Build serial de servicio + front + app movil
REM       (build_all.bat -- evita race de NuGet entre ventanas)
REM    2) API           (ventana separada, via invoke_run_service.bat)
REM       Espera por HTTP poll a /swagger/index.html
REM    3) Frontend Web  (ventana separada, via invoke_run_front.bat)
REM       Espera por HTTP poll a /
REM    4) App movil     (build + install + launch en USB, via run_app.bat)
REM
REM  Las ventanas hijas reciben SGR_SKIP_BUILD=1 para evitar
REM  recompilar lo que ya construyo build_all.bat.
REM
REM  Uso: scripts\local\run_all.bat
REM ============================================================

echo.
echo ============================================================
echo   SGR -- Arrancando stack completo
echo ============================================================
echo.

REM Matar procesos previos
echo Deteniendo procesos previos...
taskkill /F /IM Sgr.Backend.Api.exe /T 2>nul
taskkill /F /IM Sgr.Frontend.Web.exe /T 2>nul
echo.

REM ------------------------------------------------------------
REM  [1/4] Build serial (servicio + front + app movil)
REM        Ejecuta los 3 builds uno detras de otro en esta misma
REM        ventana, sin race de NuGet entre ventanas paralelas.
REM ------------------------------------------------------------
echo [1/4] Build serial de todos los proyectos...
call "%~dp0build_all.bat"
if %ERRORLEVEL% neq 0 goto :error

REM Indicar a run_service.bat / run_front.bat que NO recompilen
set "SGR_SKIP_BUILD=1"

REM ------------------------------------------------------------
REM  [2/4] API en ventana separada
REM ------------------------------------------------------------
echo.
echo [2/4] Iniciando Sgr.Backend.Api en http://localhost:5000 ...
start "Sgr.Backend.Api" /D "%~dp0" cmd /k call invoke_run_service.bat

echo Esperando a que el backend responda en localhost:5000...
call :wait_for_http http://localhost:5000/swagger/index.html backend
if %ERRORLEVEL% neq 0 goto :error

REM ------------------------------------------------------------
REM  [3/4] Frontend Web en ventana separada
REM ------------------------------------------------------------
echo.
echo [3/4] Iniciando Sgr.Frontend.Web en http://localhost:5100 ...
start "Sgr.Frontend.Web" /D "%~dp0" cmd /k call invoke_run_front.bat

echo Esperando a que el frontend responda en localhost:5100...
call :wait_for_http http://localhost:5100/ frontend
if %ERRORLEVEL% neq 0 goto :error

REM ------------------------------------------------------------
REM  [4/4] App movil (en esta misma ventana, para ver el resultado del install ADB)
REM ------------------------------------------------------------
echo.
echo [4/4] Deploy de Sgr.Frontend.Mobile via USB ...
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

REM ============================================================
REM  Subrutina :wait_for_http URL ETIQUETA
REM    Hace polling con curl cada 2s hasta que la URL responda OK
REM    o se alcance MAX (180s -- cubre cold starts pesimistas).
REM    Imprime feedback cada 10s y sugiere abrir la ventana del
REM    proceso despues de 30s sin respuesta.
REM    Devuelve errorlevel 0 si OK, 1 si timeout.
REM    Requiere curl (built-in en Windows 10+).
REM ============================================================
:wait_for_http
set "_URL=%~1"
set "_LABEL=%~2"
set /a "_MAX=180"
set /a "_WAITED=0"
set /a "_HINT_SHOWN=0"
:wait_for_http_loop
curl -fsL --max-time 2 "%_URL%" >nul 2>&1
if not errorlevel 1 (
    echo   %_LABEL% listo en !_WAITED!s ^(%_URL%^)
    exit /b 0
)
if !_WAITED! geq !_MAX! (
    echo   ERROR: %_LABEL% no respondio en %_MAX%s ^(URL: %_URL%^)
    echo   Revisar la ventana del proceso para ver el log.
    exit /b 1
)
set /a "_MOD10=!_WAITED! %% 10"
if !_WAITED! gtr 0 if !_MOD10! equ 0 echo     ...esperando %_LABEL% ^(!_WAITED!s/%_MAX%s^)
if !_WAITED! geq 30 if !_HINT_SHOWN! equ 0 (
    echo     ^(si tarda, abri la ventana titulada con el proceso para ver el log^)
    set /a "_HINT_SHOWN=1"
)
timeout /t 2 /nobreak >nul
set /a "_WAITED+=2"
goto :wait_for_http_loop

:error
echo.
echo ============================================================
echo   ERROR: No se pudo levantar el stack completo.
echo ============================================================
exit /b 1
:end
endlocal
exit /b 0
