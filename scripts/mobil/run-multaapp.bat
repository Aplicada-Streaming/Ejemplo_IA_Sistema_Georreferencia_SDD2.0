@echo off
echo Lanzando MotorDsl.MultaApp en dispositivo Android...

cd  ..

set PROJECT_PATH=samples\MotorDsl.MultaApp\MotorDsl.MultaApp.csproj
set OUTPUT_PATH=output

set PATH=%PATH%;C:\Program Files (x86)\Android\android-sdk\platform-tools
adb devices

cd %PROJECT_PATH%
dotnet build -t:Run -f net10.0-android

cd ..\..
pause
