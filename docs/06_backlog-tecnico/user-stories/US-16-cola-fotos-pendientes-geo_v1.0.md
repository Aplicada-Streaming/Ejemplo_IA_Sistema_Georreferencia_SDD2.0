**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-16-cola-fotos-pendientes-geo_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# US-16 — Cola de fotos pendientes de georreferenciar manualmente

**Épica:** EP-03.1 · **MoSCoW:** Should · **SP:** 8 · **Sprint sugerido:** Slice 7

> Como **jefe de área o relevador**,
> quiero **georreferenciar manualmente las fotos del lote que no traían EXIF, ingresando coordenadas o seleccionando posición en un picker en mapa**,
> para **completar la cobertura del lote sin descartar fotos**.

## CUs y RNs relacionados
- CU: [CU-09](../../02_especificacion_funcional/casos-de-uso/CU-09-cargar-lote-fotos-web_v1.0.md)

## Alcance
- Listado de fotos pendientes por relevamiento.
- Endpoint `POST /photos/{id}/geo-resolve` con coordenadas.
- Picker en mapa OpenStreetMap.
- Asociación a Punto existente o creación de nuevo Punto según proximidad.

## Criterios de aceptación
- **CA-16.1** Foto pendiente seleccionada → form lat/lng o picker → confirma.
- **CA-16.2** Coordenadas dentro del radio del modo elegido → asocia a Punto cercano.
- **CA-16.3** Coordenadas fuera del radio → crea nuevo Punto.
- **CA-16.4** Lista se actualiza al confirmar.

## Dependencias
- US-15.

## DoR — checklist
- [x] Atada a EP-03.1.
- [x] Criterios verificables.
- [x] Estimada.
- [x] Cabe en un sprint.
- [x] PO confirma.

---
**Fin — US-16**
