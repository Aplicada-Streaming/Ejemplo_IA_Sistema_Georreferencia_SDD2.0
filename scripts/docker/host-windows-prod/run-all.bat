@echo off
REM run-all.bat — levanta el stack completo (db + backend + web) con dependencias resueltas.
setlocal
call "%~dp0_common.bat" || exit /b 1

echo ===========================================
echo Levantando stack SGR
echo ===========================================
docker compose -f "%~dp0docker-compose.yml" --env-file "%~dp0.env" up -d
if errorlevel 1 (
    echo *** Compose up FALLO.
    exit /b 1
)

echo.
echo Stack levantado. Status:
docker compose -f "%~dp0docker-compose.yml" --env-file "%~dp0.env" ps

echo.
echo URLs:
for /f "usebackq tokens=1,2 delims==" %%A in ("%~dp0.env") do (
    if /I "%%A"=="HOST_IP" (
        echo   Backend API:  http://%%B:5000
        echo   Web:          http://%%B:5100
        echo   SQL Server:   %%B,1433
    )
)
endlocal
