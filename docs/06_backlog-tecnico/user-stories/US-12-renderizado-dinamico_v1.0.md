**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-12-renderizado-dinamico_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# US-12 — Renderizado dinámico de campos en web y móvil

**Épica:** EP-02.2 · **MoSCoW:** Should · **SP:** 8 · **Sprint sugerido:** Slice 5

> Como **arquitecto del sistema**,
> quiero **que móvil y web rindan los campos del Punto en función de la versión de plantilla del relevamiento, sin código específico por tipo de inspección**,
> para **agregar plantillas nuevas sin cambiar el frontend**.

## CUs y RNs relacionados
- CU: [CU-03](../../02_especificacion_funcional/casos-de-uso/CU-03-crear-versionar-plantilla_v1.0.md), [CU-06](../../02_especificacion_funcional/casos-de-uso/CU-06-capturar-punto-georreferenciado_v1.0.md), [CU-10](../../02_especificacion_funcional/casos-de-uso/CU-10-revisar-y-editar-relevamiento-web_v1.0.md)

## Alcance
- Endpoint `GET /template-versions/{id}/resolved` con herencia aplicada.
- Componentes de renderizado dinámico en MudBlazor (web y móvil).
- Render por tipo de campo (`texto`, `número`, `fecha`, `booleano`, `selección`).
- Aplicación de validaciones derivadas de la plantilla.
- Cacheo en cliente de la versión resuelta para offline.

## Criterios de aceptación
- **CA-12.1** Plantilla con 3 campos custom → web y móvil los renderizan con el tipo y validación correctos.
- **CA-12.2** Plantilla con campo "no aplica" → no se renderiza.
- **CA-12.3** Plantilla con sobrescritura de hint → muestra el hint nuevo.
- **CA-12.4** Sin conexión: el móvil renderiza desde caché de la versión resuelta.

## Dependencias
- US-11.

## DoR — checklist
- [x] Atada a EP-02.2.
- [x] Criterios verificables.
- [x] Estimada.
- [x] Cabe en un sprint.
- [x] PO confirma.

---
**Fin — US-12**
