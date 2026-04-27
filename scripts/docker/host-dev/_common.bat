@echo off
REM _common.bat — Variables compartidas por los scripts del developer.
REM
REM Override-eable: definir DOCKERHUB_USER y/o SGR_TAG en el environment del shell
REM ANTES de invocar los scripts (set DOCKERHUB_USER=otro && build-all.bat).

if not defined DOCKERHUB_USER set DOCKERHUB_USER=fernandofilipuzzi
if not defined SGR_TAG set SGR_TAG=latest

REM Imagenes finales que produce el flujo build → publish.
set IMG_BACKEND=%DOCKERHUB_USER%/sgr-backend:%SGR_TAG%
set IMG_WEB=%DOCKERHUB_USER%/sgr-web:%SGR_TAG%
set IMG_DB=%DOCKERHUB_USER%/sgr-db:%SGR_TAG%

REM Ruta a la raiz del repo (este archivo vive en scripts/docker/host-dev/).
set REPO_ROOT=%~dp0..\..\..
