**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-09-catalogo-fotos-web_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# US-09 — Catálogo de fotos por punto y vista plana en web

**Épica:** EP-02.1 · **MoSCoW:** Must · **SP:** 13 · **Sprint sugerido:** Slice 4

> Como **jefe de área o relevador en gabinete**,
> quiero **revisar el catálogo del relevamiento en web, agrupando por punto o como vista plana, y editar título / descripción / comentarios respetando permisos**,
> para **consolidar la información y dejarla lista para análisis**.

## CUs y RNs relacionados
- CU: [CU-10](../../02_especificacion_funcional/casos-de-uso/CU-10-revisar-y-editar-relevamiento-web_v1.0.md), [CU-12](../../02_especificacion_funcional/casos-de-uso/CU-12-consultar-trazabilidad-punto_v1.0.md)
- RN: [RN-01](../../02_especificacion_funcional/reglas-de-negocio/RN-01-permisos-por-punto_v1.0.md), [RN-10](../../02_especificacion_funcional/reglas-de-negocio/RN-10-eventos-append-only_v1.0.md)

## Alcance
- Pantalla W-W05 con layout 3 zonas (mapa + lista + catálogo).
- Toggle vista por punto / vista plana.
- Edición inline de título, descripción, comentarios.
- Aplicación de permisos por punto (modo lectura cuando corresponda).
- Pestaña "Trazabilidad" con histórico de eventos.
- Cada foto enlaza al mapa.

## Criterios de aceptación
- **CA-9.1** Vista por punto agrupa fotos del mismo Punto.
- **CA-9.2** Vista plana muestra todas las fotos con info del Punto.
- **CA-9.3** Colaborador no puede editar campos de un punto creado por otro (modo lectura).
- **CA-9.4** Dueño del relevamiento puede editar todos los puntos.
- **CA-9.5** Pestaña trazabilidad muestra eventos en orden cronológico con filtro por autor.
- **CA-9.6** Click en enlace de mapa centra y resalta el punto.

## Dependencias
- US-04, US-05, US-14 (permisos por punto).

## DoR — checklist
- [x] Atada a EP-02.1.
- [x] Criterios verificables.
- [x] Estimada.
- [x] Cabe en un sprint.
- [x] PO confirma.

---
**Fin — US-09**
