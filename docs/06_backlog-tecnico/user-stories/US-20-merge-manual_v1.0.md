**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-20-merge-manual_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# US-20 — Merge manual con valores lado a lado

**Épica:** EP-04.1 · **MoSCoW:** Should · **SP:** 8 · **Sprint sugerido:** Slice 9

> Como **jefe de área**,
> quiero **resolver una sobrescritura LWW eligiendo el valor final cuando los valores en conflicto requieren una decisión manual**,
> para **mantener la información correcta sin perder la edición original ni la sobrescritura**.

## CUs y RNs relacionados
- CU: [CU-08](../../02_especificacion_funcional/casos-de-uso/CU-08-sincronizar-relevamiento_v1.0.md) (panel de conflictos manual).

## Alcance
- UI lado a lado sobre el ítem del panel: valor anterior, valor LWW aplicado.
- Selector "valor final"; al confirmar genera un nuevo evento `field_updated` que reemplaza con el valor elegido.
- Audit del usuario que toma la decisión.

## Criterios de aceptación
- **CA-20.1** Conflicto seleccionado muestra ambos valores con autores y timestamps.
- **CA-20.2** Confirmar el valor anterior → backend genera nuevo evento; LWW vuelve a aplicar.
- **CA-20.3** Confirmar el valor actual → marca conflicto como `revisado_sin_cambio`.
- **CA-20.4** Conflicto resuelto deja de aparecer en panel.

## Dependencias
- US-19.

## DoR — checklist
- [x] Atada a EP-04.1.
- [x] Criterios verificables.
- [x] Estimada.
- [x] Cabe en un sprint.
- [x] PO confirma.

---
**Fin — US-20**
