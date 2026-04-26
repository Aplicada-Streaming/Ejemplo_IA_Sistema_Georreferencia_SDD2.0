**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** contratos-interfaces_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-05 via orquestador

---

# Contratos de Interfaces

Contrato público del backend (API REST) y contratos internos relevantes (eventos del outbox, puerto de Storage). El documento OpenAPI vivo se genera automáticamente desde el código y se sirve en `/swagger`. Este documento describe los recursos, patrones y eventos a alto nivel — la fuente de verdad ejecutable es OpenAPI.

---

## 1. Convenciones del API REST

- **Versionado:** prefijo `/api/v1/...` (RNF-05 + DD-16).
- **Auth:** header `Authorization: Bearer <jwt>`.
- **Correlación:** header `X-Correlation-ID` opcional en request, siempre presente en response.
- **IDs:** GUIDs v4 generados en cliente.
- **Timestamps:** ISO 8601 UTC en transporte; el servidor recibe `timestamp_original` además del implícito de llegada.
- **Errores:** RFC 7807 Problem Details (`application/problem+json`).

| Código HTTP | Significado |
|---|---|
| 200 | OK con cuerpo |
| 201 | Created (con Location header) |
| 202 | Accepted (operaciones diferidas como sync) |
| 204 | OK sin cuerpo |
| 400 | Validación sintáctica (payload mal formado) |
| 401 | Sin auth o token inválido |
| 403 | Autorización fallida |
| 404 | Recurso inexistente |
| 409 | Conflicto de estado (e.g. plantilla publicada inmutable) |
| 422 | Validación de dominio (e.g. cambio de tipo de campo heredado) |
| 500 | Error interno |

---

## 2. Recursos del API REST

### 2.1. Identity

| Método | Path | Descripción |
|---|---|---|
| POST | `/api/v1/auth/login` | ROPC: ingresa email + password, devuelve JWT + refresh token. |
| POST | `/api/v1/auth/refresh` | Rota refresh token + emite nuevo JWT. |
| POST | `/api/v1/auth/logout` | Revoca refresh token. |
| POST | `/api/v1/users/register` | Registro de jefe o relevador (estado `pendiente_aceptacion`). |
| GET | `/api/v1/users/me` | Datos del usuario autenticado. |
| GET | `/api/v1/users?role=&status=&area=` | Listado por rol/área para aceptación. |
| POST | `/api/v1/users/{id}/accept` | Aceptar usuario pendiente (admin → jefe; jefe → relevador). |
| POST | `/api/v1/users/{id}/disable` | Inhabilitar (reversible) — solo admin sobre jefe. |
| POST | `/api/v1/users/{id}/enable` | Reactivar inhabilitado. |
| POST | `/api/v1/users/{id}/delete` | Dar de baja (terminal) — solo admin sobre jefe. |
| GET / POST | `/api/v1/areas` | CRUD de áreas. |

### 2.2. Templates

| Método | Path | Descripción |
|---|---|---|
| GET | `/api/v1/templates` | Lista de plantillas (con árbol de herencia). |
| GET | `/api/v1/templates/{id}` | Plantilla con sus versiones. |
| POST | `/api/v1/templates` | Crear plantilla hija. |
| GET | `/api/v1/template-versions/{id}` | Detalle de una versión. |
| GET | `/api/v1/template-versions/{id}/resolved` | Versión con herencia aplicada (lo que renderiza el frontend). |
| POST | `/api/v1/template-versions` | Crear nueva versión (borrador). |
| POST | `/api/v1/template-versions/{id}/publish` | Publicar versión (la vuelve inmutable). |

### 2.3. Surveys

| Método | Path | Descripción |
|---|---|---|
| GET | `/api/v1/surveys?area=&status=&tag=&from=&to=` | Listado con filtros. |
| GET | `/api/v1/surveys/{id}` | Detalle del relevamiento. |
| POST | `/api/v1/surveys` | Crear relevamiento (con GUID generado en cliente). |
| PATCH | `/api/v1/surveys/{id}` | Edición de metadata (nombre, etiquetas). |
| POST | `/api/v1/surveys/{id}/close` | Cerrar. |
| POST | `/api/v1/surveys/{id}/reopen` | Reabrir. |
| DELETE | `/api/v1/surveys/{id}` | Eliminación lógica (sólo dueño / jefe). |
| POST | `/api/v1/surveys/{id}/collaborators` | Agregar colaborador. |
| DELETE | `/api/v1/surveys/{id}/collaborators/{user_id}` | Quitar colaborador. |

### 2.4. Points

| Método | Path | Descripción |
|---|---|---|
| GET | `/api/v1/surveys/{id}/points` | Puntos del relevamiento (con metadata de origen). |
| GET | `/api/v1/points/{id}` | Detalle de un punto + ValorDeCampo. |
| GET | `/api/v1/points/{id}/events` | Trazabilidad histórica del punto ([CU-12](../02_especificacion_funcional/casos-de-uso/CU-12-consultar-trazabilidad-punto_v1.0.md)). |

> Las **modificaciones** sobre puntos (creación, edición, eliminación) NO usan endpoints REST clásicos; se hacen vía `POST /api/v1/sync/push` enviando eventos. Esto unifica el camino de móvil offline y web online.

### 2.5. Photos

| Método | Path | Descripción |
|---|---|---|
| GET | `/api/v1/photos/{id}` | Metadata de la foto. |
| GET | `/api/v1/photos/{id}/content` | Contenido binario (lee del adaptador con que fue creada). |
| GET | `/api/v1/photos/{id}/thumbnail` | Thumbnail. |

> La subida de fotos también va por `POST /api/v1/sync/push` con multipart, donde el evento `photo_created` adjunta el binario. Idempotente por GUID.

### 2.6. Sync

| Método | Path | Descripción |
|---|---|---|
| POST | `/api/v1/sync/push` | Recibe lista de eventos del cliente. Aplica idempotencia, LWW, post-cierre, candidatos a fusión. Retorna 202 con resumen. |
| GET | `/api/v1/surveys/{id}/changes?since={timestamp}` | Pull diferencial. Retorna eventos posteriores a `since`. |
| GET | `/api/v1/sync/status` | Estado del usuario: pendientes, conflictos. |

#### Schema del evento (push)

```jsonc
{
  "event_id": "guid",
  "entity_type": "survey | point | photo",
  "entity_id": "guid",
  "event_type": "created | field_updated | deleted | restored | merged",
  "field": "title | description | coords | comment | <field_key>",  // null si created/deleted
  "old_value": "...",     // null si created
  "new_value": "...",     // null si deleted
  "author_id": "guid",
  "origin": "mobile_capture | mobile_edit | web_edit | web_manual_upload",
  "device_id": "string|null",
  "timestamp_original": "ISO8601",
  "payload_extra": { /* específico al event_type */ }
}
```

### 2.7. Conflicts (panel)

| Método | Path | Descripción |
|---|---|---|
| GET | `/api/v1/conflicts?type=&survey=&area=` | Listado de conflictos pendientes. |
| GET | `/api/v1/conflicts/{id}` | Detalle. |
| POST | `/api/v1/conflicts/{id}/resolve` | Resolución manual (revertir, fusionar, mantener separados, reabrir). |

### 2.8. SystemConfig

| Método | Path | Descripción |
|---|---|---|
| GET | `/api/v1/system-config` | Config actual (sin secretos). |
| POST | `/api/v1/system-config/storage` | Cambiar storage (con validación de conexión). |
| POST | `/api/v1/system-config/storage/test` | Probar conexión sin persistir. |

### 2.9. Manual upload web

| Método | Path | Descripción |
|---|---|---|
| POST | `/api/v1/surveys/{id}/manual-upload` | Subida en lote multipart con modo de agrupación. Devuelve resumen + cola de pendientes. |
| GET | `/api/v1/surveys/{id}/manual-upload/pending` | Fotos pendientes de georreferenciar. |
| POST | `/api/v1/photos/{id}/geo-resolve` | Asigna coordenadas manualmente a una foto pendiente. |

---

## 3. Eventos internos del outbox del backend

| Evento | Disparador | Consumer |
|---|---|---|
| `point.created.v1` | Sync recibe punto nuevo | Worker sync (recalcula candidatos) |
| `point.coords.updated.v1` | Cambio del campo `coords` | Worker sync (recalcula candidatos) |
| `photo.uploaded.v1` | Foto subida a través del API | Worker imágenes (normalización + thumb) |
| `photo.processed.v1` | Worker imágenes terminó | Sync (notifica al cliente vía pull) |
| `merge.requested.v1` | Acción manual del usuario en panel | Sync (aplica merge, emite evento `merged`) |
| `notification.sync.v1` | Resolución automática de conflicto | Sync (encola para móvil/web) |

Schema base:

```jsonc
{
  "event_id": "guid",
  "event_type": "string",
  "occurred_at": "ISO8601",
  "payload": { /* específico */ },
  "correlation_id": "guid"
}
```

---

## 4. Puerto de Storage (interno hexagonal)

```csharp
public interface IPhotoStorageAdapter
{
    string AdapterName { get; } // "local" | "s3" | "ftp" | "sftp"
    Task<StoredPhotoRef> StoreAsync(Stream content, StorageHints hints, CancellationToken ct);
    Task<Stream> ReadAsync(string adapterRef, CancellationToken ct);
    Task DeleteAsync(string adapterRef, CancellationToken ct);
    Task<bool> TestConnectionAsync(CancellationToken ct);
}

public record StoredPhotoRef(string AdapterRef, string AdapterName, long SizeBytes, string ContentHash);
public record StorageHints(string SuggestedFolder, string ContentType, string FileName);
```

- Cada adaptador implementa esta interfaz exactamente. La selección del adaptador activo se resuelve por `SystemConfig`.
- Las Fotos persisten `{adapter_ref, adapter_name}` que les permite leerse desde el adaptador con el que fueron creadas, aunque el sistema haya cambiado de configuración ([RN-12](../02_especificacion_funcional/reglas-de-negocio/RN-12-storage-datos-previos_v1.0.md)).

---

## 5. Trazabilidad

| Documento upstream | Aporte |
|---|---|
| [arquitectura-solucion](arquitectura-solucion_v1.0.md) | Componentes y módulos cuyas interfaces se contractualizan |
| [especificacion-funcional](../02_especificacion_funcional/especificacion-funcional_v1.0.md) | CUs cuyo soporte exige cada endpoint |
| [modelo-datos-conceptual](../02_especificacion_funcional/modelo-datos-conceptual_v1.0.md) | Entidades cuya forma define los recursos del API |
| `devs/intake/PROJECT-BRIEF.md` Sec. 5 | Esquema de eventos y sync diferencial |

---

**Fin del documento — contratos-interfaces_v1.0.md**
