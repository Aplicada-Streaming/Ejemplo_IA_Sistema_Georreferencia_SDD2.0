@echo off
setlocal
call "%~dp0_common.bat" || exit /b 1
echo Pulling %DOCKERHUB_USER%/sgr-db:latest ...
docker pull %DOCKERHUB_USER%/sgr-db:latest
endlocal
