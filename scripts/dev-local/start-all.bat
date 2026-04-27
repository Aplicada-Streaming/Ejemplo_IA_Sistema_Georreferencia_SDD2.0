@echo off
REM start-all.bat
REM Levanta el stack de desarrollo: LocalDB + Backend API + Frontend Web.
REM Cada servicio se abre en su propia ventana para mantener logs separados.

setlocal
cd /d %~dp0

echo =============================================
echo Iniciando stack SGR (modo desarrollo local)
echo =============================================

echo [1/3] LocalDB...
call start-db.bat
if %errorlevel% neq 0 (
    echo ERROR al iniciar la base de datos.
    exit /b 1
)

echo [2/3] Backend API en ventana separada...
start "SGR Backend API" cmd /k start-backend.bat

echo Esperando 8s a que el backend levante...
timeout /t 8 /nobreak >nul

echo [3/3] Frontend Web en ventana separada...
start "SGR Frontend Web" cmd /k start-web.bat

echo.
echo Stack levantado.
echo   - Backend API:   http://localhost:5000   (Swagger: /swagger)
echo   - Frontend Web:  http://localhost:5100
echo Para detener: cerrar las ventanas correspondientes.
endlocal
