**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-21-deteccion-candidatos-fusion_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# US-21 — Detección automática de candidatos a fusión durante sync

**Épica:** EP-04.2 · **MoSCoW:** Must · **SP:** 13 · **Sprint sugerido:** Slice 10

> Como **arquitecto del sistema**,
> quiero **que el backend detecte automáticamente pares de puntos cercanos creados por distintos colaboradores en el mismo relevamiento, y los marque como candidatos a fusión sin fusionar automáticamente**,
> para **evitar duplicaciones invisibles y a la vez no perder información cuando los puntos cercanos son defectos legítimamente distintos**.

## CUs y RNs relacionados
- CU: [CU-08](../../02_especificacion_funcional/casos-de-uso/CU-08-sincronizar-relevamiento_v1.0.md), [CU-11](../../02_especificacion_funcional/casos-de-uso/CU-11-resolver-candidato-fusion_v1.0.md)
- RN: [RN-09](../../02_especificacion_funcional/reglas-de-negocio/RN-09-deteccion-candidatos-fusion_v1.0.md)

## Alcance
- Worker de sync calcula candidatos al recibir `point.created` o `point.coords.updated`.
- Consulta espacial usando `GEOGRAPHY` con buffer de `merge_radius_m`.
- Filtro por colaboradores distintos + ventana temporal `merge_time_window`.
- Persistencia en `MergeCandidates`.
- Emisión de notificación al panel ([US-19](US-19-panel-conflictos_v1.0.md)).

## Criterios de aceptación
- **CA-21.1** Punto creado por colab. A + punto a 7m por colab. B en 2h con threshold 10m/24h → candidato `pendiente`.
- **CA-21.2** Dos puntos del mismo colab. → no se crea candidato.
- **CA-21.3** Punto a 12m con threshold 10m → no se crea candidato.
- **CA-21.4** Diferencia temporal 30h con ventana 24h → no se crea candidato.
- **CA-21.5** Par marcado `mantenido_separado` no se vuelve a proponer.

## Dependencias
- US-04, US-21 spike validado.

## DoR — checklist
- [x] Atada a EP-04.2.
- [x] Criterios verificables.
- [x] Estimada.
- [x] Cabe en un sprint.
- [x] PO confirma.

---
**Fin — US-21**
