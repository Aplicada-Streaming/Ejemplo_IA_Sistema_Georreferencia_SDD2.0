**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-14-permisos-por-punto_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# US-14 — Permisos por punto: dueño edita todo, colaborador lo suyo

**Épica:** EP-02.3 · **MoSCoW:** Must · **SP:** 8 · **Sprint sugerido:** Slice 6

> Como **dueño del relevamiento o colaborador asignado**,
> quiero **que el sistema imponga las reglas de edición por punto: el dueño puede editar todo, los colaboradores solo lo suyo**,
> para **trabajar en equipo sin pisarnos**.

## CUs y RNs relacionados
- CU: [CU-05](../../02_especificacion_funcional/casos-de-uso/CU-05-asignar-colaboradores-y-ciclo-vida_v1.0.md), [CU-07](../../02_especificacion_funcional/casos-de-uso/CU-07-editar-catalogo-punto-desde-movil_v1.0.md), [CU-10](../../02_especificacion_funcional/casos-de-uso/CU-10-revisar-y-editar-relevamiento-web_v1.0.md)
- RN: [RN-01](../../02_especificacion_funcional/reglas-de-negocio/RN-01-permisos-por-punto_v1.0.md), [RN-02](../../02_especificacion_funcional/reglas-de-negocio/RN-02-restricciones-eliminacion-relevamiento_v1.0.md)

## Alcance
- Política de autorización por punto en backend (`PolicyHandler`).
- Aplicación tanto en API REST como en validación de eventos del sync push.
- Modo lectura en frontends para usuarios sin permiso de edición.
- Bloqueo de eliminación de relevamiento por colaborador.
- Tests unitarios y de integración cubriendo la matriz completa.

## Criterios de aceptación
- **CA-14.1** Colaborador edita su propio punto → permitido.
- **CA-14.2** Colaborador edita punto creado por otro → 403.
- **CA-14.3** Dueño edita cualquier punto del relevamiento → permitido.
- **CA-14.4** Colaborador intenta eliminar relevamiento → 403.
- **CA-14.5** Frontends muestran modo lectura cuando corresponde.

## Dependencias
- US-13, US-09.

## DoR — checklist
- [x] Atada a EP-02.3.
- [x] Criterios verificables.
- [x] Estimada.
- [x] Cabe en un sprint.
- [x] PO confirma.

---
**Fin — US-14**
