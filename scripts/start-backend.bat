@echo off
REM start-backend.bat
REM Inicia el backend API en localhost:5000 (HTTP, dev).
REM Aplica migraciones automaticamente al arrancar y ejecuta seeds.

setlocal
cd /d %~dp0..

set ASPNETCORE_ENVIRONMENT=Development
set ASPNETCORE_URLS=http://localhost:5000

echo ===================================
echo Iniciando SGR Backend API
echo URL: http://localhost:5000
echo Swagger: http://localhost:5000/swagger
echo ===================================

dotnet run --project src/Sgr.Backend.Api/Sgr.Backend.Api.csproj --no-launch-profile

endlocal
