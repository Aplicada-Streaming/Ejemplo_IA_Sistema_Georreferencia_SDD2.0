# scripts/

Carpeta de scripts operativos del SGR. Dos universos separados:

```
scripts/
├─ dev-local/        ← Desarrollo en la máquina del developer (sin Docker)
└─ docker/
   ├─ host-dev/      ← Build y publish de imágenes a Docker Hub (developer)
   └─ host-windows-prod/  ← Deploy en la máquina destino (cliente)
```

## ¿Cuál usar?

| Escenario | Carpeta |
|---|---|
| Estoy desarrollando en mi PC con SQL Server LocalDB | `dev-local/` |
| Quiero correr tests | `dev-local/run-tests.bat` |
| Voy a publicar imágenes Docker para el cliente | `docker/host-dev/` |
| Estoy en la máquina del cliente, voy a instalar el sistema | `docker/host-windows-prod/` |

## Flujo end-to-end

```
       DEV machine                          PROD host (Windows + Docker Desktop)
       ───────────                          ─────────────────────────────────────

  1. dev-local/start-all.bat        ┐
     (loop normal de desarrollo)    │
                                    │
  2. docker/host-dev/build-all.bat  │      3. docker/host-windows-prod/configure.bat
     docker login                   │         (genera .env + patchea config móvil)
     docker/host-dev/publish-all.bat│
                                    │      4. docker/host-windows-prod/pull-all.bat
                                    └─────► 5. docker/host-windows-prod/run-all.bat
                                              (sistema accesible en HOST_IP:5100)

                                           6. docker/host-windows-prod/publish-mobile.bat
                                              (genera APK con la URL del host destino)
```

Ver READMEs específicos de cada subcarpeta para detalle.
