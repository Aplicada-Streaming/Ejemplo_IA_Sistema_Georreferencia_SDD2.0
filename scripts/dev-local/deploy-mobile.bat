@echo off
REM deploy-mobile.bat
REM Orquesta el deploy de Sgr.Frontend.Mobile a un dispositivo Android conectado por USB.
REM Asume:
REM   - El backend está corriendo en localhost:5000 en este PC (ver start-backend.bat).
REM   - El dispositivo está conectado por USB con depuración habilitada y autorizado.
REM
REM Pasos:
REM   1) ADB reverse tcp:5000 → el celular alcanza localhost:5000 vía USB
REM   2) Build + install del APK en el dispositivo
REM   3) Lanza la app

setlocal
cd /d %~dp0..\..

set DEVICE_SERIAL=ZY32GSJ88S
set APP_ID=com.companyname.sgr.frontend.mobile
set ADB="C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe"

echo ===================================================
echo SGR Mobile - Deploy a %DEVICE_SERIAL%
echo ===================================================

echo [1/4] Verificando ADB y dispositivo...
%ADB% devices
%ADB% -s %DEVICE_SERIAL% get-state >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: dispositivo %DEVICE_SERIAL% no detectado por ADB.
    echo Verifica USB + modo desarrollador + autorizacion del PC.
    exit /b 1
)

echo.
echo [2/4] ADB reverse tcp:5000 -^> PC localhost:5000
%ADB% -s %DEVICE_SERIAL% reverse tcp:5000 tcp:5000
if %errorlevel% neq 0 (
    echo ERROR al setear adb reverse.
    exit /b 1
)
%ADB% -s %DEVICE_SERIAL% reverse --list

echo.
echo [3/4] Build + install (Debug, android-arm64). Tarda varios minutos la primera vez.
dotnet build src/Sgr.Frontend.Mobile/Sgr.Frontend.Mobile.csproj ^
    -f net8.0-android ^
    -c Debug ^
    -p:AndroidSdkDirectory="C:\Program Files (x86)\Android\android-sdk" ^
    -t:Install ^
    -p:AdbTarget="-s %DEVICE_SERIAL%"
if %errorlevel% neq 0 (
    echo ERROR durante build/install.
    exit /b 1
)

echo.
echo [4/4] Lanzando la app en el dispositivo...
%ADB% -s %DEVICE_SERIAL% shell monkey -p %APP_ID% -c android.intent.category.LAUNCHER 1

echo.
echo OK. Backend conectado vía USB en localhost:5000.
echo Para ver logs: %ADB% -s %DEVICE_SERIAL% logcat -s "DOTNET" "AndroidRuntime"
endlocal
