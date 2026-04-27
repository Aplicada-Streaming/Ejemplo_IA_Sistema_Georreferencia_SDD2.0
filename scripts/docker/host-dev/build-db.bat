@echo off
REM build-db.bat — "Builda" la imagen de la base de datos.
REM En realidad solo hace pull de la imagen oficial de Microsoft y la retaggea
REM bajo nuestra cuenta de Docker Hub para que host-windows-prod la baje desde
REM la misma fuente que las otras dos. Mantener simetria con build-backend / build-web.

setlocal
call "%~dp0_common.bat"

set MSSQL_BASE=mcr.microsoft.com/mssql/server:2022-latest

echo ===========================================
echo Pulling base image: %MSSQL_BASE%
echo ===========================================
docker pull %MSSQL_BASE%
if errorlevel 1 (
    echo *** Pull FALLO.
    exit /b 1
)

echo Retagging as: %IMG_DB%
docker tag %MSSQL_BASE% %IMG_DB%
if errorlevel 1 (
    echo *** Tag FALLO.
    exit /b 1
)

echo OK %IMG_DB%
endlocal
