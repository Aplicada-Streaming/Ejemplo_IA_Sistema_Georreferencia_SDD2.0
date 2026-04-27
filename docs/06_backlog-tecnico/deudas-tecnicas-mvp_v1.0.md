**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** deudas-tecnicas-mvp_v1.0.md
**Versión:** 1.0
**Estado:** Backlog para Sprint 11 (estabilización R-MVP)
**Fecha:** 2026-04-27
**Autor:** Consolidado de commits Slices 1-10

---

# Deudas técnicas diferidas durante Slices 1-10

Cada slice cerró con su funcionalidad core + tests, pero se difirieron explícitamente
items que no eran críticos para entregar valor del slice. Este documento los consolida
para que Sprint 11 los priorice contra bugs de estabilización antes del release MVP.

> Convención: cada deuda referencia el commit que la generó y el archivo concreto
> a tocar. Severidad orientativa según impacto en la experiencia del MVP final.

---

## Por slice

### Slice 3 — Modo Recorrido (E.7 commit `62f6d38`)

| ID | Severidad | Descripción | Archivos clave |
|---|---|---|---|
| DT-S3.1 | 🟡 Media | Las US menores (RN-11, modelo-datos, especificacion, TC-01) usan aún la nomenclatura "modo móvil" en menciones secundarias. La principal ya fue alineada en commit `fdb7e1b`. Faltan estas referencias para que el corpus quede 100% consistente. | docs/02_especificacion_funcional/reglas-de-negocio/RN-11*, modelo-datos-conceptual, especificacion-funcional |

### Slice 6 — Permisos por punto (E.7 commit `62f6d38` Slice 6)

| ID | Severidad | Descripción | Archivos clave |
|---|---|---|---|
| DT-S6.1 | 🟡 Media | UI hint visual "modo lectura" cuando el usuario no es creador/dueño del punto. El backend ya enforce con 403/RejectedForbidden, pero la web no deshabilita botones de edición preventivamente. | src/Sgr.Frontend.Web/Components/Pages/SurveyDetail.razor (gating de botones por permiso) |

### Slice 7 — Carga lote EXIF (E.7 commit Slice 7)

| ID | Severidad | Descripción | Archivos clave |
|---|---|---|---|
| DT-S7.1 | 🟢 Baja | Foto.PointId usa reflection para reasignar al resolver geo o fusionar. Limpiar con un método de dominio `Photo.MoveToPoint(Guid pointId)` o `Photo.AssignToPoint` permitiendo reassignment. | src/Sgr.Domain/Photos/Photo.cs |

### Slice 8 — Wizard storage (E.8 commit Slice 8)

| ID | Severidad | Descripción | Archivos clave |
|---|---|---|---|
| DT-S8.1 | 🔴 Alta | **Hot-swap del adapter activo**: cambios via wizard quedan en DB pero `StaticActiveAdapterResolver` sigue leyendo `Storage:ActiveAdapter` de appsettings hasta el próximo restart. Para producción real hay que reemplazar por un resolver dinámico que lea desde DB en cada request (cacheado con TTL corto, ej. 30s). | src/Sgr.Modules.Storage/PhotoStorageAdapterFactory.cs (`IActiveAdapterResolver`) |
| DT-S8.2 | 🟡 Media | Refactor de los Options de adapters S3/FTP/SFTP para que se rebooten con `IOptionsMonitor<T>` cuando cambia la config en DB. Hoy los `IOptions<T>` se inyectan al startup. | src/Sgr.Modules.Storage/Adapters/* + ServiceCollectionExtensions.cs |
| DT-S8.3 | 🟢 Baja | DataProtection actualmente persiste keys en el filesystem default de aspnetcore. Para deploy multi-instancia hay que configurar PersistKeysToFileSystem(shared) o PersistKeysToAzureBlobStorage o equivalente. | src/Sgr.Modules.Storage/ServiceCollectionExtensions.cs (`AddDataProtection`) |

### Slice 9 — Panel conflictos (E.9 commit Slice 9)

| ID | Severidad | Descripción | Archivos clave |
|---|---|---|---|
| DT-S9.1 | 🟡 Media | Acción `Revert` sobre conflicts tipo `post_close` lanza `NotImplemented`. Para soportarlo: reabrir el survey (cerrado→abierto + AuditEvent), reaplicar el evento original, dejarlo abierto o cerrarlo de nuevo. | src/Sgr.Modules.Sync/Application/IConflictsService.cs (rama `ConflictActions.Revert` para `PostClose`) |
| DT-S9.2 | 🟢 Baja | Notificación post-sync con badge/conteo (CA-19.3). Hoy el panel se actualiza al entrar; no hay push activo al dashboard cuando llegan conflicts nuevos. | Frontend Web — agregar SignalR o polling en MainLayout |

### Slice 10 — Fusión puntos (E.10 commit Slice 10)

| ID | Severidad | Descripción | Archivos clave |
|---|---|---|---|
| DT-S10.1 | 🟡 Media | **Selector field-by-field** en la fusión (CA-22.2 partial). Hoy el `kept` hereda todos los campos; el dropped pierde los valores que tenía y el kept no los pisa. UX más rica permitiría elegir, por cada campo divergente, si queda el de A o el de B. | src/Sgr.Modules.Sync/Application/IMergeCandidatesService.cs + MergeCandidates.razor |
| DT-S10.2 | 🟡 Media | **Mini-mapa lado a lado** + galerías de fotos en la pantalla de detail del candidato. Hoy se muestran metadatos de cada punto en cards, sin visualización geográfica. | src/Sgr.Frontend.Web/Components/Pages/MergeCandidates.razor |
| DT-S10.3 | 🟢 Baja | `Photo.MoveToPoint()` en lugar de reflection para reasignar fotos del dropped al kept. Igual que DT-S7.1. | Mismo: src/Sgr.Domain/Photos/Photo.cs |
| DT-S10.4 | 🟢 Baja | Detector RN-09 sólo se dispara en `point.created`. Si un punto se MUEVE a una zona donde había otro punto cercano (vía `coords` field_updated), no re-detecta. RN-09 lo menciona implícitamente; agregar hook en `ApplyPointFieldUpdatedAsync` cuando el field es `coords`. | src/Sgr.Modules.Sync/Application/IEventApplier.cs |

---

## Deudas transversales (no de un slice particular)

| ID | Severidad | Descripción | Archivos clave |
|---|---|---|---|
| DT-X.1 | 🔴 Alta | **CI pipeline**: el repo no tiene workflow de GitHub Actions / Azure Pipelines. La regla de "tests pasan en CI" del DoR se cumple corriendo `dotnet test` localmente. Para ramping del equipo es esencial automatizarlo. | .github/workflows/ci.yml (crear) |
| DT-X.2 | 🟡 Media | Tests E2E con backend real + DB SQL Server (vimos que el seeding bloquea SQL Server local en sesiones de dev). Los tests integration usan EF InMemory que no atrapa diferencias con SQL real. | tests/Sgr.Tests.Integration.Sql (crear) usando Testcontainers + mssql |
| DT-X.3 | 🟡 Media | Tests integración para adapters S3/FTP/SFTP (CA-18.4). Hoy sólo Local tiene cobertura real; los otros pasan por unit tests del módulo. Testcontainers + minio + vsftpd resolverían. | tests/Sgr.Tests.Integration/Storage/ (extender) |
| DT-X.4 | 🟢 Baja | Logging estructurado con correlation-id ya está activo. Falta dashboard / agregación (Seq, ELK, Application Insights). | Configuración del entorno productivo |

---

## Sugerencia de priorización para Sprint 11

1. **DT-S8.1 + DT-S8.2** (alta): hot-swap storage es funcionalidad esperada por el cliente; sin esto el wizard pide restart manual del backend en producción.
2. **DT-X.1** (alta): CI pipeline es operacional y previene regressions.
3. **DT-S6.1** (media): UI lectura tiene gran impacto en percepción de calidad.
4. **DT-S10.1, DT-S10.2** (media): la fusión es Could Have; mejoras visuales pueden esperar a v2.
5. **DT-S9.1** (media): post-close revert es edge case; documentar workaround manual mientras tanto.
6. **Resto** (baja): cleanup tras MVP.

---

**Fin del documento — deudas-tecnicas-mvp_v1.0.md**
