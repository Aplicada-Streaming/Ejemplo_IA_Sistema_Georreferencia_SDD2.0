@echo off
setlocal
call "%~dp0_common.bat" || exit /b 1
echo Pulling %DOCKERHUB_USER%/sgr-backend:latest ...
docker pull %DOCKERHUB_USER%/sgr-backend:latest
endlocal
