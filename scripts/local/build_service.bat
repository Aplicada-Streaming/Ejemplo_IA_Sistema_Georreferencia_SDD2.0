@echo off
setlocal enabledelayedexpansion

REM Matar procesos previos
echo Deteniendo procesos previos...
taskkill /F /IM Sgr.Backend.Api.exe /T 2>nul
echo.

REM ============================================================
REM  SGR -- Build del servicio API (Debug)
REM  Uso: scripts\local\build_service.bat
REM ============================================================

set "PROJECT_ROOT=%~dp0..\.."
set "API_PROJECT=%PROJECT_ROOT%\src\Sgr.Backend.Api\Sgr.Backend.Api.csproj"

echo.
echo ============================================================
echo   SGR -- Build Sgr.Backend.Api (Debug)
echo ============================================================
echo.

echo [1/3] Limpiando artefactos previos (Debug)...
dotnet clean "%API_PROJECT%" -c Debug --nologo --verbosity quiet
if %ERRORLEVEL% neq 0 (
    echo ERROR: Fallo el clean de Sgr.Backend.Api
    goto :error
)
echo.

echo [2/3] Restaurando dependencias...
dotnet restore "%API_PROJECT%" --verbosity quiet
if %ERRORLEVEL% neq 0 (
    echo ERROR: Fallo el restore de NuGet
    goto :error
)
echo.

echo [3/3] Compilando Sgr.Backend.Api...
dotnet build "%API_PROJECT%" -c Debug --no-restore --nologo
if %ERRORLEVEL% neq 0 (
    echo ERROR: Fallo la compilacion de Sgr.Backend.Api
    goto :error
)
echo.

echo ============================================================
echo   Build del servicio OK
echo ============================================================
echo.

goto :end
:error
echo.
echo ============================================================
echo   ERROR: El build del servicio fallo.
echo ============================================================
exit /b 1
:end
endlocal
exit /b 0
