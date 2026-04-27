# scripts/dev-local/

Scripts para correr el SGR en la máquina del developer **sin Docker**, usando los
runtimes nativos: SQL Server LocalDB + dotnet run.

## Pre-requisitos

- Windows 10/11
- .NET SDK 10 (versión exacta en `global.json`)
- SQL Server Express LocalDB
- (Opcional, para mobile) workload `maui-android` + Android SDK
  → ejecutar `install-workloads.bat` como administrador

## Scripts

| Script | Propósito |
|---|---|
| `start-all.bat` | Levanta DB + Backend + Web cada uno en su ventana |
| `start-db.bat` | Sólo SQL Server LocalDB (`MSSQLLocalDB`) |
| `start-backend.bat` | Sólo backend en `http://localhost:5000` |
| `start-web.bat` | Sólo web en `http://localhost:5100` |
| `run-tests.bat` | `dotnet test sgr.sln` con verbosity normal |
| `install-workloads.bat` | Instala `maui-android` (correr como admin) |
| `deploy-mobile.bat` | Build + install + launch del APK en Android via USB |

## Quickstart

```cmd
cd scripts\dev-local
start-all.bat
```

Si querés sólo backend + web (DB ya corriendo):
```cmd
start-backend.bat   :: en una ventana
start-web.bat       :: en otra
```

Para mobile en USB (`adb reverse` permite que el dispositivo alcance localhost del PC):
```cmd
deploy-mobile.bat
```
