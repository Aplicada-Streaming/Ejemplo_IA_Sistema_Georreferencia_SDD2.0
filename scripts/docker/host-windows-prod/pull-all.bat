@echo off
setlocal
echo ===========================================
echo Pulling todas las imagenes SGR
echo ===========================================
call "%~dp0pull-db.bat"      || exit /b 1
call "%~dp0pull-backend.bat" || exit /b 1
call "%~dp0pull-web.bat"     || exit /b 1
echo.
echo OK — todas las imagenes presentes localmente.
endlocal
