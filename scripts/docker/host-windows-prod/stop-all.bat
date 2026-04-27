@echo off
REM stop-all.bat — Detiene y borra los containers (preserva volumes).
setlocal
call "%~dp0_common.bat" || exit /b 1
docker compose -f "%~dp0docker-compose.yml" --env-file "%~dp0.env" down
echo Stack detenido. Datos persisten en el volume sgr-db-data.
endlocal
