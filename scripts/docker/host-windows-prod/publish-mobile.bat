@echo off
REM publish-mobile.bat — Compila el APK firmado de la app movil con la URL del
REM backend del host destino y lo deja en out\sgr-mobile.apk para distribucion.
REM
REM Pre-requisitos en este host:
REM   - Codigo fuente del repo SGR (este script vive dentro de el)
REM   - .NET 10 SDK (10.0.202 segun global.json)
REM   - Workload maui-android (ver scripts/dev-local/install-workloads.bat)
REM   - Android SDK (instalado por VS o standalone) con build-tools y platforms
REM
REM Asume que configure.bat ya patcheó Resources/Raw/sgr-config.json con la
REM URL correcta. Si querés sobreescribir aca, exporta BACKEND_URL antes.

setlocal EnableDelayedExpansion

REM Verificamos .env porque queremos confirmar la URL final que se va a embebir.
if not exist "%~dp0.env" (
    echo *** No existe .env. Ejecuta primero configure.bat
    exit /b 1
)

set REPO_ROOT=%~dp0..\..\..
set MOBILE_PROJ=%REPO_ROOT%\src\Sgr.Frontend.Mobile\Sgr.Frontend.Mobile.csproj
set MOBILE_CONFIG=%REPO_ROOT%\src\Sgr.Frontend.Mobile\Resources\Raw\sgr-config.json
set OUT_DIR=%~dp0out
set FRAMEWORK=net10.0-android

if not exist "%MOBILE_PROJ%" (
    echo *** No encuentro %MOBILE_PROJ%
    echo Este script tiene que correr desde dentro del repo SGR.
    exit /b 1
)

REM Si BACKEND_URL no fue exportado, lo derivamos del .env: HOST_IP -> http://HOST_IP:5000
if "%BACKEND_URL%"=="" (
    for /f "usebackq tokens=1,2 delims==" %%A in ("%~dp0.env") do (
        if /I "%%A"=="HOST_IP" set BACKEND_URL=http://%%B:5000
    )
)

if "%BACKEND_URL%"=="" (
    echo *** No se pudo determinar BACKEND_URL. Configurar HOST_IP en .env.
    exit /b 1
)

echo ===========================================
echo Publish APK movil
echo Backend URL embebida: %BACKEND_URL%
echo ===========================================

REM Re-escribimos sgr-config.json a la URL deseada — idempotente con configure.bat
REM pero por si acaso queremos llamarlo standalone.
echo Patcheando %MOBILE_CONFIG% ...
>  "%MOBILE_CONFIG%" echo {
>> "%MOBILE_CONFIG%" echo   "_comment": "Generado por publish-mobile.bat — backend URL del host destino.",
>> "%MOBILE_CONFIG%" echo   "backendBaseUrl": "%BACKEND_URL%"
>> "%MOBILE_CONFIG%" echo }

REM Build + sign del APK Release.
REM EmbedAssembliesIntoApk=true es necesario en MAUI Android para que adb install
REM no crashee al arranque (ver memory: maui-android-deploy).
echo.
echo Compilando APK signed (Release, %FRAMEWORK%) ...
dotnet publish "%MOBILE_PROJ%" ^
    -f %FRAMEWORK% ^
    -c Release ^
    -p:EmbedAssembliesIntoApk=true ^
    -p:AndroidPackageFormat=apk
if errorlevel 1 (
    echo *** Build fallo.
    exit /b 1
)

REM Localizar el APK firmado en bin\Release\.
set APK_DIR=%REPO_ROOT%\src\Sgr.Frontend.Mobile\bin\Release\%FRAMEWORK%
set APK_FILE=
for %%F in ("%APK_DIR%\publish\*-Signed.apk") do set APK_FILE=%%F
if "%APK_FILE%"=="" (
    REM Algunas configs no usan publish/ — buscamos directo.
    for %%F in ("%APK_DIR%\*-Signed.apk") do set APK_FILE=%%F
)

if "%APK_FILE%"=="" (
    echo *** No encontre el APK firmado en %APK_DIR%
    echo Listado de %APK_DIR%:
    dir /B "%APK_DIR%"
    exit /b 1
)

if not exist "%OUT_DIR%" mkdir "%OUT_DIR%"
copy /Y "%APK_FILE%" "%OUT_DIR%\sgr-mobile.apk" >nul

echo.
echo ===========================================
echo OK — APK listo en: %OUT_DIR%\sgr-mobile.apk
echo Backend URL: %BACKEND_URL%
echo Para instalar en un dispositivo: adb install -r "%OUT_DIR%\sgr-mobile.apk"
echo ===========================================
endlocal
