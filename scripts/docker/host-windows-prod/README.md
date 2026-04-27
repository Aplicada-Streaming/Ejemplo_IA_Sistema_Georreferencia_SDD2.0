# scripts/docker/host-windows-prod/

Scripts que corre el **operador del cliente** en la máquina destino para instalar
y operar el SGR. Asume que el dev ya publicó las imágenes con `host-dev/`.

## Pre-requisitos en el host

| Componente | ¿Para qué? |
|---|---|
| Windows 10/11 | OS soportado por Docker Desktop |
| Docker Desktop | Levantar containers |
| Conexión a internet (1 vez) | Bajar imágenes de Docker Hub |
| Carpeta vacía para fotos | Bind mount del storage local del backend |
| **(opcional)** .NET SDK 10 + workload `maui-android` + Android SDK | Sólo si vas a compilar el APK móvil acá con `publish-mobile.bat` |

## Quickstart (5 pasos)

```cmd
cd scripts\docker\host-windows-prod

:: 1) Configurar el host (genera .env y patchea config móvil)
configure.bat

:: 2) Bajar las imágenes desde Docker Hub
pull-all.bat

:: 3) Levantar el stack
run-all.bat

:: 4) Verificar
:: Browser → http://<HOST_IP>:5100
:: Login: admin@vialidad.local / Admin1234! (o el password que pusiste en configure)

:: 5) (opcional) Compilar APK con la URL del host configurada
publish-mobile.bat
:: APK queda en: out\sgr-mobile.apk
```

## Scripts

### Configuración inicial (correr una sola vez por host)

| Script | Acción |
|---|---|
| `configure.bat` | Wizard interactivo. Pregunta IP, paths, passwords. Genera `.env` y patchea `Resources/Raw/sgr-config.json` de la móvil. |

### Pull de imágenes

| Script | Acción |
|---|---|
| `pull-backend.bat` | `docker pull` del backend |
| `pull-web.bat` | `docker pull` del web |
| `pull-db.bat` | `docker pull` del db |
| `pull-all.bat` | invoca los 3 |

### Run

| Script | Acción |
|---|---|
| `run-db.bat` | `docker compose up -d sgr-db` |
| `run-backend.bat` | `docker compose up -d sgr-backend` (depende de db) |
| `run-web.bat` | `docker compose up -d sgr-web` |
| `run-all.bat` | levanta los 3 + muestra `ps` y URLs |
| `stop-all.bat` | `docker compose down` (preserva volume `sgr-db-data`) |
| `logs-all.bat` | sigue los logs de los 3 servicios en tiempo real |

### APK móvil

| Script | Acción |
|---|---|
| `publish-mobile.bat` | Patchea `sgr-config.json` con la URL del backend del host, hace `dotnet publish` Release del proyecto móvil, deja APK firmado en `out\sgr-mobile.apk` |

## Variables de entorno (`.env`)

Generado por `configure.bat`. NO se commitea. Plantilla en `.env.template`.

| Variable | Para qué |
|---|---|
| `DOCKERHUB_USER` | Cuenta de Docker Hub donde están las imágenes |
| `HOST_IP` | IP/hostname accesible para los clientes web y móvil |
| `HOST_PHOTOS_PATH` | Carpeta Windows donde el backend escribe las fotos |
| `SQL_SA_PASSWORD` | Password de SA para SQL Server |
| `JWT_SIGNING_KEY` | Clave de firma JWT (≥32 chars) |
| `SEED_*_PASSWORD` | Passwords iniciales del seed (admin/jefe/relevador) |

## Volumes

| Volume | Tipo | Para qué |
|---|---|---|
| `sgr-db-data` | named | Persistencia de la DB SQL Server |
| `${HOST_PHOTOS_PATH}` | bind mount | Storage local del backend (fotos) |

## Puertos publicados al host

| Puerto | Servicio |
|---|---|
| `5100` | Web (Blazor) → es el que entra el cliente |
| `5000` | Backend API → lo consume web (red interna) y móvil (LAN) |
| `1433` | SQL Server → debug/ops |

## Sin TLS

Este compose sirve **HTTP plano**. Para HTTPS productivo recomendamos poner
nginx/caddy delante con cert (Let's Encrypt o cert del cliente).

## Troubleshooting

- **"Docker no responde"** → abrir Docker Desktop y esperar a que el ícono diga "running"
- **DB tarda 30-60s al primer arranque** → SQL Server Express necesita inicializar
  en el primer boot. `run-all.bat` espera el healthcheck antes de subir el backend.
- **Backend no se conecta a DB** → revisar `SQL_SA_PASSWORD` cumple los requisitos
  (8+ chars, mayus + minus + número + símbolo). SQL Server falla silencioso si no.
- **Cambié `HOST_IP` y la móvil sigue apuntando a la vieja** → `publish-mobile.bat` rebuild + reinstalar APK.
