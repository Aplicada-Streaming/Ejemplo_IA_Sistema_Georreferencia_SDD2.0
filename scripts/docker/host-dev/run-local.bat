@echo off
REM run-local.bat - Levanta el stack SGR sobre el mismo Docker Desktop donde se
REM construyeron las imagenes con build-all.bat. NO requiere push a Docker Hub.
REM
REM Reusa scripts/docker/host-windows-prod/docker-compose.yml como definicion del
REM stack, pero con un .env.local propio en host-dev/. El .env.local queda
REM ignorado por git via la regla .env.local del .gitignore.

setlocal
cd /d %~dp0

set COMPOSE=%~dp0..\host-windows-prod\docker-compose.yml
set ENVFILE=%~dp0.env.local

REM 1) Daemon arriba?
docker version >nul 2>&1
if errorlevel 1 (
    echo *** Docker no responde. Iniciar Docker Desktop antes de continuar.
    exit /b 1
)

REM 2) Imagenes presentes localmente? Si falta alguna, dirigir al build.
call :check_image fernandofilipuzzi/sgr-db:latest
if errorlevel 1 exit /b 1
call :check_image fernandofilipuzzi/sgr-backend:latest
if errorlevel 1 exit /b 1
call :check_image fernandofilipuzzi/sgr-web:latest
if errorlevel 1 exit /b 1

REM 3) Generar .env.local si no existe, con defaults para correr en este host.
if not exist "%ENVFILE%" call :generate_env

REM 4) compose up con la imagen local. pull_policy default = "missing": como
REM las imagenes ya estan en el daemon local, compose no las baja de Docker Hub.
echo ===========================================
echo Levantando stack SGR en modo local
echo ===========================================
docker compose -f "%COMPOSE%" --env-file "%ENVFILE%" up -d
if errorlevel 1 (
    echo *** Compose up FALLO.
    exit /b 1
)

echo.
docker compose -f "%COMPOSE%" --env-file "%ENVFILE%" ps

echo.
echo URLs:
echo   Web:          http://localhost:5100
echo   Backend API:  http://localhost:5000
echo   SQL Server:   localhost,1433
echo.
echo Logs en vivo: logs-local.bat
echo Detener:      stop-local.bat
endlocal
goto :eof


:check_image
docker image inspect %1 >nul 2>&1
if errorlevel 1 (
    echo *** Falta imagen local: %1
    echo Ejecuta primero: build-all.bat
    exit /b 1
)
exit /b 0


:generate_env
echo Generando %ENVFILE% con defaults para localhost ...
if not exist "C:\sgr\storage" mkdir "C:\sgr\storage" 2>nul

>  "%ENVFILE%" echo # Generado por run-local.bat. NO commitear, cubierto por .env.local en .gitignore.
>> "%ENVFILE%" echo DOCKERHUB_USER=fernandofilipuzzi
>> "%ENVFILE%" echo HOST_IP=localhost
>> "%ENVFILE%" echo HOST_PHOTOS_PATH=C:\sgr\storage
>> "%ENVFILE%" echo SQL_SA_PASSWORD=Sgr_DB_StrongP@ss1
>> "%ENVFILE%" echo JWT_SIGNING_KEY=local-dev-only-jwt-signing-key-min-32-chars-padding-ok
>> "%ENVFILE%" echo SEED_ADMIN_PASSWORD=Admin1234!
>> "%ENVFILE%" echo SEED_JEFE_PASSWORD=Jefe1234!
>> "%ENVFILE%" echo SEED_RELEVADOR_PASSWORD=Relev1234!

echo OK %ENVFILE%
exit /b 0
