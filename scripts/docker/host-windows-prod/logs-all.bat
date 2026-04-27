@echo off
REM logs-all.bat — Sigue los logs de los 3 servicios en una sola consola.
setlocal
call "%~dp0_common.bat" || exit /b 1
docker compose -f "%~dp0docker-compose.yml" --env-file "%~dp0.env" logs -f --tail=50
endlocal
