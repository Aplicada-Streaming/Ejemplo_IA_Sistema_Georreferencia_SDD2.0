@echo off
REM publish-backend.bat — docker push del backend a Docker Hub.
REM Requiere: docker login (una sola vez por sesion).

setlocal
call "%~dp0_common.bat"

echo Pushing %IMG_BACKEND% ...
docker push %IMG_BACKEND%
if errorlevel 1 (
    echo *** Push FALLO. Confirmar que estas logueado: docker login
    exit /b 1
)

echo OK %IMG_BACKEND%
endlocal
