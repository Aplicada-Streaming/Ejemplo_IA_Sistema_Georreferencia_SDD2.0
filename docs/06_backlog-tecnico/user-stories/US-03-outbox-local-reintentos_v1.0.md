**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-03-outbox-local-reintentos_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# US-03 — Outbox local móvil con reintentos exponenciales

**Épica:** EP-01.1 · **MoSCoW:** Must · **SP:** 13 · **Sprint sugerido:** Slice 1

> Como **relevador trabajando offline**,
> quiero **que mis acciones se persistan localmente y se sincronicen al backend en cuanto haya conexión, sin perder operaciones por fallos transitorios**,
> para **trabajar con confianza en zonas con red intermitente o nula**.

## CUs y RNs relacionados
- CU: [CU-08](../../02_especificacion_funcional/casos-de-uso/CU-08-sincronizar-relevamiento_v1.0.md)
- RN: [RN-06](../../02_especificacion_funcional/reglas-de-negocio/RN-06-guids-cliente-idempotencia_v1.0.md)

## Alcance
- Tabla `PendingOperations` en SQLite local.
- Servicio background drainer con política exponencial (5s, 15s, 1m, 5m, 15m).
- Estados: `pendiente`, `en_envio`, `enviado`, `error`, `terminal_error`.
- Detección de conectividad y disparo automático.
- UI mínima: badge de sync + panel de estado [W-M06].

## Criterios de aceptación
- **CA-3.1** Crear punto offline → operación queda en outbox `pendiente`.
- **CA-3.2** Al volver la conexión, drainer envía y la operación pasa a `enviado`.
- **CA-3.3** Falla transitoria del backend → estado `error`, `next_retry_at` exponencial.
- **CA-3.4** Tras 7+ reintentos → estado `terminal_error` y notificación al usuario.
- **CA-3.5** Reenvío del mismo evento es idempotente (no duplica nada).

## Dependencias
- US-01, US-02, US-04 (push endpoint), spike de sync de Fase 0.

## DoR — checklist
- [x] Atada a EP-01.1 y CU-08.
- [x] Criterios verificables (tests con red simulada).
- [x] Estimada.
- [x] Spike de sync ejecutado previamente.
- [x] Cabe en un sprint.
- [x] PO confirma.

---
**Fin — US-03**
