**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-06-plantilla-raiz_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# US-06 — Plantilla raíz con campos comunes y parámetros de captura

**Épica:** EP-01.2 · **MoSCoW:** Must · **SP:** 8 · **Sprint sugerido:** Slice 2

> Como **arquitecto del sistema y futuros jefes de área**,
> quiero **una plantilla genérica raíz no eliminable con campos comunes y parámetros de captura por defecto**,
> para **que toda inspección herede de una base coherente y sea posible análisis transversal**.

## CUs y RNs relacionados
- CU: [CU-03](../../02_especificacion_funcional/casos-de-uso/CU-03-crear-versionar-plantilla_v1.0.md)
- RN: [RN-03](../../02_especificacion_funcional/reglas-de-negocio/RN-03-plantilla-raiz-inmutable_v1.0.md), [RN-05](../../02_especificacion_funcional/reglas-de-negocio/RN-05-inmutabilidad-plantilla-publicada_v1.0.md)

## Alcance
- Migración inicial seed con plantilla raíz `is_root=1`, `is_deletable=0`, versión 1 publicada.
- Campos comunes: fecha, ubicación (auto), condición general, observaciones, prioridad.
- Parámetros de captura default: gps_timeout=30, accuracy=50, radio modo recorrido (`movil_radius_m`)=10, photo_max_long=2048, photo_quality=85.
- Validación que rechaza eliminación.

## Criterios de aceptación
- **CA-6.1** Tras la migración inicial, existe plantilla raíz con versión 1 publicada.
- **CA-6.2** Endpoint que intenta eliminar la raíz responde 409.
- **CA-6.3** Endpoint `GET /template-versions/{id}/resolved` devuelve la raíz con sus campos y parámetros.
- **CA-6.4** Crear hija de la raíz funciona; crear hija sin padre que no sea la raíz responde 422.

## Dependencias
- US-02, BT-06.

## DoR — checklist
- [x] Atada a EP-01.2.
- [x] Criterios verificables.
- [x] Estimada.
- [x] Cabe en un sprint.
- [x] PO confirma.

---
**Fin — US-06**
