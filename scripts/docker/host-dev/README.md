# scripts/docker/host-dev/

Scripts que corre el **developer** para construir y publicar las imágenes Docker
del SGR a Docker Hub. Producen los tags que después `host-windows-prod/` baja.

## Pre-requisitos

- Docker Desktop instalado y corriendo
- Cuenta de Docker Hub (default: `fernandofilipuzzi`; override con
  `set DOCKERHUB_USER=otro_user` antes de ejecutar)
- `docker login` ejecutado al menos una vez en esta sesión

## Imágenes que produce

| Tag | Build a partir de |
|---|---|
| `<DOCKERHUB_USER>/sgr-backend:latest` | `src/Sgr.Backend.Api/Dockerfile` |
| `<DOCKERHUB_USER>/sgr-web:latest` | `src/Sgr.Frontend.Web/Dockerfile` |
| `<DOCKERHUB_USER>/sgr-db:latest` | `pull mcr.microsoft.com/mssql/server:2022-latest` + retag |

## Scripts

### Build (no toca el registry)

| Script | Acción |
|---|---|
| `build-backend.bat` | `docker build` del backend con contexto = repo root |
| `build-web.bat` | `docker build` del web con contexto = repo root |
| `build-db.bat` | `docker pull` de mssql:2022 + `docker tag` |
| `build-all.bat` | invoca los 3 anteriores en orden |

### Publish (push a Docker Hub)

| Script | Acción |
|---|---|
| `publish-backend.bat` | `docker push` del backend |
| `publish-web.bat` | `docker push` del web |
| `publish-db.bat` | `docker push` del DB (ver nota) |
| `publish-all.bat` | invoca los 3 anteriores |

## Quickstart

```cmd
cd scripts\docker\host-dev
docker login
build-all.bat
publish-all.bat
```

## Smoke test antes de publicar

`smoke-test.bat` orquesta una validación local end-to-end **sin tocar Docker Hub**.
Después de que pasa, podés publicar con confianza.

```cmd
cd scripts\docker\host-dev
smoke-test.bat
```

Hace en orden:
1. Verifica que Docker Desktop responde.
2. Crea un `.env` mínimo en `host-windows-prod/` si no existe (passwords de smoke,
   storage en `%TEMP%\sgr-smoke-photos`). Si ya tenés un `.env` real lo respeta.
3. `docker compose config` valida sintaxis.
4. `build-all.bat` buildea/pull-ea las 3 imágenes localmente.
5. `docker compose up -d` levanta el stack.
6. Espera al healthcheck de `sgr-db` (~60-90s la primera vez).
7. Imprime URLs y credenciales para verificación visual.

Validación manual (lo que hacés vos a mano):
- Browser → `http://localhost:5100` carga el login.
- Login con `admin@vialidad.local / Admin1234!`.
- Crear un relevamiento; aparece en el listado.
- `http://localhost:5000/health` responde 200.

Si algo falla, los logs están en `docker compose logs -f`.
El stack queda arriba; bajarlo con `call ..\host-windows-prod\stop-all.bat`.

## Nota sobre `publish-db.bat`

`sgr-db` es esencialmente un retag de la imagen oficial de Microsoft (~1.5 GB).
Publicarla bajo tu user puede tardar. Alternativa más liviana: editar
`scripts/docker/host-windows-prod/docker-compose.yml` para que `sgr-db` apunte
directamente a `mcr.microsoft.com/mssql/server:2022-latest` y omitir el publish.
Mantenemos la simetría 3-imágenes para que el flujo del cliente sea uniforme.
