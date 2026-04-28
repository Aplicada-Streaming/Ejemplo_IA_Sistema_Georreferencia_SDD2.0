# syntax=docker/dockerfile:1.7
# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY global.json ./
COPY sgr.sln ./
COPY src/Sgr.Domain/*.csproj                 src/Sgr.Domain/
COPY src/Sgr.Persistence/*.csproj            src/Sgr.Persistence/
COPY src/Sgr.Modules.Identity/*.csproj       src/Sgr.Modules.Identity/
COPY src/Sgr.Modules.Templates/*.csproj      src/Sgr.Modules.Templates/
COPY src/Sgr.Modules.Surveys/*.csproj        src/Sgr.Modules.Surveys/
COPY src/Sgr.Modules.Sync/*.csproj           src/Sgr.Modules.Sync/
COPY src/Sgr.Modules.Storage/*.csproj        src/Sgr.Modules.Storage/
COPY src/Sgr.Modules.Points/*.csproj         src/Sgr.Modules.Points/
COPY src/Sgr.Frontend.Web/*.csproj           src/Sgr.Frontend.Web/

RUN dotnet restore src/Sgr.Frontend.Web/Sgr.Frontend.Web.csproj

COPY src/Sgr.Domain/                src/Sgr.Domain/
COPY src/Sgr.Persistence/           src/Sgr.Persistence/
COPY src/Sgr.Modules.Identity/      src/Sgr.Modules.Identity/
COPY src/Sgr.Modules.Templates/     src/Sgr.Modules.Templates/
COPY src/Sgr.Modules.Surveys/       src/Sgr.Modules.Surveys/
COPY src/Sgr.Modules.Sync/          src/Sgr.Modules.Sync/
COPY src/Sgr.Modules.Storage/       src/Sgr.Modules.Storage/
COPY src/Sgr.Modules.Points/        src/Sgr.Modules.Points/
COPY src/Sgr.Frontend.Web/          src/Sgr.Frontend.Web/

RUN dotnet publish src/Sgr.Frontend.Web/Sgr.Frontend.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---------- runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5100
ENV ASPNETCORE_ENVIRONMENT=Production
# Default; override-eable por compose con la URL real del backend.
ENV BackendApi__BaseUrl=http://sgr-backend:5000
EXPOSE 5100

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Sgr.Frontend.Web.dll"]
