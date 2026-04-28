@echo off
REM stop-local.bat — Detiene el stack local levantado por run-local.bat.
REM Preserva el volume sgr-db-data, asi que la DB no pierde estado al re-arrancar.

setlocal
cd /d %~dp0

set COMPOSE=%~dp0..\host-windows-prod\docker-compose.yml
set ENVFILE=%~dp0.env.local

if not exist "%ENVFILE%" (
    echo No existe %ENVFILE% — el stack no fue levantado por run-local.bat.
    exit /b 0
)

docker compose -f "%COMPOSE%" --env-file "%ENVFILE%" down
echo Stack detenido. Datos persisten en el volume sgr-db-data.
endlocal
