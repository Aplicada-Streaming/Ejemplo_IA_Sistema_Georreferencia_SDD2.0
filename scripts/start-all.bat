@echo off
REM start-all.bat
REM Levanta el stack mínimo de desarrollo: LocalDB + Backend API.
REM Cada servicio se abre en su propia ventana para mantener logs separados.
REM
REM A medida que se incorporan los siguientes componentes (frontend web Blazor,
REM workers, etc.) cada uno se sumara con su propio start-*.bat invocado desde aqui.

setlocal
cd /d %~dp0

echo =============================================
echo Iniciando stack SGR (modo desarrollo local)
echo =============================================

echo [1/2] LocalDB...
call start-db.bat
if %errorlevel% neq 0 (
    echo ERROR al iniciar la base de datos.
    exit /b 1
)

echo [2/2] Backend API en ventana separada...
start "SGR Backend API" cmd /k start-backend.bat

echo.
echo Stack levantado. Backend API: http://localhost:5000
echo Para detener: cerrar la ventana del backend.
endlocal
