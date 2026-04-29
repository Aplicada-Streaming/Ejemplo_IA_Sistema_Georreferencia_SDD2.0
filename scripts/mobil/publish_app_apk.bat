@echo off
setlocal enabledelayedexpansion
REM ============================================================
REM  SGR -- Genera bundle APK de Sgr.Frontend.Mobile (Release)
REM
REM  Uso:
REM    scripts\local\publish_app_apk.bat
REM    scripts\local\publish_app_apk.bat http://192.168.0.10:5000
REM
REM  Si se pasa una URL como argumento, se patchea
REM  Resources/Raw/sgr-config.json (backendBaseUrl) antes de
REM  compilar, para que el APK apunte a esa URL del backend.
REM
REM  IMPORTANTE: EmbedAssembliesIntoApk=true es obligatorio para
REM  que el APK firmado pueda lanzarse via "adb install".
REM ============================================================

set "PROJECT_ROOT=%~dp0..\.."
set "MOBILE_PROJECT=%PROJECT_ROOT%\src\Sgr.Frontend.Mobile\Sgr.Frontend.Mobile.csproj"
set "CONFIG_JSON=%PROJECT_ROOT%\src\Sgr.Frontend.Mobile\Resources\Raw\sgr-config.json"
set "ANDROID_SDK=C:\Program Files (x86)\Android\android-sdk"
set "OUT_DIR=%PROJECT_ROOT%\out\apk"
set "OUT_APK=%PROJECT_ROOT%\out\sgr-mobile.apk"

echo.
echo ============================================================
echo   SGR -- Publish APK Sgr.Frontend.Mobile (Release)
echo ============================================================
echo.

REM ------------------------------------------------------------
REM  Patcheo opcional de backendBaseUrl en sgr-config.json
REM ------------------------------------------------------------
if not "%~1"=="" (
    echo [0/4] Patcheando %CONFIG_JSON%
    echo        backendBaseUrl = %~1
    powershell -NoProfile -Command "$p='%CONFIG_JSON%'; $j = Get-Content -Raw -Path $p | ConvertFrom-Json; $j.backendBaseUrl = '%~1'; ($j | ConvertTo-Json -Depth 5) | Set-Content -Path $p -Encoding UTF8"
    if !ERRORLEVEL! neq 0 (
        echo ERROR al patchear sgr-config.json
        goto :error
    )
    echo.
)

REM ------------------------------------------------------------
REM  Clean previo + build Release + publish APK
REM ------------------------------------------------------------
echo [1/4] Limpiando artefactos previos (Release, net10.0-android)...
dotnet clean "%MOBILE_PROJECT%" ^
    -f net10.0-android ^
    -c Release ^
    --nologo --verbosity quiet
if %ERRORLEVEL% neq 0 (
    echo ERROR: Fallo el clean de Sgr.Frontend.Mobile
    goto :error
)
if exist "%OUT_DIR%" rmdir /S /Q "%OUT_DIR%"
echo.

echo [2/4] dotnet publish (Release, android-arm64, EmbedAssembliesIntoApk=true)
echo        Tarda varios minutos la primera vez.
dotnet publish "%MOBILE_PROJECT%" ^
    -f net10.0-android ^
    -c Release ^
    -p:AndroidSdkDirectory="%ANDROID_SDK%" ^
    -p:RuntimeIdentifier=android-arm64 ^
    -p:AndroidPackageFormat=apk ^
    -p:EmbedAssembliesIntoApk=true ^
    -o "%OUT_DIR%"
if %ERRORLEVEL% neq 0 (
    echo ERROR durante dotnet publish.
    goto :error
)
echo.

REM ------------------------------------------------------------
REM  Localizar el APK firmado y copiarlo a out\sgr-mobile.apk
REM ------------------------------------------------------------
echo [3/4] Buscando APK firmado en %OUT_DIR% ...
set "FOUND_APK="
for %%F in ("%OUT_DIR%\*-Signed.apk") do set "FOUND_APK=%%F"
if not defined FOUND_APK (
    for %%F in ("%OUT_DIR%\*.apk") do set "FOUND_APK=%%F"
)
if not defined FOUND_APK (
    echo ERROR: no se encontro ningun APK en %OUT_DIR%.
    goto :error
)
echo        APK origen: !FOUND_APK!

echo.
echo [4/4] Copiando a %OUT_APK% ...
copy /Y "!FOUND_APK!" "%OUT_APK%" >nul
if %ERRORLEVEL% neq 0 (
    echo ERROR al copiar el APK final.
    goto :error
)

echo.
echo ============================================================
echo   APK generado:
echo     %OUT_APK%
echo   Para instalarlo:
echo     adb install -r "%OUT_APK%"
echo ============================================================
echo.

goto :end
:error
echo.
echo ============================================================
echo   ERROR: Fallo el publish del APK.
echo ============================================================
exit /b 1
:end
endlocal
exit /b 0
