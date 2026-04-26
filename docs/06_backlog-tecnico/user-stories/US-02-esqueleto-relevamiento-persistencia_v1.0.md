**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-02-esqueleto-relevamiento-persistencia_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# US-02 — Esqueleto: crear relevamiento + persistir + ver en web

**Épica:** EP-00.1 · **MoSCoW:** Must · **SP:** 8 · **Sprint sugerido:** Sprint 0

> Como **relevador autenticado**,
> quiero **crear un relevamiento con nombre y plantilla raíz desde móvil o web y verlo en el listado de la web**,
> para **validar que el slice trivial end-to-end funciona y que la columna vertebral de DB / API / frontends está conectada**.

## CUs y RNs relacionados
- CU: [CU-04](../../02_especificacion_funcional/casos-de-uso/CU-04-crear-relevamiento_v1.0.md)
- RN: [RN-06](../../02_especificacion_funcional/reglas-de-negocio/RN-06-guids-cliente-idempotencia_v1.0.md), [RN-10](../../02_especificacion_funcional/reglas-de-negocio/RN-10-eventos-append-only_v1.0.md)

## Alcance
- Endpoint `POST /api/v1/surveys`.
- Generación de GUID en cliente.
- Persistencia con tabla `Surveys` + evento `created` en `AuditEvents`.
- Pantallas: "Nuevo relevamiento" en móvil y web; "Listado" en web.
- Plantilla raíz seed creada por una migración inicial.

## Criterios de aceptación
- **CA-2.1** Crear desde web → aparece en el listado web inmediatamente.
- **CA-2.2** Crear desde móvil con conexión → aparece en web tras refrescar.
- **CA-2.3** GUID generado en cliente se preserva en backend.
- **CA-2.4** Evento `created` registrado en `AuditEvents` con autor, timestamp, origen.
- **CA-2.5** Sin plantillas publicadas → bloquea con mensaje (E1 de CU-04). En seed siempre hay raíz publicada.

## Dependencias
- US-01, BT-06, BT-08.

## DoR — checklist
- [x] Atada a EP-00.1 y CU-04.
- [x] Criterios verificables.
- [x] Estimada.
- [x] Sin `[REQUIERE_INFO]` bloqueante.
- [x] Cabe en un sprint.
- [x] PO confirma.

---
**Fin — US-02**
