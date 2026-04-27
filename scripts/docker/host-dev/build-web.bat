@echo off
REM build-web.bat — Construye la imagen del frontend Blazor Server.

setlocal
call "%~dp0_common.bat"

echo ===========================================
echo Building image: %IMG_WEB%
echo Context: %REPO_ROOT%
echo ===========================================

docker build ^
    -t %IMG_WEB% ^
    -f "%REPO_ROOT%\src\Sgr.Frontend.Web\Dockerfile" ^
    "%REPO_ROOT%"
if errorlevel 1 (
    echo *** Build FALLO.
    exit /b 1
)

echo OK %IMG_WEB%
endlocal
