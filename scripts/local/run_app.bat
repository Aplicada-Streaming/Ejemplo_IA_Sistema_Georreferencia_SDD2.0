@echo off
setlocal enabledelayedexpansion

REM ============================================================
REM  SGR -- Build + install + launch de Sgr.Frontend.Mobile
REM  en un dispositivo Android conectado por USB.
REM
REM  Asume:
REM    - El backend API corre en localhost:5000 en este PC
REM      (ver invoke_run_service.bat).
REM    - El dispositivo esta conectado por USB con depuracion
REM      habilitada y autorizado.
REM
REM  Pasos:
REM    1) Verifica ADB y dispositivo
REM    2) ADB reverse tcp:5000 -> el celular alcanza el backend
REM       del PC via USB
REM    3) Clean de artefactos previos
REM    4) Build + install del APK en el dispositivo
REM    5) Lanza la app
REM ============================================================

set "PROJECT_ROOT=%~dp0..\.."
set "MOBILE_PROJECT=%PROJECT_ROOT%\src\Sgr.Frontend.Mobile\Sgr.Frontend.Mobile.csproj"
set "DEVICE_SERIAL=ZY32GSJ88S"
set "APP_ID=com.companyname.sgr.frontend.mobile"
set "ADB=C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe"
set "ANDROID_SDK=C:\Program Files (x86)\Android\android-sdk"

echo.
echo ============================================================
echo   SGR Mobile - Deploy a %DEVICE_SERIAL%
echo ============================================================
echo.

echo [1/5] Verificando ADB y dispositivo...
"%ADB%" devices
"%ADB%" -s %DEVICE_SERIAL% get-state >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: dispositivo %DEVICE_SERIAL% no detectado por ADB.
    echo Verifica USB + modo desarrollador + autorizacion del PC.
    goto :error
)

echo.
echo [2/5] ADB reverse tcp:5000 -^> PC localhost:5000
"%ADB%" -s %DEVICE_SERIAL% reverse tcp:5000 tcp:5000
if %ERRORLEVEL% neq 0 (
    echo ERROR al setear adb reverse.
    goto :error
)
"%ADB%" -s %DEVICE_SERIAL% reverse --list

echo.
echo [3/5] Limpiando artefactos previos (Debug, net10.0-android)...
dotnet clean "%MOBILE_PROJECT%" ^
    -f net10.0-android ^
    -c Debug ^
    --nologo --verbosity quiet
if %ERRORLEVEL% neq 0 (
    echo ERROR: Fallo el clean de Sgr.Frontend.Mobile
    goto :error
)

echo.
echo [4/5] Build + install (Debug, android-arm64). Tarda varios minutos la primera vez.
dotnet build "%MOBILE_PROJECT%" ^
    -f net10.0-android ^
    -c Debug ^
    -p:AndroidSdkDirectory="%ANDROID_SDK%" ^
    -p:EmbedAssembliesIntoApk=true ^
    -t:Install ^
    -p:AdbTarget="-s %DEVICE_SERIAL%"
if %ERRORLEVEL% neq 0 (
    echo ERROR durante build/install.
    goto :error
)

echo.
echo [5/5] Lanzando la app en el dispositivo...
"%ADB%" -s %DEVICE_SERIAL% shell monkey -p %APP_ID% -c android.intent.category.LAUNCHER 1

echo.
echo ============================================================
echo   OK. Backend conectado via USB en localhost:5000.
echo   Logs: "%ADB%" -s %DEVICE_SERIAL% logcat -s "DOTNET" "AndroidRuntime"
echo ============================================================
echo.

goto :end
:error
echo.
echo ============================================================
echo   ERROR: Fallo el deploy de Sgr.Frontend.Mobile.
echo ============================================================
exit /b 1
:end
endlocal
exit /b 0
