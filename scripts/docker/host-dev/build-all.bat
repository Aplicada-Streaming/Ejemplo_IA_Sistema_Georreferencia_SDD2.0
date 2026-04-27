@echo off
REM build-all.bat — Construye / pull-eta las 3 imagenes en orden.

setlocal
echo ===========================================
echo Build all SGR images
echo ===========================================

call "%~dp0build-db.bat"      || exit /b 1
call "%~dp0build-backend.bat" || exit /b 1
call "%~dp0build-web.bat"     || exit /b 1

echo.
echo ===========================================
echo Todas las imagenes construidas OK.
echo Para publicar a Docker Hub: publish-all.bat
echo ===========================================
endlocal
