@echo off
REM run-tests.bat
REM Ejecuta unit + integration tests con cobertura.

setlocal
cd /d %~dp0..\..

echo ===================================
echo Ejecutando unit + integration tests
echo ===================================

dotnet test sgr.sln --nologo --logger "console;verbosity=normal"
set RC=%errorlevel%

if %RC% neq 0 (
    echo.
    echo *** Tests FALLARON con codigo %RC%
    exit /b %RC%
)

echo.
echo *** Tests PASARON
endlocal
