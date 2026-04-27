@echo off
REM publish-db.bat — docker push del tag DB a Docker Hub.
REM
REM Como la imagen real es de Microsoft (mssql/server:2022-latest), publicarla bajo
REM nuestro user es solo "linkear" el tag — Docker Hub solo permite push si tu cuenta
REM acepta layers ya conocidas. Si tu plan free no soporta el tamaño / layers (mssql
REM ronda 1.5 GB), considerar dejar el tag sin publish y que host-windows-prod tire
REM directo de mcr.microsoft.com/mssql/server:2022-latest (ver nota en README).

setlocal
call "%~dp0_common.bat"

echo Pushing %IMG_DB% ...
docker push %IMG_DB%
if errorlevel 1 (
    echo *** Push FALLO. Si el error es por tamano, ver el README para alternativa.
    exit /b 1
)

echo OK %IMG_DB%
endlocal
