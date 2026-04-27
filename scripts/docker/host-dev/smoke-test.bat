@echo off
REM smoke-test.bat — Build local + levantar stack para validacion manual.
REM
REM Lo que hace en orden:
REM   [1/4] Pre-flight (docker disponible) y .env minimo en host-windows-prod si no existe.
REM   [2/4] docker compose config — valida sintaxis del compose.
REM   [3/4] build-all.bat — buildea/pull-ea las 3 imagenes localmente (sin push).
REM   [4/4] docker compose up + espera al healthcheck de sgr-db (60-90s primera vez).
REM
REM Al final imprime URLs + credenciales para que valides a mano:
REM     - http://localhost:5100   (web)
REM     - http://localhost:5000/health   (backend)
REM     - http://localhost:5000/swagger
REM
REM Si todo arranca limpio el armado Docker es correcto y podes publicar a Docker Hub.
REM El stack queda arriba — para bajarlo: call host-windows-prod\stop-all.bat

setlocal EnableDelayedExpansion

REM ───────────── Pre-flight ─────────────
docker version >nul 2>&1
if errorlevel 1 (
    echo *** Docker no responde. Levanta Docker Desktop antes de continuar.
    exit /b 1
)

set HOSTPROD=%~dp0..\host-windows-prod
set ENV=%HOSTPROD%\.env
set COMPOSE=%HOSTPROD%\docker-compose.yml

if not exist "%COMPOSE%" (
    echo *** No encuentro %COMPOSE%
    exit /b 1
)

REM ───── Crear .env minimo si no existe ─────
REM Si el dev ya tiene un .env real (por configure.bat), lo respetamos.
if not exist "%ENV%" (
    echo Creando .env minimo para smoke test ...
    set "SMOKEDIR=%TEMP%\sgr-smoke-photos"
    if not exist "!SMOKEDIR!" mkdir "!SMOKEDIR!"

    (
        echo # Generado por smoke-test.bat el %date% %time%
        echo # Borra este archivo si queres regenerarlo, o sobreescribilo con configure.bat
        echo DOCKERHUB_USER=fernandofilipuzzi
        echo HOST_IP=localhost
        echo HOST_PHOTOS_PATH=!SMOKEDIR!
        echo SQL_SA_PASSWORD=Sgr_DB_StrongP@ss1
        echo JWT_SIGNING_KEY=smoke-test-jwt-key-32-chars-min-ok-pad
        echo SEED_ADMIN_PASSWORD=Admin1234!
        echo SEED_JEFE_PASSWORD=Jefe1234!
        echo SEED_RELEVADOR_PASSWORD=Relev1234!
    ) > "%ENV%"
    echo .env creado en %ENV%
    echo Fotos del smoke seran persistidas en !SMOKEDIR!
) else (
    echo Reutilizando .env existente en %ENV%.
)

REM ───────────── [1/4] compose config ─────────────
echo.
echo [1/4] Validando docker compose config ...
docker compose -f "%COMPOSE%" --env-file "%ENV%" config >nul
if errorlevel 1 (
    echo *** docker compose config FALLO. Revisar docker-compose.yml o .env.
    exit /b 1
)
echo OK

REM ───────────── [2/4] Build local ─────────────
echo.
echo [2/4] Build local de las 3 imagenes (sin push) ...
call "%~dp0build-all.bat"
if errorlevel 1 (
    echo *** build-all FALLO.
    exit /b 1
)

REM ───────────── [3/4] Levantar stack ─────────────
echo.
echo [3/4] docker compose up -d ...
docker compose -f "%COMPOSE%" --env-file "%ENV%" up -d
if errorlevel 1 (
    echo *** Compose up FALLO.
    exit /b 1
)

REM ───────────── [4/4] Esperar healthcheck DB ─────────────
echo.
echo [4/4] Esperando healthcheck de sgr-db (60-90s la primera vez) ...
set /a RETRIES=24
:wait_db
docker inspect --format "{{.State.Health.Status}}" sgr-db 2>nul | findstr "healthy" >nul
if !errorlevel! equ 0 goto db_ok
set /a RETRIES-=1
if !RETRIES! leq 0 (
    echo *** sgr-db no llego a healthy en ~120s. Logs:
    docker compose -f "%COMPOSE%" --env-file "%ENV%" logs sgr-db --tail=50
    echo.
    echo Stack queda arriba para inspeccion manual.
    echo Bajar con: call "%HOSTPROD%\stop-all.bat"
    exit /b 1
)
timeout /t 5 /nobreak >nul
goto wait_db

:db_ok
echo sgr-db healthy.

REM ───────────── Resumen ─────────────
echo.
echo ===========================================
echo SMOKE TEST OK — stack arriba para validacion manual
echo ===========================================
docker compose -f "%COMPOSE%" --env-file "%ENV%" ps
echo.
echo Validar a mano:
echo   1. Browser -^> http://localhost:5100
echo   2. Login admin@vialidad.local / Admin1234!
echo   3. Crear un relevamiento -^> ver listado
echo   4. Backend health: http://localhost:5000/health  (debe responder 200)
echo   5. Swagger: http://localhost:5000/swagger
echo.
echo Credenciales seed:
echo   Admin:     admin@vialidad.local / Admin1234!
echo   Jefe:      jefe@vialidad.local / Jefe1234!
echo   Relevador: relevador@vialidad.local / Relev1234!
echo.
echo Logs en tiempo real:
echo   docker compose -f "%COMPOSE%" logs -f
echo.
echo Bajar el stack:
echo   call "%HOSTPROD%\stop-all.bat"
echo ===========================================
endlocal
