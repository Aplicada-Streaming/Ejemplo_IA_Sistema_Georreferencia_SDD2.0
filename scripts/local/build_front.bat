@echo off
setlocal enabledelayedexpansion

REM Matar procesos previos
echo Deteniendo procesos previos...
taskkill /F /IM Sgr.Frontend.Web.exe /T 2>nul
echo.

REM ============================================================
REM  SGR -- Build del frontend web (Blazor Server, Debug)
REM  Uso: scripts\local\build_front.bat
REM ============================================================

set "PROJECT_ROOT=%~dp0..\.."
set "WEB_PROJECT=%PROJECT_ROOT%\src\Sgr.Frontend.Web\Sgr.Frontend.Web.csproj"

echo.
echo ============================================================
echo   SGR -- Build Sgr.Frontend.Web (Debug)
echo ============================================================
echo.

echo [1/3] Limpiando artefactos previos (Debug)...
dotnet clean "%WEB_PROJECT%" -c Debug --nologo --verbosity quiet
if %ERRORLEVEL% neq 0 (
    echo ERROR: Fallo el clean de Sgr.Frontend.Web
    goto :error
)
echo.

echo [2/3] Restaurando dependencias...
dotnet restore "%WEB_PROJECT%" --verbosity quiet
if %ERRORLEVEL% neq 0 (
    echo ERROR: Fallo el restore de NuGet
    goto :error
)
echo.

echo [3/3] Compilando Sgr.Frontend.Web...
dotnet build "%WEB_PROJECT%" -c Debug --no-restore --nologo
if %ERRORLEVEL% neq 0 (
    echo ERROR: Fallo la compilacion de Sgr.Frontend.Web
    goto :error
)
echo.

echo ============================================================
echo   Build del frontend web OK
echo ============================================================
echo.

goto :end
:error
echo.
echo ============================================================
echo   ERROR: El build del frontend web fallo.
echo ============================================================
exit /b 1
:end
endlocal
exit /b 0
