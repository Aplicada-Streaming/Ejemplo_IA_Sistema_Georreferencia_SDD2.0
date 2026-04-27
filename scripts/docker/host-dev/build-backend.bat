@echo off
REM build-backend.bat — Construye la imagen del backend API.
REM Contexto del build = raiz del repo (necesario para acceder a src/).

setlocal
call "%~dp0_common.bat"

echo ===========================================
echo Building image: %IMG_BACKEND%
echo Context: %REPO_ROOT%
echo ===========================================

docker build ^
    -t %IMG_BACKEND% ^
    -f "%REPO_ROOT%\src\Sgr.Backend.Api\Dockerfile" ^
    "%REPO_ROOT%"
if errorlevel 1 (
    echo *** Build FALLO.
    exit /b 1
)

echo OK %IMG_BACKEND%
endlocal
