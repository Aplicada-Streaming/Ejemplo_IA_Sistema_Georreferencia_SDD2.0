**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-04-push-eventos-idempotencia_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# US-04 — Push de eventos al backend con idempotencia

**Épica:** EP-01.1 · **MoSCoW:** Must · **SP:** 13 · **Sprint sugerido:** Slice 1

> Como **cliente sincronizando**,
> quiero **enviar mis eventos al backend con la garantía de que reenvíos no duplicarán datos y que los conflictos LWW se resuelvan determinísticamente**,
> para **converger con otros colaboradores sin perder información**.

## CUs y RNs relacionados
- CU: [CU-08](../../02_especificacion_funcional/casos-de-uso/CU-08-sincronizar-relevamiento_v1.0.md)
- RN: [RN-06](../../02_especificacion_funcional/reglas-de-negocio/RN-06-guids-cliente-idempotencia_v1.0.md), [RN-07](../../02_especificacion_funcional/reglas-de-negocio/RN-07-resolucion-conflictos-lww-y-precedencia_v1.0.md), [RN-10](../../02_especificacion_funcional/reglas-de-negocio/RN-10-eventos-append-only_v1.0.md)

## Alcance
- Endpoint `POST /api/v1/sync/push` que recibe lista de eventos.
- Validación de schema y de idempotencia por `(event_id, timestamp_original, event_type)`.
- Aplicación al modelo central; persistencia en `AuditEvents` (append-only).
- Resolución LWW por campo + precedencia del dueño.
- Respuesta con resumen: aplicados, deduplicados, en conflicto.
- Trigger / política append-only sobre `AuditEvents`.

## Criterios de aceptación
- **CA-4.1** Mismo evento enviado dos veces → backend lo aplica una sola vez.
- **CA-4.2** Dos eventos sobre el mismo campo → gana el de timestamp original mayor.
- **CA-4.3** Edición del colaborador vs. del dueño sobre mismo campo → gana dueño.
- **CA-4.4** Eventos sobre campos distintos del mismo punto → ambos se aplican (no son conflicto).
- **CA-4.5** Trigger DB rechaza UPDATE/DELETE sobre `AuditEvents`.

## Dependencias
- US-01, US-02, US-03, BT-10.

## DoR — checklist
- [x] Atada a EP-01.1.
- [x] Criterios verificables (tests con escenarios de race).
- [x] Estimada.
- [x] Spike de sync de Fase 0 ejecutado.
- [x] Cabe en un sprint.
- [x] PO confirma.

---
**Fin — US-04**
