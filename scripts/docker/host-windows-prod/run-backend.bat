@echo off
REM run-backend.bat — levanta sgr-backend (compose se encarga de la dependencia con sgr-db).
setlocal
call "%~dp0_common.bat" || exit /b 1
docker compose -f "%~dp0docker-compose.yml" --env-file "%~dp0.env" up -d sgr-backend
endlocal
