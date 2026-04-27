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
| DT-S6.1 | ✅ Sprint 11 | Banner informacional en SurveyDetail visible sólo para relevadores recordando RN-01 (sólo el creador del punto o el dueño del survey pueden mutarlo desde el móvil). Para admin/jefe no aparece porque tienen permiso global. La web no edita puntos directamente: la deshabilitación preventiva de botones no aplicaba. | src/Sgr.Frontend.Web/Components/Pages/SurveyDetail.razor |

### Slice 7 — Carga lote EXIF (E.7 commit Slice 7)

| ID | Severidad | Descripción | Archivos clave |
|---|---|---|---|
| DT-S7.1 | 🟢 Baja | Foto.PointId usa reflection para reasignar al resolver geo o fusionar. Limpiar con un método de dominio `Photo.MoveToPoint(Guid pointId)` o `Photo.AssignToPoint` permitiendo reassignment. | src/Sgr.Domain/Photos/Photo.cs |

### Slice 8 — Wizard storage (E.8 commit Slice 8)

| ID | Severidad | Descripción | Archivos clave |
|---|---|---|---|
| DT-S8.1 | ✅ Sprint 11 | **Hot-swap del adapter activo** — `PhotoStorageAdapterFactory` ahora consulta `SystemConfig.storage.active` con cache TTL 30s y construye un adapter ephemeral con la config de DB; fallback a appsettings si no hay config. Cambios via wizard se reflejan en ≤30s sin restart. | src/Sgr.Modules.Storage/PhotoStorageAdapterFactory.cs |
| DT-S8.2 | ✅ Sprint 11 | Resuelto junto con DT-S8.1 vía `IStorageAdapterBuilder` que construye adapters ephemerales con la config de DB (no via `IOptionsMonitor` puro, pero efecto equivalente: cada cambio de config produce un adapter nuevo en el siguiente request post-cache-expiry). | src/Sgr.Modules.Storage/Configuration/IStorageAdapterBuilder.cs |
| DT-S8.3 | 🟢 Baja | DataProtection actualmente persiste keys en el filesystem default de aspnetcore. Para deploy multi-instancia hay que configurar PersistKeysToFileSystem(shared) o PersistKeysToAzureBlobStorage o equivalente. | src/Sgr.Modules.Storage/ServiceCollectionExtensions.cs (`AddDataProtection`) |

### Slice 9 — Panel conflictos (E.9 commit Slice 9)

| ID | Severidad | Descripción | Archivos clave |
|---|---|---|---|
| DT-S9.1 | ✅ Sprint 11 | `Revert` sobre `post_close` ahora reabre el survey (con AuditEvent del cambio de status), persiste el reopen, y reemite el `point.created` con el payload original como evento nuevo. El survey queda abierto — re-cerrarlo es decisión explícita del admin. `Survey.Reopen()` agregado al dominio. Test integración cubre el flujo end-to-end. | src/Sgr.Domain/Surveys/Survey.cs (`Reopen`) + src/Sgr.Modules.Sync/Application/IConflictsService.cs |
| DT-S9.2 | 🟢 Baja | Notificación post-sync con badge/conteo (CA-19.3). Hoy el panel se actualiza al entrar; no hay push activo al dashboard cuando llegan conflicts nuevos. | Frontend Web — agregar SignalR o polling en MainLayout |

### Slice 10 — Fusión puntos (E.10 commit Slice 10)

| ID | Severidad | Descripción | Archivos clave |
|---|---|---|---|
| DT-S10.1 | ✅ Sprint 11 | Selector field-by-field implementado: `MergeAsync(strategy, fieldChoices?)` acepta un dict (fieldKey → "a" \| "b") que sobreescribe campos del kept con el valor del lado elegido. Soporta built-in (title/description) y custom fields via PointFieldValues. UI: en cada card de candidato pendiente, las divergencias entre A y B muestran radio buttons. | src/Sgr.Modules.Sync/Application/IMergeCandidatesService.cs + MergeCandidates.razor |
| DT-S10.2 | ✅ Sprint 11 (parcial) | Mini-mapa con ambos puntos (markers A/B + centro automático) en la card de cada candidato. Galerías de fotos lado a lado siguen pendientes — el patrón actual abre el `PhotoGalleryDialog` por cada punto, integrarlo en la card de fusión queda como UX nice-to-have. | src/Sgr.Frontend.Web/Components/Pages/MergeCandidates.razor |
| DT-S10.3 | 🟢 Baja | `Photo.MoveToPoint()` en lugar de reflection para reasignar fotos del dropped al kept. Igual que DT-S7.1. | Mismo: src/Sgr.Domain/Photos/Photo.cs |
| DT-S10.4 | 🟢 Baja | Detector RN-09 sólo se dispara en `point.created`. Si un punto se MUEVE a una zona donde había otro punto cercano (vía `coords` field_updated), no re-detecta. RN-09 lo menciona implícitamente; agregar hook en `ApplyPointFieldUpdatedAsync` cuando el field es `coords`. | src/Sgr.Modules.Sync/Application/IEventApplier.cs |

---

## Deudas transversales (no de un slice particular)

| ID | Severidad | Descripción | Archivos clave |
|---|---|---|---|
| DT-X.1 | ✅ Sprint 11 | **CI pipeline** — `.github/workflows/ci.yml` con `dotnet build + test` sobre `sgr.no-mobile.slnf` (filter que excluye Sgr.Frontend.Mobile para no requerir MAUI workloads en runners Linux). Sube TRX como artifact para drilldown de fallos. | .github/workflows/ci.yml + sgr.no-mobile.slnf |
| DT-X.2 | 🟡 Media | Tests E2E con backend real + DB SQL Server (vimos que el seeding bloquea SQL Server local en sesiones de dev). Los tests integration usan EF InMemory que no atrapa diferencias con SQL real. | tests/Sgr.Tests.Integration.Sql (crear) usando Testcontainers + mssql |
| DT-X.3 | 🟡 Media | Tests integración para adapters S3/FTP/SFTP (CA-18.4). Hoy sólo Local tiene cobertura real; los otros pasan por unit tests del módulo. Testcontainers + minio + vsftpd resolverían. | tests/Sgr.Tests.Integration/Storage/ (extender) |
| DT-X.4 | 🟢 Baja | Logging estructurado con correlation-id ya está activo. Falta dashboard / agregación (Seq, ELK, Application Insights). | Configuración del entorno productivo |

---

## Sugerencia de priorización para Sprint 11

**Resueltas en Sprint 11**:
- ✅ DT-X.1: CI pipeline GitHub Actions con solution filter sin MAUI
- ✅ DT-S8.1 + DT-S8.2: hot-swap storage activo (factory dinámico con cache TTL 30s)
- ✅ DT-S9.1: post_close revert reabre survey + reaplica evento
- ✅ DT-S10.2: mini-mapa lado a lado en pantalla de fusión (galerías de fotos pendientes)
- ✅ DT-S6.1: banner informacional para relevadores en SurveyDetail
- ✅ DT-S10.1: selector field-by-field en la fusión (built-in + custom fields)

**Pendientes priorizadas (todas baja-media para Sprint 12+)**:
1. **DT-X.2** (media): tests E2E con SQL real via Testcontainers.
2. **DT-X.3** (media): tests integración para adapters S3/FTP/SFTP.
3. **DT-S9.2** (baja): notificación push post-sync (SignalR).
4. **DT-S7.1 / DT-S10.3** (baja): `Photo.MoveToPoint()` limpio en lugar de reflection.
5. **DT-S10.4** (baja): detector RN-09 también en `coords` field_updated.
6. **DT-S8.3** (baja): DataProtection con keys persistidas para deploy multi-instancia.
7. **DT-S3.1** (baja): docs menores con nomenclatura "modo móvil" residual.
8. **DT-X.4** (baja): logging dashboard (Seq/ELK).

---

**Fin del documento — deudas-tecnicas-mvp_v1.0.md**
