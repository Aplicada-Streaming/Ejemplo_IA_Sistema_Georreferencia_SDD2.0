**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-05-pull-diferencial_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# US-05 — Pull diferencial de eventos al móvil

**Épica:** EP-01.1 · **MoSCoW:** Must · **SP:** 8 · **Sprint sugerido:** Slice 1

> Como **colaborador en una campaña multi-relevador**,
> quiero **recibir los eventos de mis pares posteriores a mi último pull para tener el estado actualizado en mi móvil**,
> para **ver los puntos que crearon, las ediciones que hicieron, y los conflictos que se resolvieron**.

## CUs y RNs relacionados
- CU: [CU-08](../../02_especificacion_funcional/casos-de-uso/CU-08-sincronizar-relevamiento_v1.0.md)

## Alcance
- Endpoint `GET /api/v1/surveys/{id}/changes?since={timestamp}`.
- Persistencia del `last_pulled_at` en SQLite local.
- Aplicación de eventos remotos a la DB local del móvil.
- Notificación post-sync con resumen.

## Criterios de aceptación
- **CA-5.1** Pull con `since` cero devuelve todos los eventos del relevamiento.
- **CA-5.2** Pull con `since` posterior solo devuelve los eventos siguientes.
- **CA-5.3** Aplicar eventos en orden de `timestamp_original` da estado coherente.
- **CA-5.4** Tras pull exitoso, `last_pulled_at` queda persistido.
- **CA-5.5** Si dos clientes hacen pull simultáneo, ambos reciben el set correcto.

## Dependencias
- US-04, US-03, BT-04.

## DoR — checklist
- [x] Atada a EP-01.1.
- [x] Criterios verificables.
- [x] Estimada.
- [x] Cabe en un sprint.
- [x] PO confirma.

---
**Fin — US-05**
