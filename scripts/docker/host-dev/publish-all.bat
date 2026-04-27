@echo off
REM publish-all.bat — Push de las 3 imagenes a Docker Hub.

setlocal
echo ===========================================
echo Publish all SGR images
echo ===========================================

call "%~dp0publish-db.bat"      || exit /b 1
call "%~dp0publish-backend.bat" || exit /b 1
call "%~dp0publish-web.bat"     || exit /b 1

echo.
echo ===========================================
echo Todas las imagenes publicadas en Docker Hub.
echo ===========================================
endlocal
