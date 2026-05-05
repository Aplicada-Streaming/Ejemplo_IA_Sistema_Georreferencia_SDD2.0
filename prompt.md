# Contexto y bug actual

Repo: SGR (Sistema de Georreferencia). Trabajo en `C:\repos\Ejemplo_IA_Sistema_Georreferencia_SDD2.0` sobre la rama `main` (HEAD = `b3b95d3`, fix relevante en commit `8a378a0`).

Stack: ASP.NET Core 10 (Blazor Server interactivo + MudBlazor 8.5.1) dockerizado. Tres containers: `sgr-db` (SQL Server), `sgr-backend` (API .NET 10), `sgr-web` (Blazor Server). Levanto con `scripts\docker\host-dev\run-local.bat` (no requiere Docker Hub; consume imágenes locales).

Síntoma: `GET http://localhost:5100/_framework/blazor.web.js` devuelve **404** desde el container `sgr-web`. Como consecuencia, las páginas con interactividad MudBlazor (`/templates/new` etc) quedan estáticas — el login funciona porque es form POST + cookie auth, no necesita Blazor JS en el browser.

Causa raíz parcialmente identificada: el publish output dentro del SDK Linux container NO está emitiendo `/app/wwwroot/_framework/`. Verificado con:

```
docker exec sgr-web ls /app/wwwroot/_framework/
→ ls: cannot access '/app/wwwroot/_framework/': No such file or directory
```

En cambio, un `dotnet publish` del MISMO proyecto en Windows host (.NET SDK 10.0.202 según `global.json`) sí emite `wwwroot/_framework/blazor.web.js` (200575 bytes) y la app funciona correctamente con `ASPNETCORE_ENVIRONMENT=Production`. Por lo tanto el bug es específico del build del Dockerfile (`mcr.microsoft.com/dotnet/sdk:10.0` → `mcr.microsoft.com/dotnet/aspnet:10.0`).

# Lo que se cambió en código (commit 8a378a0)

`src/Sgr.Frontend.Web/Program.cs` ahora usa `app.MapStaticAssets()` en lugar del combo previo (`builder.WebHost.UseStaticWebAssets()` + `app.UseStaticFiles()`). El cambio se validó localmente con éxito; el problema es que el container ni siquiera tiene los archivos físicos en `wwwroot/_framework/`, así que ningún middleware los puede servir.

# Arquitectura relevante

- `src/Sgr.Frontend.Web/Sgr.Frontend.Web.csproj` — `net10.0`, PackageReference MudBlazor 8.5.1
- `src/Sgr.Frontend.Web/Dockerfile` — multi-stage. Build stage hace `dotnet restore` + `dotnet publish -c Release -o /app/publish`, runtime stage hace `COPY --from=build /app/publish .`
- `global.json` fija sdk a 10.0.202 con rollForward por default
- `.dockerignore` excluye `**/bin/`, `**/obj/` (correcto, no excluye wwwroot)
- `scripts\docker\host-dev\build-web.bat` hace `docker build` con context = repo root, `-f src/Sgr.Frontend.Web/Dockerfile`

# Hipótesis ordenadas por probabilidad

1. El SDK del image `mcr.microsoft.com/dotnet/sdk:10.0` trae una versión que no respeta global.json (10.0.202) o que tiene un bug con static web assets en publish para Blazor Web. Verificar con `docker run --rm mcr.microsoft.com/dotnet/sdk:10.0 dotnet --list-sdks`.
2. El publish corre OK pero faltan archivos por algún workload no instalado en el image (poco probable para un proyecto Web puro, pero posible si hay un trimming o AOT que descarta el `_framework`).
3. Algún MSBuild target específico de Blazor Web requiere una variable de entorno o flag (`-p:UseStaticAssets=true`, `--use-current-runtime`, etc) en el `dotnet publish` que falta en la línea actual del Dockerfile.
4. La sln referencia el proyecto mobile (Sgr.Frontend.Mobile) y eso rompe el restore en Linux. Verificar con docker logs durante un build limpio.

# Lo que necesito que hagas

## a) Reproducir el bug

```
cd scripts\docker\host-dev
stop-local.bat
docker rmi fernandofilipuzzi/sgr-web:latest
build-web.bat 2>&1 | tee build-web.log
run-local.bat
curl -sI http://localhost:5100/_framework/blazor.web.js
docker exec sgr-web ls /app/wwwroot/
```

Pegar la salida, especialmente cualquier warning del `dotnet publish` dentro de `build-web.log`.

## b) Inspeccionar el publish output dentro del container

```
docker exec sgr-web ls /app/wwwroot/
docker exec sgr-web ls /app/ | findstr staticwebassets
docker exec sgr-web cat /app/Sgr.Frontend.Web.staticwebassets.endpoints.json | findstr blazor.web.js
```

Si el `endpoints.json` tiene `blazor.web.js` pero el archivo físico no está, el publish no lo copió. Si ni siquiera hay `endpoints.json`, el static assets pipeline no corrió.

## c) Diagnosticar el SDK del container de build

```
docker run --rm mcr.microsoft.com/dotnet/sdk:10.0 dotnet --list-sdks
docker run --rm mcr.microsoft.com/dotnet/sdk:10.0 dotnet --version
```

Comparar con la versión local Windows (debería ser 10.0.202 o compatible).

## d) Reproducir el publish manual en un container intermedio para ver qué emite

```
docker run --rm -v "%CD%/../../..:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet publish src/Sgr.Frontend.Web/Sgr.Frontend.Web.csproj -c Release -o /tmp/pub
```

(La ruta del volume puede necesitar ajuste — el objetivo es montar el repo.)

Después: `docker run --rm ... ls -la /tmp/pub/wwwroot/_framework/` para confirmar si `_framework` se emitió o no.

## e) Aplicar el fix más probable

- Si la hipótesis 3 es correcta, agregar al csproj un PropertyGroup con `<PublishStaticAssets>true</PublishStaticAssets>` o similar. Si es un tema de workloads, agregar `RUN dotnet workload install` al Dockerfile de build.
- Si la hipótesis 4 (sln con mobile rompe), ya existe `sgr.no-mobile.slnf` en repo root — cambiar el Dockerfile para usar ese slnf o restaurar/publicar el csproj individual sin pasar por la sln.

## f) Validar end-to-end

Después del fix, `rebuild + run-local.bat + curl -sI http://localhost:5100/_framework/blazor.web.js` debe devolver `200 OK` con `Content-Length` cercano a `200575`. Después en browser, hard-refresh sobre `http://localhost:5100/templates/new` debería levantar la página con MudBlazor interactivo.

# Restricciones

- No tocar `src/Sgr.Frontend.Web/Program.cs` salvo que el diagnóstico lo justifique. El commit `8a378a0` ya volvió a `MapStaticAssets()` — eso está validado en Windows host.
- No modificar el `.csproj` agregando packages nuevos sin razón clara del diagnóstico.
- Si proponés cambios al Dockerfile o al csproj, mostrame el diff antes de commitear, y commiteá con mensaje en español describiendo causa raíz, no solo el síntoma.
- Esta máquina tiene Docker Desktop corriendo. Las imágenes son `fernandofilipuzzi/sgr-{db,backend,web}:latest`. El `stop-local.bat` y `run-local.bat` orquestan compose; hay `smoke-test.bat` también.

Reportá en este orden: (1) qué encontraste en el publish output del container, (2) cuál hipótesis confirmaste, (3) el fix aplicado y por qué, (4) la salida del curl post-fix.
