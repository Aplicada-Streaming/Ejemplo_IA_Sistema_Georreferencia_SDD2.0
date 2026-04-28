@echo off
setlocal enabledelayedexpansion

REM Matar procesos previos
echo Deteniendo procesos previos...
taskkill /F /IM Sgr.Backend.Api.exe /T 2>nul
echo.


REM ============================================================
REM  SGR -- Wrapper que setea env vars (DB local en host DEV,
REM  autenticacion integrada de Windows) y delega en run_service.bat.
REM  Uso: scripts\local\invoke_run_service.bat
REM ============================================================

REM ------------------------------------------------------------
REM  Configuracion de la base de datos
REM    Host:       DEV (SQL Server local)
REM    Auth:       Windows / Trusted_Connection (usuario Windows actual)
REM ------------------------------------------------------------
set "SGR_DB_HOST=DEV"
set "SGR_DB_NAME=SgrDev"

set "ConnectionStrings__DefaultConnection=Server=%SGR_DB_HOST%;Database=%SGR_DB_NAME%;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"

REM ------------------------------------------------------------
REM  Configuracion del runtime ASP.NET
REM ------------------------------------------------------------
set "ASPNETCORE_ENVIRONMENT=Development"
set "ASPNETCORE_URLS=http://localhost:5000"

echo.
echo ============================================================
echo   SGR -- invoke_run_service
echo     DB host:     %SGR_DB_HOST%
echo     DB name:     %SGR_DB_NAME%
echo     DB auth:     Windows (%USERDOMAIN%\%USERNAME%)
echo     Environment: %ASPNETCORE_ENVIRONMENT%
echo     URL:         %ASPNETCORE_URLS%
echo ============================================================
echo.

call "%~dp0run_service.bat"
if %ERRORLEVEL% neq 0 goto :error

goto :end
:error
echo.
echo ============================================================
echo   ERROR: invoke_run_service fallo. Ver errores arriba.
echo ============================================================
exit /b 1
:end
endlocal
exit /b 0
