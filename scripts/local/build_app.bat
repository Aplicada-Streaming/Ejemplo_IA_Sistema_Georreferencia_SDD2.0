@echo off
setlocal enabledelayedexpansion
REM ============================================================
REM  SGR -- Build de la app movil (MAUI Android, Debug)
REM  Uso: scripts\local\build_app.bat
REM
REM  No instala en el dispositivo (eso lo hace run_app.bat).
REM ============================================================

set "PROJECT_ROOT=%~dp0..\.."
set "MOBILE_PROJECT=%PROJECT_ROOT%\src\Sgr.Frontend.Mobile\Sgr.Frontend.Mobile.csproj"
set "ANDROID_SDK=C:\Program Files (x86)\Android\android-sdk"

echo.
echo ============================================================
echo   SGR -- Build Sgr.Frontend.Mobile (net10.0-android, Debug)
echo ============================================================
echo.

echo [1/2] Limpiando artefactos previos (Debug, net10.0-android)...
dotnet clean "%MOBILE_PROJECT%" ^
    -f net10.0-android ^
    -c Debug ^
    --nologo --verbosity quiet
if %ERRORLEVEL% neq 0 (
    echo ERROR: Fallo el clean de Sgr.Frontend.Mobile
    goto :error
)
echo.

echo [2/2] Compilando Sgr.Frontend.Mobile...
dotnet build "%MOBILE_PROJECT%" ^
    -f net10.0-android ^
    -c Debug ^
    -p:AndroidSdkDirectory="%ANDROID_SDK%" ^
    --nologo
if %ERRORLEVEL% neq 0 goto :error

echo.
echo ============================================================
echo   Build de Sgr.Frontend.Mobile OK
echo ============================================================
echo.

goto :end
:error
echo.
echo ============================================================
echo   ERROR: Fallo el build de Sgr.Frontend.Mobile.
echo ============================================================
exit /b 1
:end
endlocal
exit /b 0
