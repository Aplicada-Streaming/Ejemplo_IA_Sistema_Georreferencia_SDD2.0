@echo off
REM run-db.bat — levanta solo sgr-db.
setlocal
call "%~dp0_common.bat" || exit /b 1
docker compose -f "%~dp0docker-compose.yml" --env-file "%~dp0.env" up -d sgr-db
endlocal
