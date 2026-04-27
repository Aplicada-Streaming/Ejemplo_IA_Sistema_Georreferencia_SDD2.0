@echo off
REM start-web.bat
REM Inicia el frontend Web Blazor Server en localhost:5100.
REM Apunta por default al backend en http://localhost:5000.

setlocal
cd /d %~dp0..

set ASPNETCORE_ENVIRONMENT=Development
set ASPNETCORE_URLS=http://localhost:5100

echo ===================================
echo Iniciando SGR Frontend Web (Blazor)
echo URL: http://localhost:5100
echo Backend esperado: http://localhost:5000
echo ===================================

dotnet run --project src/Sgr.Frontend.Web/Sgr.Frontend.Web.csproj --no-launch-profile

endlocal
