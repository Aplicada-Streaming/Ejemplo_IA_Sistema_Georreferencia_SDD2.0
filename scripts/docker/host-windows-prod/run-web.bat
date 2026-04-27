@echo off
REM run-web.bat — levanta sgr-web.
setlocal
call "%~dp0_common.bat" || exit /b 1
docker compose -f "%~dp0docker-compose.yml" --env-file "%~dp0.env" up -d sgr-web
endlocal
