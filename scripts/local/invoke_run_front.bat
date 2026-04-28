@echo off
setlocal enabledelayedexpansion

REM Matar procesos previos
echo Deteniendo procesos previos...
taskkill /F /IM Sgr.Frontend.Web.exe /T 2>nul
echo.


REM ============================================================
REM  SGR -- Wrapper que setea env vars (URL del backend API +
REM  puerto local) y delega en run_front.bat.
REM  Uso: scripts\local\invoke_run_front.bat
REM ============================================================

REM ------------------------------------------------------------
REM  URL del backend API (consumido por BackendApiOptions en
REM  Program.cs del frontend web). Override de "BackendApi:BaseUrl"
REM  via la convencion de env var BackendApi__BaseUrl.
REM ------------------------------------------------------------
set "BackendApi__BaseUrl=http://localhost:5000"

REM ------------------------------------------------------------
REM  Configuracion del runtime ASP.NET
REM ------------------------------------------------------------
set "ASPNETCORE_ENVIRONMENT=Development"
set "ASPNETCORE_URLS=http://localhost:5100"

echo.
echo ============================================================
echo   SGR -- invoke_run_front
echo     Backend URL: %BackendApi__BaseUrl%
echo     Environment: %ASPNETCORE_ENVIRONMENT%
echo     URL:         %ASPNETCORE_URLS%
echo ============================================================
echo.

call "%~dp0run_front.bat"
if %ERRORLEVEL% neq 0 goto :error

goto :end
:error
echo.
echo ============================================================
echo   ERROR: invoke_run_front fallo. Ver errores arriba.
echo ============================================================
exit /b 1
:end
endlocal
exit /b 0
