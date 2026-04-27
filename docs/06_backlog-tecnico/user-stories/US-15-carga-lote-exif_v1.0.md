**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-15-carga-lote-exif_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# US-15 — Subida en lote desde web con extracción EXIF

**Épica:** EP-03.1 · **MoSCoW:** Should · **SP:** 13 · **Sprint sugerido:** Slice 7

> Como **jefe de área o relevador en gabinete**,
> quiero **subir un lote de fotos desde la web a un relevamiento, eligiendo el modo de agrupación y aprovechando el EXIF cuando esté disponible**,
> para **incorporar relevamientos hechos sin la app móvil sin perder información geoespacial**.

## CUs y RNs relacionados
- CU: [CU-09](../../02_especificacion_funcional/casos-de-uso/CU-09-cargar-lote-fotos-web_v1.0.md)

## Alcance
- Pantalla W-W07 con drag-and-drop.
- Endpoint `POST /surveys/{id}/manual-upload` multipart.
- Worker de imágenes procesa el lote: extracción EXIF, agrupación por modo, creación de Puntos y Fotos.
- Comentarios genéricos iniciales editables.
- Resumen post-procesamiento.

## Criterios de aceptación
- **CA-15.1** Lote 50 fotos con EXIF en modo recorrido radio 10m → puntos por proximidad espacial y temporal.
- **CA-15.2** Lote en modo detenido → todas las fotos al mismo punto.
- **CA-15.3** Comentarios genéricos creados con fecha de carga.
- **CA-15.4** Foto sin EXIF → encolada en pendientes (cubierto por US-16).
- **CA-15.5** Origen `web_manual_upload` correctamente marcado.

## Dependencias
- US-09, US-17 (storage configurado).

## DoR — checklist
- [x] Atada a EP-03.1.
- [x] Criterios verificables.
- [x] Estimada.
- [x] Cabe en un sprint.
- [x] PO confirma.

---
**Fin — US-15**
