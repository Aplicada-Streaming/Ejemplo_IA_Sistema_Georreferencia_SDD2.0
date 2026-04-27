# syntax=docker/dockerfile:1.7
# ---------- build stage ----------
# Usamos el SDK de .NET 10 (preview/RC) — el repo fija 10.0.202 en global.json.
# Si en el host build no está el tag exacto, el build falla con un mensaje claro;
# en ese caso bajar a "10.0" general.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiamos primero los .csproj para que el restore se cachee bajo capas.
# Cualquier cambio en codigo NO invalida el restore; solo lo invalidan los .csproj.
COPY Directory.Build.props ./
COPY global.json ./
COPY sgr.sln ./
COPY sgr.no-mobile.slnf ./
COPY src/Sgr.Domain/*.csproj                 src/Sgr.Domain/
COPY src/Sgr.Persistence/*.csproj            src/Sgr.Persistence/
COPY src/Sgr.Modules.Identity/*.csproj       src/Sgr.Modules.Identity/
COPY src/Sgr.Modules.Templates/*.csproj      src/Sgr.Modules.Templates/
COPY src/Sgr.Modules.Surveys/*.csproj        src/Sgr.Modules.Surveys/
COPY src/Sgr.Modules.Sync/*.csproj           src/Sgr.Modules.Sync/
COPY src/Sgr.Modules.Storage/*.csproj        src/Sgr.Modules.Storage/
COPY src/Sgr.Modules.Points/*.csproj         src/Sgr.Modules.Points/
COPY src/Sgr.Backend.Api/*.csproj            src/Sgr.Backend.Api/

RUN dotnet restore src/Sgr.Backend.Api/Sgr.Backend.Api.csproj

# Ahora copiamos el resto del codigo.
COPY src/Sgr.Domain/                src/Sgr.Domain/
COPY src/Sgr.Persistence/           src/Sgr.Persistence/
COPY src/Sgr.Modules.Identity/      src/Sgr.Modules.Identity/
COPY src/Sgr.Modules.Templates/     src/Sgr.Modules.Templates/
COPY src/Sgr.Modules.Surveys/       src/Sgr.Modules.Surveys/
COPY src/Sgr.Modules.Sync/          src/Sgr.Modules.Sync/
COPY src/Sgr.Modules.Storage/       src/Sgr.Modules.Storage/
COPY src/Sgr.Modules.Points/        src/Sgr.Modules.Points/
COPY src/Sgr.Backend.Api/           src/Sgr.Backend.Api/

RUN dotnet publish src/Sgr.Backend.Api/Sgr.Backend.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---------- runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Carpeta donde el adapter Local persiste las fotos. Override-eable con
# Storage__Local__RootPath; el compose la mapea a un volume del host.
RUN mkdir -p /var/sgr/photos
ENV Storage__Local__RootPath=/var/sgr/photos

# Kestrel escucha en 0.0.0.0:5000 dentro del container; el host publica el puerto.
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5000

COPY --from=build /app/publish .

# Sin HEALTHCHECK: la imagen base aspnet:10.0 no trae wget ni curl. El compose
# usa depends_on simple sobre el backend; el unico healthcheck con valor es el
# del DB, definido en el compose.

ENTRYPOINT ["dotnet", "Sgr.Backend.Api.dll"]
