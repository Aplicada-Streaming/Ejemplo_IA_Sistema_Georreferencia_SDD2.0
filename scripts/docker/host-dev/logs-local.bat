@echo off
REM logs-local.bat — Sigue los logs de los 3 servicios del stack local en una sola consola.

setlocal
cd /d %~dp0

set COMPOSE=%~dp0..\host-windows-prod\docker-compose.yml
set ENVFILE=%~dp0.env.local

if not exist "%ENVFILE%" (
    echo No existe %ENVFILE% — el stack no fue levantado por run-local.bat.
    exit /b 1
)

docker compose -f "%COMPOSE%" --env-file "%ENVFILE%" logs -f --tail=50
endlocal
