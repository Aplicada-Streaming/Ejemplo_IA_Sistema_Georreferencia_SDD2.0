@echo off
REM configure.bat — Wizard interactivo que prepara el host destino.
REM
REM Resuelve dos cosas:
REM   1. Genera el .env que toma docker-compose con la config de runtime
REM      (IP, passwords, paths). El compose despues lo carga automaticamente.
REM   2. Patchea el asset de la app movil (Resources/Raw/sgr-config.json) con
REM      la URL del backend en este host. Asi cuando publish-mobile.bat compile
REM      el APK, el dispositivo apunta a este host.
REM
REM Pre-requisitos del host destino:
REM   - Docker Desktop instalado y corriendo
REM   - Para publish-mobile.bat: .NET 10 SDK + workload maui-android + Android SDK
REM     (ver scripts/dev-local/install-workloads.bat)

setlocal EnableDelayedExpansion
cd /d %~dp0

set TEMPLATE=%~dp0.env.template
set TARGET=%~dp0.env

if not exist "%TEMPLATE%" (
    echo *** No existe .env.template en %~dp0
    exit /b 1
)

echo ===========================================
echo SGR — Wizard de configuracion del host
echo ===========================================
echo.
echo Vas a configurar 6 valores. Enter para usar el sugerido entre [].
echo.

REM 1) Docker Hub user (defaultea al del template).
set DEFAULT_DOCKERHUB_USER=fernandofilipuzzi
set /p DOCKERHUB_USER=Docker Hub user de las imagenes [%DEFAULT_DOCKERHUB_USER%]:
if "!DOCKERHUB_USER!"=="" set DOCKERHUB_USER=%DEFAULT_DOCKERHUB_USER%

REM 2) IP del host destino.
echo.
echo La IP del host es la que ven los clientes (web y movil) para llegar al backend.
echo Para acceso solo desde esta misma maquina: localhost
echo Para acceso desde la LAN: la IP local (ej. 192.168.1.50)
set /p HOST_IP=IP/hostname del host [localhost]:
if "!HOST_IP!"=="" set HOST_IP=localhost

REM 3) Carpeta para fotos.
echo.
echo La carpeta donde el backend guarda las fotos. Sera bind-mounteada al container.
echo Sera creada si no existe.
set /p HOST_PHOTOS_PATH=Carpeta de fotos [C:\sgr\storage]:
if "!HOST_PHOTOS_PATH!"=="" set HOST_PHOTOS_PATH=C:\sgr\storage
if not exist "!HOST_PHOTOS_PATH!" (
    echo Creando !HOST_PHOTOS_PATH! ...
    mkdir "!HOST_PHOTOS_PATH!" 2>nul
)

REM 4) SQL Server SA password.
echo.
echo Password de SA para SQL Server. Requisitos del engine:
echo   8+ caracteres, mayus + minus + numero + simbolo.
set /p SQL_SA_PASSWORD=SQL_SA_PASSWORD [Sgr_DB_StrongP@ss1]:
if "!SQL_SA_PASSWORD!"=="" set SQL_SA_PASSWORD=Sgr_DB_StrongP@ss1

REM 5) JWT signing key.
echo.
echo Clave de firma JWT (minimo 32 caracteres).
set /p JWT_SIGNING_KEY=JWT_SIGNING_KEY [auto-generar]:
if "!JWT_SIGNING_KEY!"=="" (
    REM Random pseudo-aleatorio basado en %RANDOM% repetido. No es crypto-grade
    REM pero suficiente para dev/MVP. Producir uno fuera y pegar acá si quieren mejor.
    set JWT_SIGNING_KEY=k_!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!!RANDOM!_sgr
)

REM 6) Seed passwords (opcionales).
echo.
echo Passwords iniciales del seed (admin / jefe / relevador). Cambiables despues por UI.
set /p SEED_ADMIN_PASSWORD=Admin password [Admin1234!]:
if "!SEED_ADMIN_PASSWORD!"=="" set SEED_ADMIN_PASSWORD=Admin1234!
set /p SEED_JEFE_PASSWORD=Jefe password [Jefe1234!]:
if "!SEED_JEFE_PASSWORD!"=="" set SEED_JEFE_PASSWORD=Jefe1234!
set /p SEED_RELEVADOR_PASSWORD=Relevador password [Relev1234!]:
if "!SEED_RELEVADOR_PASSWORD!"=="" set SEED_RELEVADOR_PASSWORD=Relev1234!

REM ───── Escribir .env ─────
echo.
echo Escribiendo .env ...
(
    echo # Generado por configure.bat el %date% %time%
    echo DOCKERHUB_USER=!DOCKERHUB_USER!
    echo HOST_IP=!HOST_IP!
    echo HOST_PHOTOS_PATH=!HOST_PHOTOS_PATH!
    echo SQL_SA_PASSWORD=!SQL_SA_PASSWORD!
    echo JWT_SIGNING_KEY=!JWT_SIGNING_KEY!
    echo SEED_ADMIN_PASSWORD=!SEED_ADMIN_PASSWORD!
    echo SEED_JEFE_PASSWORD=!SEED_JEFE_PASSWORD!
    echo SEED_RELEVADOR_PASSWORD=!SEED_RELEVADOR_PASSWORD!
) > "%TARGET%"

echo OK %TARGET%

REM ───── Patchear el asset de la app movil ─────
REM Buscamos el repo del proyecto en el path del host destino. Si el script vive en
REM <repo>/scripts/docker/host-windows-prod/, la raiz es ../../..
set REPO_ROOT=%~dp0..\..\..
set MOBILE_CONFIG=%REPO_ROOT%\src\Sgr.Frontend.Mobile\Resources\Raw\sgr-config.json

set BACKEND_URL=http://!HOST_IP!:5000

if exist "%MOBILE_CONFIG%" (
    echo.
    echo Patcheando %MOBILE_CONFIG%
    echo   backendBaseUrl = !BACKEND_URL!
    >  "%MOBILE_CONFIG%" echo {
    >> "%MOBILE_CONFIG%" echo   "_comment": "Generado por configure.bat — backend URL del host destino.",
    >> "%MOBILE_CONFIG%" echo   "backendBaseUrl": "!BACKEND_URL!"
    >> "%MOBILE_CONFIG%" echo }
    echo OK config movil
) else (
    echo.
    echo NOTA: %MOBILE_CONFIG% no existe en este host.
    echo Si vas a compilar el APK en otra maquina, copia este .env y corre publish-mobile.bat alla.
)

echo.
echo ===========================================
echo Configuracion completa.
echo Siguientes pasos:
echo   1. pull-all.bat    (baja imagenes desde Docker Hub)
echo   2. run-all.bat     (levanta el stack)
echo   3. publish-mobile.bat   (opcional — compila APK con la URL configurada)
echo ===========================================
endlocal
