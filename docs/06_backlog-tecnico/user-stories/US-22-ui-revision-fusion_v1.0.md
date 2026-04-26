**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-22-ui-revision-fusion_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# US-22 — UI de revisión y resolución de candidato a fusión

**Épica:** EP-04.2 · **MoSCoW:** Could · **SP:** 13 · **Sprint sugerido:** Slice 10

> Como **jefe de área**,
> quiero **revisar lado a lado los pares de puntos candidatos a fusión y decidir Fusionar (con elección de posición y valores) o Mantener separados**,
> para **resolver duplicaciones reales sin pisar trabajo de los colaboradores y registrar la decisión en el log de eventos**.

## CUs y RNs relacionados
- CU: [CU-11](../../02_especificacion_funcional/casos-de-uso/CU-11-resolver-candidato-fusion_v1.0.md)
- RN: [RN-09](../../02_especificacion_funcional/reglas-de-negocio/RN-09-deteccion-candidatos-fusion_v1.0.md), [RN-10](../../02_especificacion_funcional/reglas-de-negocio/RN-10-eventos-append-only_v1.0.md)

## Alcance
- Pantalla W-W09: mini-mapa, fotos lado a lado, comparación de campos.
- Acción "Fusionar" → diálogo con posición (centroide / A / B) + selector de valor por campo divergente.
- Acción "Mantener separados" → marca persistente.
- Evento `merged` con valores antes/después y referencia a los Puntos originales.
- Unificación de fotos en el catálogo del Punto resultante.

## Criterios de aceptación
- **CA-22.1** Fusionar con centroide → Punto consolidado en posición media; fotos unificadas; evento `merged` registra todo.
- **CA-22.2** Fusionar con elección de A → posición de A queda; valores divergentes según selección.
- **CA-22.3** Mantener separados → estado `mantenido_separado`; el par no se vuelve a proponer.
- **CA-22.4** Tras fusión, consulta de trazabilidad muestra historia completa (eventos previos + `merged` + posteriores).

## Dependencias
- US-21, US-09, US-19.

## DoR — checklist
- [x] Atada a EP-04.2.
- [x] Criterios verificables.
- [x] Estimada.
- [x] Cabe en un sprint.
- [x] PO confirma.

---
**Fin — US-22**
