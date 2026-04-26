**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** CU-08-sincronizar-relevamiento_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# CU-08 — Sincronizar relevamiento bidireccional con resolución de conflictos

**Código:** CU-08
**Actor primario:** Relevador (sync explícito) / Sistema (sync automático en background)
**Actores secundarios:** Worker de sincronización (backend), Jefe de área (recibe notificaciones de conflicto pendiente)
**Frente:** Móvil + Backend (también disparable desde web)

## Precondiciones

- Existe conexión disponible (al menos intermitente).
- El móvil tiene operaciones en outbox y/o quiere traer cambios de otros colaboradores.
- El usuario está autenticado.

## Postcondiciones

- La outbox local queda drenada hasta donde haya sido posible.
- El móvil tiene los eventos remotos posteriores al último timestamp de pull.
- Los conflictos se resuelven automáticamente por LWW por campo + precedencia del dueño ([RN-07](../reglas-de-negocio/RN-07-resolucion-conflictos-lww-y-precedencia_v1.0.md)).
- Los casos especiales se marcan en panel de conflictos pendientes (capturas post-cierre, eliminaciones con actividad posterior, candidatos a fusión).
- Los usuarios afectados reciben notificación post-sync con resumen de cambios y conflictos.

## Flujo principal — Sync bidireccional

1. El móvil dispara sync (manual con botón, o automático cuando detecta conexión).
2. **Push**: el móvil envía las operaciones de la outbox al backend, en orden de timestamp original.
3. El backend valida cada operación y la aplica al modelo central. Operaciones idempotentes por GUID + timestamp ([RN-06](../reglas-de-negocio/RN-06-guids-cliente-idempotencia_v1.0.md)).
4. **Pull**: el móvil pide al backend los eventos del relevamiento posteriores a su último `last_pulled_at`.
5. El backend devuelve la lista de eventos.
6. El móvil aplica los eventos a su DB local.
7. Para cada conflicto detectado en el backend (edición concurrente del mismo campo): el sistema aplica LWW por timestamp original (con excepción de precedencia del dueño).
8. Para cada operación con casos especiales: se registra en panel de conflictos para revisión humana.
9. El backend, al recibir un Punto nuevo, ejecuta detección de candidatos a fusión ([RN-09](../reglas-de-negocio/RN-09-deteccion-candidatos-fusion_v1.0.md)).
10. El móvil persiste el nuevo `last_pulled_at`.
11. El sistema notifica al usuario el resumen: "Sincronizado · N cambios · M conflictos para revisar".

## Flujos alternativos

- 1a. **Sync parcial**: si la subida se corta tras N de M fotos, los reintentos por foto son idempotentes y la próxima sync continúa sin duplicar.
- 1b. **Reintentos exponenciales** del outbox: cuando una operación falla, se reintenta a los 5s, 15s, 1m, 5m, 15m. Tras un número configurable de reintentos pasa a `terminal_error`.
- 1c. **Resolución de conflictos manual**: el usuario abre el panel y resuelve un conflicto de sobrescritura revertiéndolo (lo cual genera una nueva edición con timestamp actual, que vuelve a aplicar LWW).
- 1d. **Reapertura post-cierre**: si tras sync el dueño ve "hay N capturas posteriores al cierre", el sistema le ofrece reabrir el relevamiento (que aplica las capturas pendientes) o mantenerlas rechazadas.

## Flujos de error

- E1. Token JWT vencido durante sync → re-autenticación silenciosa o redirigir a login.
- E2. Conflicto de plantilla: el evento se generó sobre una versión que el backend ya no acepta → el sistema rechaza el evento con mensaje claro al usuario.
- E3. Backend devuelve 409 por inconsistencia → el cliente queda en estado de error para esa operación, registra y reintenta tras backoff.
- E4. Storage del backend caído → la subida de fotos queda diferida; el resto de los eventos puede aplicarse.

## Reglas de negocio relacionadas

- [RN-06](../reglas-de-negocio/RN-06-guids-cliente-idempotencia_v1.0.md), [RN-07](../reglas-de-negocio/RN-07-resolucion-conflictos-lww-y-precedencia_v1.0.md), [RN-08](../reglas-de-negocio/RN-08-capturas-post-cierre_v1.0.md), [RN-09](../reglas-de-negocio/RN-09-deteccion-candidatos-fusion_v1.0.md), [RN-10](../reglas-de-negocio/RN-10-eventos-append-only_v1.0.md).

## Trazabilidad

- Origen: [NB-02](../../01_necesidades_negocio/necesidades-de-negocio/NB-02-trabajo-offline-y-colaborativo_v1.0.md).
- RFs cubiertos: RF-34, RF-35, RF-36, RF-37, RF-38, RF-39, RF-40, RF-41, RF-42, RF-43.

## Criterios de aceptación

- **CA-08.1** — *Given* dos dispositivos offline con eventos sobre el mismo punto, *when* ambos sincronizan, *then* los eventos se aplican en orden por timestamp original y prevalece el de timestamp posterior por campo (excepto precedencia de dueño).
- **CA-08.2** — *Given* un mismo evento subido dos veces por reintentos, *when* el backend lo recibe, *then* solo se aplica una vez (idempotencia por GUID + timestamp).
- **CA-08.3** — *Given* un colaborador edita un campo y simultáneamente el dueño edita el mismo campo, *when* sincronizan, *then* gana la edición del dueño aunque el timestamp del colaborador sea posterior.
- **CA-08.4** — *Given* sync con corte tras subir 3 de 8 fotos, *when* se reintenta, *then* solo se suben las 5 restantes (no se duplica).
- **CA-08.5** — *Given* un punto nuevo subido por el colaborador A cerca de un punto del colaborador B (mismo relevamiento), *when* el backend procesa el evento, *then* se crea un CandidatoAFusión en estado `pendiente`.
- **CA-08.6** — *Given* el dueño cerró el relevamiento y un colaborador captura offline después, *when* el colaborador sincroniza, *then* las capturas posteriores al cierre quedan en estado de revisión pendiente; el dueño ve la opción de reabrir.
- **CA-08.7** — *Given* sincronización exitosa con N cambios y M conflictos, *when* el usuario ve la barra superior, *then* aparece el badge "Sincronizado · N cambios · M conflictos".

---

**Fin del documento — CU-08-sincronizar-relevamiento_v1.0.md**
