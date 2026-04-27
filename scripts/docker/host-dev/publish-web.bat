@echo off
REM publish-web.bat — docker push del frontend web a Docker Hub.

setlocal
call "%~dp0_common.bat"

echo Pushing %IMG_WEB% ...
docker push %IMG_WEB%
if errorlevel 1 (
    echo *** Push FALLO. Confirmar que estas logueado: docker login
    exit /b 1
)

echo OK %IMG_WEB%
endlocal
