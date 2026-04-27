@echo off
REM start-db.bat
REM Inicia SQL Server LocalDB para desarrollo (instancia MSSQLLocalDB).
REM
REM Pre-requisito: SQL Server Express LocalDB instalado.
REM   https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb

setlocal

echo =============================
echo Iniciando SQL Server LocalDB
echo =============================
SqlLocalDB.exe info MSSQLLocalDB >nul 2>&1
if %errorlevel% neq 0 (
    echo La instancia MSSQLLocalDB no existe. Creandola...
    SqlLocalDB.exe create MSSQLLocalDB
)

SqlLocalDB.exe start MSSQLLocalDB
if %errorlevel% neq 0 (
    echo ERROR al iniciar LocalDB.
    exit /b 1
)

SqlLocalDB.exe info MSSQLLocalDB
echo.
echo Para conectarse: Server=(localdb)\MSSQLLocalDB
echo.
endlocal
