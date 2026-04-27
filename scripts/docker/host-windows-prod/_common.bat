@echo off
REM _common.bat — Variables compartidas en host destino.

REM Cambiar a la carpeta del compose para que los comandos lo levanten siempre.
cd /d %~dp0

REM Verifica que docker este disponible.
docker version >nul 2>&1
if errorlevel 1 (
    echo *** Docker no responde. Levanta Docker Desktop antes de continuar.
    exit /b 1
)

REM Verifica que existe el .env.
if not exist "%~dp0.env" (
    echo *** No existe .env en %~dp0
    echo Ejecuta primero: configure.bat
    exit /b 1
)

REM Lee DOCKERHUB_USER del .env para que pull-* sepa que tag pedir.
for /f "usebackq tokens=1,2 delims==" %%A in ("%~dp0.env") do (
    if /I "%%A"=="DOCKERHUB_USER" set DOCKERHUB_USER=%%B
)

if not defined DOCKERHUB_USER (
    echo *** DOCKERHUB_USER no encontrado en .env
    exit /b 1
)
