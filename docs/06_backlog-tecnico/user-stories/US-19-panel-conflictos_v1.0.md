**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-19-panel-conflictos_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# US-19 — Panel de conflictos pendientes en web

**Épica:** EP-04.1 · **MoSCoW:** Should · **SP:** 13 · **Sprint sugerido:** Slice 9

> Como **jefe de área o relevador (dueño)**,
> quiero **un panel web que liste los conflictos pendientes de revisión, agrupados por tipo (sobrescrituras, eliminaciones con actividad, capturas post-cierre, candidatos a fusión)**,
> para **ver, priorizar y resolver los casos donde la sincronización automática requiere intervención humana**.

## CUs y RNs relacionados
- CU: [CU-08](../../02_especificacion_funcional/casos-de-uso/CU-08-sincronizar-relevamiento_v1.0.md)
- RN: [RN-07](../../02_especificacion_funcional/reglas-de-negocio/RN-07-resolucion-conflictos-lww-y-precedencia_v1.0.md), [RN-08](../../02_especificacion_funcional/reglas-de-negocio/RN-08-capturas-post-cierre_v1.0.md)

## Alcance
- Pantalla W-W08 con tabs por categoría.
- Endpoints `GET /conflicts` con filtros y `POST /conflicts/{id}/resolve`.
- Notificación post-sync con badge.
- Acciones: revertir sobrescritura, descartar / aplicar capturas post-cierre, reabrir relevamiento, restaurar punto eliminado.

## Criterios de aceptación
- **CA-19.1** Conflicto LWW resuelto auto → aparece en panel; usuario revierte → genera nueva edición que vuelve a aplicar LWW.
- **CA-19.2** Captura post-cierre → aparece en panel; usuario reabre → eventos pendientes se aplican.
- **CA-19.3** Notificación post-sync con conteo correcto.
- **CA-19.4** Filtros por relevamiento y por área.

## Dependencias
- US-04, US-05, US-09.

## DoR — checklist
- [x] Atada a EP-04.1.
- [x] Criterios verificables.
- [x] Estimada.
- [x] Cabe en un sprint.
- [x] PO confirma.

---
**Fin — US-19**
