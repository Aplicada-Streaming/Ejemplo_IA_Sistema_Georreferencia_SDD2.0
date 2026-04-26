**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-10-mapa-colaborativo_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# US-10 — Mapa colaborativo con diferenciación por colaborador

**Épica:** EP-02.1 · **MoSCoW:** Should · **SP:** 8 · **Sprint sugerido:** Slice 4

> Como **relevador o jefe de área**,
> quiero **ver en el mapa los puntos diferenciados por colaborador y poder filtrar entre los míos y los de todos, viendo qué se editó recientemente**,
> para **coordinar la cobertura sin duplicar trabajo y enterarme de actividad reciente**.

## CUs y RNs relacionados
- CU: [CU-10](../../02_especificacion_funcional/casos-de-uso/CU-10-revisar-y-editar-relevamiento-web_v1.0.md)

## Alcance
- Mapa OpenStreetMap con marcadores por punto.
- Color/ícono por `created_by`, paleta categórica estable.
- Filtros: "ver solo mis puntos" / "ver todos" / "actividad reciente (24h)".
- Indicador visual sobre puntos con actividad reciente.
- Leyenda con colaboradores activos.

## Criterios de aceptación
- **CA-10.1** Dos colaboradores → puntos con colores distintos y leyenda visible.
- **CA-10.2** Filtro "solo mis puntos" → oculta los de otros.
- **CA-10.3** Punto editado dentro de las últimas 24h → indicador visible.
- **CA-10.4** Punto editado hace 25h → sin indicador.
- **CA-10.5** Mismo color asignado al mismo `created_by` entre sesiones.

## Dependencias
- US-09.

## DoR — checklist
- [x] Atada a EP-02.1.
- [x] Criterios verificables.
- [x] Estimada.
- [x] Cabe en un sprint.
- [x] PO confirma.

---
**Fin — US-10**
